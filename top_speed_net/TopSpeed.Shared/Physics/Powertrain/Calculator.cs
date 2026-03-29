using System;

namespace TopSpeed.Physics.Powertrain
{
    public static class Calculator
    {
        private const float AirDensityKgPerM3 = 1.225f;
        private const float Gravity = 9.80665f;
        private const float TwoPi = (float)(Math.PI * 2.0);
        private const float CompressionRpmFactorFloor = 0.22f;

        public static float DriveRpm(
            Config config,
            int gear,
            float speedMps,
            float throttle,
            bool inReverse,
            float? driveRatioOverride = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var wheelCircumference = config.WheelRadiusM * TwoPi;
            var ratio = inReverse
                ? config.ReverseGearRatio
                : (driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                    ? driveRatioOverride.Value
                    : config.GetGearRatio(gear));
            var speedBasedRpm = wheelCircumference > 0f
                ? (speedMps / wheelCircumference) * 60f * ratio * config.FinalDriveRatio
                : 0f;
            var launchTarget = config.IdleRpm + (Clamp(throttle, 0f, 1f) * (config.LaunchRpm - config.IdleRpm));
            var rpm = Math.Max(speedBasedRpm, launchTarget);
            return Clamp(rpm, config.IdleRpm, config.RevLimiter);
        }

        public static float RpmAtSpeed(Config config, float speedMps, int gear, float? driveRatioOverride = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var wheelCircumference = config.WheelRadiusM * TwoPi;
            if (wheelCircumference <= 0f)
                return 0f;
            var gearRatio = driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                ? driveRatioOverride.Value
                : config.GetGearRatio(gear);
            return (speedMps / wheelCircumference) * 60f * gearRatio * config.FinalDriveRatio;
        }

        public static float EngineTorque(Config config, float rpm)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            return Math.Max(0f, config.TorqueCurve.EvaluateTorque(Clamp(rpm, config.IdleRpm, config.RevLimiter)));
        }

        public static float Horsepower(float torqueNm, float rpm)
        {
            if (rpm <= 0f || torqueNm <= 0f)
                return 0f;
            return (torqueNm * rpm) / 7127f;
        }

        public static float DriveAccel(
            Config config,
            int gear,
            float speedMps,
            float throttle,
            float surfaceTractionModifier,
            float longitudinalGripFactor,
            float? driveRatioOverride = null)
        {
            return DriveAccelCore(
                config,
                gear,
                inReverse: false,
                speedMps,
                throttle,
                surfaceTractionModifier,
                longitudinalGripFactor,
                driveRatioOverride);
        }

        public static float ReverseAccel(
            Config config,
            float speedMps,
            float throttle,
            float surfaceTractionModifier,
            float longitudinalGripFactor)
        {
            return DriveAccelCore(
                config,
                1,
                inReverse: true,
                speedMps,
                throttle,
                surfaceTractionModifier,
                longitudinalGripFactor);
        }

        public static float BrakeDecelKph(
            Config config,
            float brakeInput,
            float surfaceDecelerationModifier)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (brakeInput <= 0f)
                return 0f;

            var grip = Math.Max(0.1f, config.TireGripCoefficient * surfaceDecelerationModifier);
            var decelMps2 = Clamp(brakeInput, 0f, 1f) * config.BrakeStrength * grip * Gravity;
            return decelMps2 * 3.6f;
        }

        public static float EngineBrakeDecelKph(
            Config config,
            int gear,
            bool inReverse,
            float speedMps,
            float surfaceDecelerationModifier,
            float currentEngineRpm,
            float? driveRatioOverride = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (config.EngineBrakingTorqueNm <= 0f || config.MassKg <= 0f || config.WheelRadiusM <= 0f)
                return 0f;

            var rpmRange = config.RevLimiter - config.IdleRpm;
            if (rpmRange <= 0f)
                return 0f;

            var ratio = inReverse
                ? config.ReverseGearRatio
                : (driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                    ? driveRatioOverride.Value
                    : config.GetGearRatio(gear));
            var speedBasedRpm = RpmForRatio(config, speedMps, ratio);
            var effectiveRpm = Math.Max(currentEngineRpm, speedBasedRpm);
            var rpmFactor = (effectiveRpm - config.IdleRpm) / rpmRange;
            rpmFactor = Clamp(rpmFactor, 0f, 1f);
            var compressionFactor = BlendCompressionFactor(rpmFactor);

            float drivelineTorque;
            if (config.UseStrictEngineClutchModel)
            {
                var omega = effectiveRpm * TwoPi / 60f;
                var frictionTorque = config.EngineFrictionCoulombNm + (config.EngineFrictionViscousNmPerRadS * Math.Max(0f, omega));
                var pumpingTorque = config.EnginePumpingLossNmAtClosedThrottle * compressionFactor;
                drivelineTorque = frictionTorque + pumpingTorque + config.EngineAccessoryTorqueNm + config.ClutchDragTorqueNm;
            }
            else
            {
                drivelineTorque = config.EngineFrictionTorqueNm + (config.EngineBrakingTorqueNm * config.EngineBraking * compressionFactor);
            }
            var wheelTorque = drivelineTorque * ratio * config.FinalDriveRatio * config.DrivetrainEfficiency;
            var wheelForce = wheelTorque / config.WheelRadiusM;
            var decelMps2 = (wheelForce / config.MassKg) * surfaceDecelerationModifier;
            return Math.Max(0f, decelMps2 * 3.6f);
        }

        public static float ResistiveForce(Config config, float speedMps, float currentEngineRpm = 0f)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var dragForce = AerodynamicDragForce(config, speedMps);
            var rollingForce = RollingResistanceForce(config, speedMps, surfaceDecelerationModifier: 1f);
            var drivelineForce = DrivelineCoastForce(config, currentEngineRpm);
            return dragForce + rollingForce + drivelineForce;
        }

        public static float ResistiveDecelKph(
            Config config,
            float speedMps,
            float surfaceDecelerationModifier = 1f,
            float currentEngineRpm = 0f)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (config.MassKg <= 0f)
                return 0f;

            var dragForce = AerodynamicDragForce(config, speedMps);
            var rollingForce = RollingResistanceForce(config, speedMps, surfaceDecelerationModifier);
            var drivelineForce = DrivelineCoastForce(config, currentEngineRpm);
            var decelMps2 = (dragForce + rollingForce + drivelineForce) / config.MassKg;
            if (config.UseStrictEngineClutchModel)
            {
                var speedKph = Math.Max(0f, speedMps * 3.6f);
                if (config.CoastStopSpeedKph > 0f && speedKph < config.CoastStopSpeedKph)
                {
                    var settleBlend = Clamp(1f - (speedKph / config.CoastStopSpeedKph), 0f, 1f);
                    decelMps2 += (config.CoastStopDecelKphps / 3.6f) * settleBlend;
                }
            }

            return Math.Max(0f, decelMps2 * 3.6f);
        }

        private static float DriveAccelCore(
            Config config,
            int gear,
            bool inReverse,
            float speedMps,
            float throttle,
            float surfaceTractionModifier,
            float longitudinalGripFactor,
            float? driveRatioOverride = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var clampedThrottle = Clamp(throttle, 0f, 1f);
            if (clampedThrottle <= 0f)
                return 0f;

            var driveRpm = DriveRpm(config, gear, speedMps, clampedThrottle, inReverse, driveRatioOverride);
            var engineTorque = EngineTorque(config, driveRpm) * clampedThrottle * config.PowerFactor;
            var ratio = inReverse
                ? config.ReverseGearRatio
                : (driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                    ? driveRatioOverride.Value
                    : config.GetGearRatio(gear));
            var wheelTorque = engineTorque * ratio * config.FinalDriveRatio * config.DrivetrainEfficiency;
            var wheelForce = wheelTorque / config.WheelRadiusM;
            var tractionLimit = config.TireGripCoefficient * surfaceTractionModifier * config.MassKg * Gravity;
            if (wheelForce > tractionLimit)
                wheelForce = tractionLimit;
            wheelForce *= Clamp(longitudinalGripFactor, 0f, 1f);
            if (inReverse)
                wheelForce *= config.ReversePowerFactor;

            var netForce = wheelForce - ResistiveForce(config, speedMps);
            return netForce / config.MassKg;
        }

        private static float RpmForRatio(Config config, float speedMps, float ratio)
        {
            var wheelCircumference = config.WheelRadiusM * TwoPi;
            if (wheelCircumference <= 0f || ratio <= 0f)
                return 0f;
            return (speedMps / wheelCircumference) * 60f * ratio * config.FinalDriveRatio;
        }

        private static float AerodynamicDragForce(Config config, float speedMps)
        {
            var nonNegativeSpeedMps = Math.Max(0f, speedMps);
            var airDensity = config.UseStrictEngineClutchModel ? config.AirDensityKgPerM3 : AirDensityKgPerM3;
            return 0.5f * airDensity * config.DragCoefficient * config.FrontalAreaM2 * nonNegativeSpeedMps * nonNegativeSpeedMps;
        }

        private static float RollingResistanceForce(Config config, float speedMps, float surfaceDecelerationModifier)
        {
            var nonNegativeSpeed = Math.Max(0f, speedMps);
            var crr = config.RollingResistanceCoefficient;
            if (config.UseStrictEngineClutchModel)
            {
                crr *= 1f + (config.RollingResistanceSpeedGainPerMps * nonNegativeSpeed);
                if (crr < 0f)
                    crr = 0f;
                return crr * config.MassKg * Gravity;
            }

            var surfaceFactor = Math.Max(0f, surfaceDecelerationModifier);
            return crr * config.MassKg * Gravity * surfaceFactor;
        }

        private static float DrivelineCoastForce(Config config, float currentEngineRpm)
        {
            if (!config.UseStrictEngineClutchModel || config.WheelRadiusM <= 0f)
                return 0f;

            var omega = Math.Max(0f, currentEngineRpm) * TwoPi / 60f;
            var torque = config.DrivelineCoastTorqueNm + (config.DrivelineCoastViscousNmPerRadS * omega);
            if (torque <= 0f)
                return 0f;
            return torque / config.WheelRadiusM;
        }

        private static float BlendCompressionFactor(float rpmFactor)
        {
            var clamped = Clamp(rpmFactor, 0f, 1f);
            return CompressionRpmFactorFloor + ((1f - CompressionRpmFactorFloor) * clamped);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
