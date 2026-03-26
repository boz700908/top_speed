namespace TopSpeed.Vehicles
{
    public readonly struct AutomaticDrivelineInput
    {
        public AutomaticDrivelineInput(
            float elapsedSeconds,
            float speedMps,
            float throttle,
            float brake,
            bool shifting,
            float wheelCircumferenceM,
            float finalDriveRatio,
            float idleRpm,
            float revLimiter,
            float launchRpm = 0f,
            float currentEngineRpm = 0f)
        {
            ElapsedSeconds = elapsedSeconds;
            SpeedMps = speedMps;
            Throttle = throttle;
            Brake = brake;
            Shifting = shifting;
            WheelCircumferenceM = wheelCircumferenceM;
            FinalDriveRatio = finalDriveRatio;
            IdleRpm = idleRpm;
            RevLimiter = revLimiter;
            LaunchRpm = launchRpm;
            CurrentEngineRpm = currentEngineRpm;
        }

        public float ElapsedSeconds { get; }
        public float SpeedMps { get; }
        public float Throttle { get; }
        public float Brake { get; }
        public bool Shifting { get; }
        public float WheelCircumferenceM { get; }
        public float FinalDriveRatio { get; }
        public float IdleRpm { get; }
        public float RevLimiter { get; }
        public float LaunchRpm { get; }
        public float CurrentEngineRpm { get; }
    }

    public readonly struct AutomaticDrivelineState
    {
        public AutomaticDrivelineState(float couplingFactor, float cvtRatio)
        {
            CouplingFactor = couplingFactor;
            CvtRatio = cvtRatio;
        }

        public float CouplingFactor { get; }
        public float CvtRatio { get; }
    }

    public readonly struct AutomaticDrivelineOutput
    {
        public AutomaticDrivelineOutput(
            float couplingFactor,
            float cvtRatio,
            float effectiveDriveRatio,
            float creepAccelerationMps2)
        {
            CouplingFactor = couplingFactor;
            CvtRatio = cvtRatio;
            EffectiveDriveRatio = effectiveDriveRatio;
            CreepAccelerationMps2 = creepAccelerationMps2;
        }

        public float CouplingFactor { get; }
        public float CvtRatio { get; }
        public float EffectiveDriveRatio { get; }
        public float CreepAccelerationMps2 { get; }
    }
}
