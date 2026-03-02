using System;

namespace TopSpeed.Vehicles
{
    public sealed class TransmissionPolicy
    {
        private readonly float[]? _upshiftCooldownBySourceGear;

        public static TransmissionPolicy Default { get; } = new TransmissionPolicy();

        public TransmissionPolicy(
            int intendedTopSpeedGear = 0,
            bool allowOverdriveAboveGameTopSpeed = false,
            float upshiftRpmFraction = 0.92f,
            float downshiftRpmFraction = 0.35f,
            float upshiftHysteresis = 0.05f,
            float baseAutoShiftCooldownSeconds = 0.15f,
            float minUpshiftNetAccelerationMps2 = -0.05f,
            float topSpeedPursuitSpeedFraction = 0.97f,
            bool preferIntendedTopSpeedGearNearLimit = true,
            float[]? upshiftCooldownBySourceGear = null)
        {
            IntendedTopSpeedGear = Math.Max(0, intendedTopSpeedGear);
            AllowOverdriveAboveGameTopSpeed = allowOverdriveAboveGameTopSpeed;
            UpshiftRpmFraction = Clamp(upshiftRpmFraction, 0.05f, 1.0f);
            DownshiftRpmFraction = Clamp(downshiftRpmFraction, 0.05f, 0.95f);
            UpshiftHysteresis = Clamp(upshiftHysteresis, 0f, 2f);
            BaseAutoShiftCooldownSeconds = Clamp(baseAutoShiftCooldownSeconds, 0f, 2f);
            MinUpshiftNetAccelerationMps2 = Clamp(minUpshiftNetAccelerationMps2, -20f, 20f);
            TopSpeedPursuitSpeedFraction = Clamp(topSpeedPursuitSpeedFraction, 0.50f, 1.20f);
            PreferIntendedTopSpeedGearNearLimit = preferIntendedTopSpeedGearNearLimit;
            _upshiftCooldownBySourceGear = upshiftCooldownBySourceGear;
        }

        public int IntendedTopSpeedGear { get; }
        public bool AllowOverdriveAboveGameTopSpeed { get; }
        public float UpshiftRpmFraction { get; }
        public float DownshiftRpmFraction { get; }
        public float UpshiftHysteresis { get; }
        public float BaseAutoShiftCooldownSeconds { get; }
        public float MinUpshiftNetAccelerationMps2 { get; }
        public float TopSpeedPursuitSpeedFraction { get; }
        public bool PreferIntendedTopSpeedGearNearLimit { get; }

        public float ResolveUpshiftRpm(float idleRpm, float revLimiter)
        {
            if (revLimiter <= idleRpm)
                return revLimiter;
            return idleRpm + ((revLimiter - idleRpm) * UpshiftRpmFraction);
        }

        public float ResolveDownshiftRpm(float idleRpm, float revLimiter)
        {
            if (revLimiter <= idleRpm)
                return idleRpm;
            return idleRpm + ((revLimiter - idleRpm) * DownshiftRpmFraction);
        }

        public int ResolveIntendedTopSpeedGear(int maxGears)
        {
            if (maxGears <= 0)
                return 1;
            if (IntendedTopSpeedGear <= 0)
                return maxGears;
            return Math.Max(1, Math.Min(maxGears, IntendedTopSpeedGear));
        }

        public float GetUpshiftCooldownSeconds(int sourceGear, int maxGears)
        {
            var fallback = BaseAutoShiftCooldownSeconds;
            if (_upshiftCooldownBySourceGear == null || _upshiftCooldownBySourceGear.Length == 0)
                return fallback;

            var clamped = Math.Max(1, Math.Min(maxGears, sourceGear));
            var idx = clamped - 1;
            if (idx < 0 || idx >= _upshiftCooldownBySourceGear.Length)
                return fallback;

            var configured = _upshiftCooldownBySourceGear[idx];
            if (configured <= 0f)
                return fallback;
            return Math.Max(fallback, configured);
        }

        public TransmissionPolicy WithUpshiftCooldownBySourceGear(float[]? upshiftCooldownBySourceGear)
        {
            return new TransmissionPolicy(
                IntendedTopSpeedGear,
                AllowOverdriveAboveGameTopSpeed,
                UpshiftRpmFraction,
                DownshiftRpmFraction,
                UpshiftHysteresis,
                BaseAutoShiftCooldownSeconds,
                MinUpshiftNetAccelerationMps2,
                TopSpeedPursuitSpeedFraction,
                PreferIntendedTopSpeedGearNearLimit,
                upshiftCooldownBySourceGear);
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
