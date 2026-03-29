using System;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Surface;
using TopSpeed.Physics.Tires;
using TopSpeed.Vehicles;

namespace TopSpeed.Bots
{
    public static partial class BotPhysics
    {
        public static void Step(BotPhysicsConfig config, ref BotPhysicsState state, in BotPhysicsInput input)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (input.ElapsedSeconds <= 0f)
                return;

            if (state.Gear < 1 || state.Gear > config.Gears)
                state.Gear = 1;
            if (state.AutomaticCouplingFactor <= 0f)
                state.AutomaticCouplingFactor = 1f;
            if (state.CvtRatio <= 0f)
                state.CvtRatio = config.AutomaticTuning.Cvt.RatioMax;

            var surface = SurfaceModel.Resolve(input.Surface, config.SurfaceTractionFactor, config.Deceleration);
            var surfaceTraction = surface.Traction;
            var surfaceDecel = surface.Deceleration;

            var thrust = 0f;
            if (input.Throttle == 0)
                thrust = input.Brake;
            else if (input.Brake == 0 || -input.Brake <= input.Throttle)
                thrust = input.Throttle;
            else
                thrust = input.Brake;

            var speedKph = Math.Max(0f, state.SpeedKph);
            var speedMpsCurrent = speedKph / 3.6f;
            var throttle = Math.Max(0f, Math.Min(100f, input.Throttle)) / 100f;
            var brake = Math.Max(0f, Math.Min(100f, -input.Brake)) / 100f;
            var steeringInput = input.Steering;
            var surfaceTractionMod = surfaceTraction / config.SurfaceTractionFactor;
            var longitudinalGripFactor = 1.0f;
            var speedDiffKph = 0f;
            var tireState = new TireModelState(state.LateralVelocityMps, state.YawRateRad);
            var activeTransmissionType = config.ActiveTransmissionType;
            var automaticFamily = TransmissionTypes.IsAutomaticFamily(activeTransmissionType);
            var autoOutput = default(AutomaticDrivelineOutput);
            var driveRatioOverride = 0f;
            if (automaticFamily)
            {
                var currentEngineRpmEstimate = Calculator.RpmAtSpeed(config.Powertrain, speedMpsCurrent, state.Gear);
                autoOutput = AutomaticDrivelineModel.Step(
                    activeTransmissionType,
                    config.AutomaticTuning,
                    new AutomaticDrivelineInput(
                        input.ElapsedSeconds,
                        speedMpsCurrent,
                        throttle,
                        brake,
                        shifting: state.AutoShiftCooldownSeconds > 0f,
                        wheelCircumferenceM: config.WheelRadiusM * 2f * (float)Math.PI,
                        finalDriveRatio: config.FinalDriveRatio,
                        idleRpm: config.IdleRpm,
                        revLimiter: config.RevLimiter,
                        launchRpm: config.LaunchRpm,
                        currentEngineRpm: currentEngineRpmEstimate),
                    new AutomaticDrivelineState(state.AutomaticCouplingFactor, state.CvtRatio));
                state.AutomaticCouplingFactor = autoOutput.CouplingFactor;
                state.CvtRatio = autoOutput.CvtRatio;
                driveRatioOverride = autoOutput.EffectiveDriveRatio;
                if (activeTransmissionType == TransmissionType.Cvt)
                    state.Gear = 1;
            }
            else
            {
                state.AutomaticCouplingFactor = 1f;
                state.EffectiveDriveRatio = 0f;
            }

            if (thrust > 10f)
            {
                var tireOutput = SolveTireModel(config, input.ElapsedSeconds, speedMpsCurrent, steeringInput, surfaceTractionMod, 1f, tireState);
                longitudinalGripFactor = tireOutput.LongitudinalGripFactor;
                var accelMps2 = Calculator.DriveAccel(
                    config.Powertrain,
                    state.Gear,
                    speedMpsCurrent,
                    throttle,
                    surfaceTractionMod,
                    longitudinalGripFactor,
                    driveRatioOverride > 0f ? driveRatioOverride : (float?)null);
                if (automaticFamily)
                    accelMps2 *= Math.Max(0f, Math.Min(1f, state.AutomaticCouplingFactor));
                var newSpeedMps = speedMpsCurrent + (accelMps2 * input.ElapsedSeconds);
                if (newSpeedMps < 0f)
                    newSpeedMps = 0f;
                speedDiffKph = (newSpeedMps - speedMpsCurrent) * 3.6f;
            }
            else
            {
                var surfaceDecelMod = surfaceDecel / config.Deceleration;
                var brakeInput = Math.Max(0f, Math.Min(100f, -input.Brake)) / 100f;
                var brakeDecel = CalculateBrakeDecel(config, brakeInput, surfaceDecelMod);
                var engineBrakeDecel = CalculateEngineBrakingDecel(
                    config,
                    state.Gear,
                    speedMpsCurrent,
                    surfaceDecelMod,
                    driveRatioOverride > 0f ? driveRatioOverride : (float?)null);
                var passiveResistiveDecel = CalculateResistiveDecel(
                    config,
                    state.Gear,
                    speedMpsCurrent,
                    surfaceDecelMod,
                    driveRatioOverride > 0f ? driveRatioOverride : (float?)null);
                var totalDecel = passiveResistiveDecel + engineBrakeDecel;
                if (thrust < -10f)
                    totalDecel += brakeDecel;
                speedDiffKph = -totalDecel * input.ElapsedSeconds;
                if (automaticFamily)
                    speedDiffKph += autoOutput.CreepAccelerationMps2 * input.ElapsedSeconds * 3.6f;
            }

            speedKph += speedDiffKph;
            var safetySpeed = ResolveForwardSafetySpeedKph(config.TopSpeedKph);
            if (speedKph > safetySpeed)
                speedKph = safetySpeed;
            if (speedKph < 0f)
                speedKph = 0f;

            if (activeTransmissionType != TransmissionType.Cvt)
            {
                UpdateAutomaticGear(
                    config,
                    ref state,
                    input.ElapsedSeconds,
                    speedKph / 3.6f,
                    throttle,
                    surfaceTractionMod,
                    longitudinalGripFactor,
                    driveRatioOverride > 0f ? driveRatioOverride : (float?)null);
            }
            else
            {
                state.AutoShiftCooldownSeconds = 0f;
            }

            if (thrust < -50f && speedKph > 0f)
                steeringInput = steeringInput * 2 / 3;

            var speedMps = speedKph / 3.6f;
            state.PositionY += speedMps * input.ElapsedSeconds;
            state.SpeedKph = speedKph;
            state.EffectiveDriveRatio = driveRatioOverride;

            var surfaceTractionModLat = surfaceTraction / config.SurfaceTractionFactor;
            var lateralOutput = SolveTireModel(config, input.ElapsedSeconds, speedMps, steeringInput, surfaceTractionModLat, surface.LateralSpeedMultiplier, tireState);
            state.PositionX += lateralOutput.LateralSpeedMps * input.ElapsedSeconds;
            state.LateralVelocityMps = lateralOutput.State.LateralVelocityMps;
            state.YawRateRad = lateralOutput.State.YawRateRad;
        }
    }
}
