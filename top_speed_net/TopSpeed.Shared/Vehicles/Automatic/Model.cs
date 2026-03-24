using System;

namespace TopSpeed.Vehicles
{
    public static class AutomaticDrivelineModel
    {
        public static AutomaticDrivelineOutput Step(
            TransmissionType transmissionType,
            in AutomaticDrivelineTuning tuning,
            in AutomaticDrivelineInput input,
            in AutomaticDrivelineState state)
        {
            var elapsed = Math.Max(0f, input.ElapsedSeconds);
            var speedKph = Math.Max(0f, input.SpeedMps * 3.6f);
            var throttle = Clamp01(input.Throttle);
            var brake = Clamp01(input.Brake);
            var currentCoupling = Clamp01(state.CouplingFactor);

            switch (transmissionType)
            {
                case TransmissionType.Atc:
                {
                    var target = ResolveAtcTargetCoupling(tuning.Atc, speedKph, throttle, input.Shifting);
                    var coupling = MoveToward(currentCoupling, target, elapsed, tuning.Atc.EngageRate, tuning.Atc.DisengageRate);
                    var creepMps2 = ResolveCreepAccelerationMps2(tuning.Atc.CreepAccelKphPerSecond, throttle, brake);
                    return new AutomaticDrivelineOutput(coupling, cvtRatio: 0f, effectiveDriveRatio: 0f, creepMps2);
                }

                case TransmissionType.Dct:
                {
                    var target = ResolveDctTargetCoupling(tuning.Dct, speedKph, throttle, input.Shifting);
                    var coupling = MoveToward(currentCoupling, target, elapsed, tuning.Dct.EngageRate, tuning.Dct.DisengageRate);
                    return new AutomaticDrivelineOutput(coupling, cvtRatio: 0f, effectiveDriveRatio: 0f, creepAccelerationMps2: 0f);
                }

                case TransmissionType.Cvt:
                {
                    var currentRatio = state.CvtRatio > 0f ? state.CvtRatio : tuning.Cvt.RatioMax;
                    currentRatio = Clamp(currentRatio, tuning.Cvt.RatioMin, tuning.Cvt.RatioMax);
                    var targetRatio = ResolveCvtTargetRatio(tuning.Cvt, input, throttle);
                    var nextRatio = MoveTowardValue(currentRatio, targetRatio, Math.Max(0.1f, tuning.Cvt.RatioChangeRate) * elapsed);
                    var targetCoupling = ResolveCvtTargetCoupling(tuning.Cvt, speedKph, throttle, input.Shifting);
                    var coupling = MoveToward(currentCoupling, targetCoupling, elapsed, tuning.Cvt.EngageRate, tuning.Cvt.DisengageRate);
                    var creepMps2 = ResolveCreepAccelerationMps2(tuning.Cvt.CreepAccelKphPerSecond, throttle, brake);
                    return new AutomaticDrivelineOutput(coupling, nextRatio, nextRatio, creepMps2);
                }

                default:
                    return new AutomaticDrivelineOutput(currentCoupling, state.CvtRatio, effectiveDriveRatio: 0f, creepAccelerationMps2: 0f);
            }
        }

        private static float ResolveAtcTargetCoupling(AtcDrivelineTuning tuning, float speedKph, float throttle, bool shifting)
        {
            if (shifting)
                return Clamp01(tuning.ShiftReleaseCoupling);
            if (speedKph < 2.5f)
                return Lerp(tuning.LaunchCouplingMin, tuning.LaunchCouplingMax, throttle);
            if (speedKph >= tuning.LockSpeedKph && throttle >= tuning.LockThrottleMin)
                return 1f;
            // Keep ATC below hard-lock until lock-speed criteria are met.
            return 0.82f + (0.14f * throttle);
        }

        private static float ResolveDctTargetCoupling(DctDrivelineTuning tuning, float speedKph, float throttle, bool shifting)
        {
            if (shifting)
                return Clamp01(tuning.ShiftOverlapCoupling);
            if (speedKph < 1.8f)
                return Lerp(tuning.LaunchCouplingMin, tuning.LaunchCouplingMax, throttle);
            if (speedKph >= tuning.LockSpeedKph && throttle >= tuning.LockThrottleMin)
                return 1f;
            return 0.95f + (0.05f * throttle);
        }

        private static float ResolveCvtTargetCoupling(CvtDrivelineTuning tuning, float speedKph, float throttle, bool shifting)
        {
            if (shifting)
                return Clamp01(tuning.ShiftHoldCoupling);
            if (speedKph < 2.2f)
                return Lerp(tuning.LaunchCouplingMin, tuning.LaunchCouplingMax, throttle);
            if (speedKph >= tuning.LockSpeedKph && throttle >= tuning.LockThrottleMin)
                return 1f;
            return 0.86f + (0.14f * throttle);
        }

        private static float ResolveCvtTargetRatio(CvtDrivelineTuning tuning, in AutomaticDrivelineInput input, float throttle)
        {
            if (input.SpeedMps <= 0.25f || input.WheelCircumferenceM <= 0.01f || input.FinalDriveRatio <= 0.01f)
                return tuning.RatioMax;

            var bandLow = Math.Max(input.IdleRpm, tuning.TargetRpmLow);
            var bandHigh = Math.Max(bandLow, Math.Min(input.RevLimiter, tuning.TargetRpmHigh));
            var desiredRpm = Lerp(bandLow, bandHigh, throttle);
            var targetRatio = (desiredRpm * input.WheelCircumferenceM) / (input.SpeedMps * 60f * input.FinalDriveRatio);
            return Clamp(targetRatio, tuning.RatioMin, tuning.RatioMax);
        }

        private static float ResolveCreepAccelerationMps2(float creepAccelKphPerSecond, float throttle, float brake)
        {
            if (creepAccelKphPerSecond <= 0f || throttle > 0.02f || brake > 0.05f)
                return 0f;
            return creepAccelKphPerSecond / 3.6f;
        }

        private static float MoveToward(float current, float target, float elapsed, float engageRate, float disengageRate)
        {
            var rate = target >= current ? engageRate : disengageRate;
            var maxDelta = Math.Max(0f, rate) * Math.Max(0f, elapsed);
            return MoveTowardValue(current, target, maxDelta);
        }

        private static float MoveTowardValue(float current, float target, float maxDelta)
        {
            if (maxDelta <= 0f || current == target)
                return current;
            if (current < target)
                return Math.Min(target, current + maxDelta);
            return Math.Max(target, current - maxDelta);
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + ((b - a) * Clamp01(t));
        }
    }
}
