namespace TopSpeed.Vehicles
{
    public readonly struct AtcDrivelineTuning
    {
        public AtcDrivelineTuning(
            float creepAccelKphPerSecond,
            float launchCouplingMin,
            float launchCouplingMax,
            float lockSpeedKph,
            float lockThrottleMin,
            float shiftReleaseCoupling,
            float engageRate,
            float disengageRate)
        {
            CreepAccelKphPerSecond = creepAccelKphPerSecond;
            LaunchCouplingMin = launchCouplingMin;
            LaunchCouplingMax = launchCouplingMax;
            LockSpeedKph = lockSpeedKph;
            LockThrottleMin = lockThrottleMin;
            ShiftReleaseCoupling = shiftReleaseCoupling;
            EngageRate = engageRate;
            DisengageRate = disengageRate;
        }

        public float CreepAccelKphPerSecond { get; }
        public float LaunchCouplingMin { get; }
        public float LaunchCouplingMax { get; }
        public float LockSpeedKph { get; }
        public float LockThrottleMin { get; }
        public float ShiftReleaseCoupling { get; }
        public float EngageRate { get; }
        public float DisengageRate { get; }
    }

    public readonly struct DctDrivelineTuning
    {
        public DctDrivelineTuning(
            float launchCouplingMin,
            float launchCouplingMax,
            float lockSpeedKph,
            float lockThrottleMin,
            float shiftOverlapCoupling,
            float engageRate,
            float disengageRate)
        {
            LaunchCouplingMin = launchCouplingMin;
            LaunchCouplingMax = launchCouplingMax;
            LockSpeedKph = lockSpeedKph;
            LockThrottleMin = lockThrottleMin;
            ShiftOverlapCoupling = shiftOverlapCoupling;
            EngageRate = engageRate;
            DisengageRate = disengageRate;
        }

        public float LaunchCouplingMin { get; }
        public float LaunchCouplingMax { get; }
        public float LockSpeedKph { get; }
        public float LockThrottleMin { get; }
        public float ShiftOverlapCoupling { get; }
        public float EngageRate { get; }
        public float DisengageRate { get; }
    }

    public readonly struct CvtDrivelineTuning
    {
        public CvtDrivelineTuning(
            float ratioMin,
            float ratioMax,
            float targetRpmLow,
            float targetRpmHigh,
            float ratioChangeRate,
            float launchCouplingMin,
            float launchCouplingMax,
            float lockSpeedKph,
            float lockThrottleMin,
            float creepAccelKphPerSecond,
            float shiftHoldCoupling,
            float engageRate,
            float disengageRate)
        {
            RatioMin = ratioMin;
            RatioMax = ratioMax;
            TargetRpmLow = targetRpmLow;
            TargetRpmHigh = targetRpmHigh;
            RatioChangeRate = ratioChangeRate;
            LaunchCouplingMin = launchCouplingMin;
            LaunchCouplingMax = launchCouplingMax;
            LockSpeedKph = lockSpeedKph;
            LockThrottleMin = lockThrottleMin;
            CreepAccelKphPerSecond = creepAccelKphPerSecond;
            ShiftHoldCoupling = shiftHoldCoupling;
            EngageRate = engageRate;
            DisengageRate = disengageRate;
        }

        public float RatioMin { get; }
        public float RatioMax { get; }
        public float TargetRpmLow { get; }
        public float TargetRpmHigh { get; }
        public float RatioChangeRate { get; }
        public float LaunchCouplingMin { get; }
        public float LaunchCouplingMax { get; }
        public float LockSpeedKph { get; }
        public float LockThrottleMin { get; }
        public float CreepAccelKphPerSecond { get; }
        public float ShiftHoldCoupling { get; }
        public float EngageRate { get; }
        public float DisengageRate { get; }
    }

    public readonly struct AutomaticDrivelineTuning
    {
        public static AutomaticDrivelineTuning Default => new AutomaticDrivelineTuning(
            new AtcDrivelineTuning(
                creepAccelKphPerSecond: 0.70f,
                launchCouplingMin: 0.22f,
                launchCouplingMax: 0.80f,
                lockSpeedKph: 38f,
                lockThrottleMin: 0.30f,
                shiftReleaseCoupling: 0.38f,
                engageRate: 3.5f,
                disengageRate: 6.0f),
            new DctDrivelineTuning(
                launchCouplingMin: 0.30f,
                launchCouplingMax: 0.92f,
                lockSpeedKph: 18f,
                lockThrottleMin: 0.15f,
                shiftOverlapCoupling: 0.45f,
                engageRate: 8.0f,
                disengageRate: 14.0f),
            new CvtDrivelineTuning(
                ratioMin: 0.45f,
                ratioMax: 3.40f,
                targetRpmLow: 1700f,
                targetRpmHigh: 4200f,
                ratioChangeRate: 4.5f,
                launchCouplingMin: 0.24f,
                launchCouplingMax: 0.85f,
                lockSpeedKph: 24f,
                lockThrottleMin: 0.12f,
                creepAccelKphPerSecond: 0.70f,
                shiftHoldCoupling: 0.75f,
                engageRate: 4.5f,
                disengageRate: 8.5f));

        public AutomaticDrivelineTuning(
            AtcDrivelineTuning atc,
            DctDrivelineTuning dct,
            CvtDrivelineTuning cvt)
        {
            Atc = atc;
            Dct = dct;
            Cvt = cvt;
        }

        public AtcDrivelineTuning Atc { get; }
        public DctDrivelineTuning Dct { get; }
        public CvtDrivelineTuning Cvt { get; }
    }
}
