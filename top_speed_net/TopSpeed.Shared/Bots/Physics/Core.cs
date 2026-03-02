using System;
using TopSpeed.Data;

namespace TopSpeed.Bots
{
    public static partial class BotPhysics
    {
        private const float BaseLateralSpeed = 7.0f;
        private const float StabilitySpeedRef = 45.0f;

        public static void Step(BotPhysicsConfig config, ref BotPhysicsState state, in BotPhysicsInput input)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (input.ElapsedSeconds <= 0f)
                return;

            if (state.Gear < 1 || state.Gear > config.Gears)
                state.Gear = 1;

            var surfaceTraction = config.SurfaceTractionFactor;
            var surfaceDecel = config.Deceleration;
            switch (input.Surface)
            {
                case TrackSurface.Gravel:
                    surfaceTraction = (surfaceTraction * 2f) / 3f;
                    surfaceDecel = (surfaceDecel * 2f) / 3f;
                    break;
                case TrackSurface.Water:
                    surfaceTraction = (surfaceTraction * 3f) / 5f;
                    surfaceDecel = (surfaceDecel * 3f) / 5f;
                    break;
                case TrackSurface.Sand:
                    surfaceTraction = surfaceTraction / 2f;
                    surfaceDecel = (surfaceDecel * 3f) / 2f;
                    break;
                case TrackSurface.Snow:
                    surfaceDecel = surfaceDecel / 2f;
                    break;
            }

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
            var steeringInput = input.Steering;
            var surfaceTractionMod = surfaceTraction / config.SurfaceTractionFactor;
            var longitudinalGripFactor = 1.0f;
            var speedDiffKph = 0f;

            if (thrust > 10f)
            {
                var steeringCommandAccel = (steeringInput / 100.0f) * config.Steering;
                steeringCommandAccel = Clamp(steeringCommandAccel, -1f, 1f);
                var steerRadAccel = DegToRad(config.MaxSteerDeg * steeringCommandAccel);
                var curvatureAccel = (float)Math.Tan(steerRadAccel) / config.WheelbaseM;
                var desiredLatAccel = curvatureAccel * speedMpsCurrent * speedMpsCurrent;
                var desiredLatAccelAbs = Math.Abs(desiredLatAccel);
                var grip = config.TireGripCoefficient * surfaceTractionMod * config.LateralGripCoefficient;
                var maxLatAccel = grip * 9.80665f;
                var lateralRatio = maxLatAccel > 0f ? Math.Min(1.0f, desiredLatAccelAbs / maxLatAccel) : 0f;
                longitudinalGripFactor = (float)Math.Sqrt(Math.Max(0.0, 1.0 - (lateralRatio * lateralRatio)));

                var driveRpm = CalculateDriveRpm(config, state.Gear, speedMpsCurrent, throttle);
                var engineTorque = CalculateEngineTorqueNm(config, driveRpm) * throttle * config.PowerFactor;
                var gearRatio = config.GetGearRatio(state.Gear);
                var wheelTorque = engineTorque * gearRatio * config.FinalDriveRatio * config.DrivetrainEfficiency;
                var wheelForce = wheelTorque / config.WheelRadiusM;
                var tractionLimit = config.TireGripCoefficient * surfaceTractionMod * config.MassKg * 9.80665f;
                if (wheelForce > tractionLimit)
                    wheelForce = tractionLimit;
                wheelForce *= longitudinalGripFactor;

                var dragForce = 0.5f * 1.225f * config.DragCoefficient * config.FrontalAreaM2 * speedMpsCurrent * speedMpsCurrent;
                var rollingForce = config.RollingResistanceCoefficient * config.MassKg * 9.80665f;
                var netForce = wheelForce - dragForce - rollingForce;
                var accelMps2 = netForce / config.MassKg;
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
                var engineBrakeDecel = CalculateEngineBrakingDecel(config, state.Gear, speedMpsCurrent, surfaceDecelMod);
                var totalDecel = thrust < -10f ? (brakeDecel + engineBrakeDecel) : engineBrakeDecel;
                speedDiffKph = -totalDecel * input.ElapsedSeconds;
            }

            speedKph += speedDiffKph;
            if (speedKph > config.TopSpeedKph)
                speedKph = config.TopSpeedKph;
            if (speedKph < 0f)
                speedKph = 0f;

            UpdateAutomaticGear(config, ref state, input.ElapsedSeconds, speedKph / 3.6f, throttle, surfaceTractionMod, longitudinalGripFactor);
            if (thrust < -50f && speedKph > 0f)
                steeringInput = steeringInput * 2 / 3;

            var speedMps = speedKph / 3.6f;
            state.PositionY += speedMps * input.ElapsedSeconds;
            state.SpeedKph = speedKph;

            var surfaceMultiplier = input.Surface == TrackSurface.Snow ? 1.44f : 1.0f;
            var steeringCommandLat = (steeringInput / 100.0f) * config.Steering;
            steeringCommandLat = Clamp(steeringCommandLat, -1f, 1f);
            var steerRadLat = DegToRad(config.MaxSteerDeg * steeringCommandLat);
            var curvatureLat = (float)Math.Tan(steerRadLat) / config.WheelbaseM;
            var surfaceTractionModLat = surfaceTraction / config.SurfaceTractionFactor;
            var gripLat = config.TireGripCoefficient * surfaceTractionModLat * config.LateralGripCoefficient;
            var maxLatAccelLat = gripLat * 9.80665f;
            var desiredLatAccelLat = curvatureLat * speedMps * speedMps;
            var massFactor = (float)Math.Sqrt(1500f / config.MassKg);
            if (massFactor > 3.0f)
                massFactor = 3.0f;
            var stabilityScale = 1.0f - (config.HighSpeedStability * (speedMps / StabilitySpeedRef) * massFactor);
            if (stabilityScale < 0.2f)
                stabilityScale = 0.2f;
            else if (stabilityScale > 1.0f)
                stabilityScale = 1.0f;
            var responseTime = BaseLateralSpeed / 20.0f;
            var maxLatSpeed = maxLatAccelLat * responseTime * stabilityScale;
            var desiredLatSpeed = desiredLatAccelLat * responseTime;
            if (desiredLatSpeed > maxLatSpeed)
                desiredLatSpeed = maxLatSpeed;
            else if (desiredLatSpeed < -maxLatSpeed)
                desiredLatSpeed = -maxLatSpeed;
            var lateralSpeed = desiredLatSpeed * surfaceMultiplier;
            state.PositionX += lateralSpeed * input.ElapsedSeconds;
        }
    }
}
