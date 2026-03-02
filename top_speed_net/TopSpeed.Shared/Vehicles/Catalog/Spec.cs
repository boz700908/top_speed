using TopSpeed.Protocol;

namespace TopSpeed.Vehicles
{
    public sealed class OfficialVehicleSpec
    {
        public OfficialVehicleSpec(
            CarType carType,
            string name,
            int hasWipers,
            float surfaceTractionFactor,
            float deceleration,
            float topSpeed,
            int idleFreq,
            int topFreq,
            int shiftFreq,
            int gears,
            float steering,
            float idleRpm,
            float maxRpm,
            float revLimiter,
            float autoShiftRpm,
            float engineBraking,
            float massKg,
            float drivetrainEfficiency,
            float engineBrakingTorqueNm,
            float tireGripCoefficient,
            float peakTorqueNm,
            float peakTorqueRpm,
            float idleTorqueNm,
            float redlineTorqueNm,
            float dragCoefficient,
            float frontalAreaM2,
            float rollingResistanceCoefficient,
            float launchRpm,
            float finalDriveRatio,
            float reverseMaxSpeedKph,
            float reversePowerFactor,
            float reverseGearRatio,
            float tireCircumferenceM,
            float lateralGripCoefficient,
            float highSpeedStability,
            float wheelbaseM,
            float maxSteerDeg,
            float widthM,
            float lengthM,
            float powerFactor,
            float[] gearRatios,
            float brakeStrength,
            TransmissionPolicy? transmissionPolicy = null)
        {
            CarType = carType;
            Name = name;
            HasWipers = hasWipers;
            SurfaceTractionFactor = surfaceTractionFactor;
            Deceleration = deceleration;
            TopSpeed = topSpeed;
            IdleFreq = idleFreq;
            TopFreq = topFreq;
            ShiftFreq = shiftFreq;
            Gears = gears;
            Steering = steering;
            IdleRpm = idleRpm;
            MaxRpm = maxRpm;
            RevLimiter = revLimiter;
            AutoShiftRpm = autoShiftRpm;
            EngineBraking = engineBraking;
            MassKg = massKg;
            DrivetrainEfficiency = drivetrainEfficiency;
            EngineBrakingTorqueNm = engineBrakingTorqueNm;
            TireGripCoefficient = tireGripCoefficient;
            PeakTorqueNm = peakTorqueNm;
            PeakTorqueRpm = peakTorqueRpm;
            IdleTorqueNm = idleTorqueNm;
            RedlineTorqueNm = redlineTorqueNm;
            DragCoefficient = dragCoefficient;
            FrontalAreaM2 = frontalAreaM2;
            RollingResistanceCoefficient = rollingResistanceCoefficient;
            LaunchRpm = launchRpm;
            FinalDriveRatio = finalDriveRatio;
            ReverseMaxSpeedKph = reverseMaxSpeedKph;
            ReversePowerFactor = reversePowerFactor;
            ReverseGearRatio = reverseGearRatio;
            TireCircumferenceM = tireCircumferenceM;
            LateralGripCoefficient = lateralGripCoefficient;
            HighSpeedStability = highSpeedStability;
            WheelbaseM = wheelbaseM;
            MaxSteerDeg = maxSteerDeg;
            WidthM = widthM;
            LengthM = lengthM;
            PowerFactor = powerFactor;
            GearRatios = gearRatios;
            BrakeStrength = brakeStrength;
            TransmissionPolicy = transmissionPolicy ?? TransmissionPolicy.Default;
        }

        public CarType CarType { get; }
        public string Name { get; }
        public int HasWipers { get; }
        public float SurfaceTractionFactor { get; }
        public float Deceleration { get; }
        public float TopSpeed { get; }
        public int IdleFreq { get; }
        public int TopFreq { get; }
        public int ShiftFreq { get; }
        public int Gears { get; }
        public float Steering { get; }
        public float IdleRpm { get; }
        public float MaxRpm { get; }
        public float RevLimiter { get; }
        public float AutoShiftRpm { get; }
        public float EngineBraking { get; }
        public float MassKg { get; }
        public float DrivetrainEfficiency { get; }
        public float EngineBrakingTorqueNm { get; }
        public float TireGripCoefficient { get; }
        public float PeakTorqueNm { get; }
        public float PeakTorqueRpm { get; }
        public float IdleTorqueNm { get; }
        public float RedlineTorqueNm { get; }
        public float DragCoefficient { get; }
        public float FrontalAreaM2 { get; }
        public float RollingResistanceCoefficient { get; }
        public float LaunchRpm { get; }
        public float FinalDriveRatio { get; }
        public float ReverseMaxSpeedKph { get; }
        public float ReversePowerFactor { get; }
        public float ReverseGearRatio { get; }
        public float TireCircumferenceM { get; }
        public float LateralGripCoefficient { get; }
        public float HighSpeedStability { get; }
        public float WheelbaseM { get; }
        public float MaxSteerDeg { get; }
        public float WidthM { get; }
        public float LengthM { get; }
        public float PowerFactor { get; }
        public float[] GearRatios { get; }
        public float BrakeStrength { get; }
        public TransmissionPolicy TransmissionPolicy { get; }
    }
}
