using System;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;

namespace TopSpeed.Data
{
    internal sealed class VehicleParameters
    {
        private readonly string?[] _sounds = new string?[7];

        public string? GetSoundPath(VehicleAction action) => _sounds[(int)action];

        public string Name { get; }
        public int HasWipers { get; }
        public float SurfaceTractionFactor { get; }
        public float Deceleration { get; }
        public float TopSpeed { get; }
        public int IdleFreq { get; }
        public int TopFreq { get; }
        public int ShiftFreq { get; }
        public float PitchCurveExponent { get; }
        public int Gears { get; }
        public float Steering { get; }
        public TransmissionType PrimaryTransmissionType { get; }
        public TransmissionType[] SupportedTransmissionTypes { get; }
        public bool ShiftOnDemand { get; }
        public AutomaticDrivelineTuning AutomaticTuning { get; }
        // Engine simulation parameters
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
        public float EngineInertiaKgm2 { get; }
        public float EngineFrictionTorqueNm { get; }
        public float DrivelineCouplingRate { get; }
        public float FinalDriveRatio { get; }
        public float ReverseMaxSpeedKph { get; }
        public float ReversePowerFactor { get; }
        public float ReverseGearRatio { get; }
        public float TireCircumferenceM { get; }
        public float LateralGripCoefficient { get; }
        public float HighSpeedStability { get; }
        public float WheelbaseM { get; }
        public float MaxSteerDeg { get; }
        public float HighSpeedSteerGain { get; }
        public float HighSpeedSteerStartKph { get; }
        public float HighSpeedSteerFullKph { get; }
        public float CombinedGripPenalty { get; }
        public float SlipAnglePeakDeg { get; }
        public float SlipAngleFalloff { get; }
        public float TurnResponse { get; }
        public float MassSensitivity { get; }
        public float DownforceGripGain { get; }
        public float CornerStiffnessFront { get; }
        public float CornerStiffnessRear { get; }
        public float YawInertiaScale { get; }
        public float SteeringCurve { get; }
        public float TransientDamping { get; }
        public float WidthM { get; }
        public float LengthM { get; }
        public float PowerFactor { get; }
        public float[]? GearRatios { get; }
        public float[]? TorqueCurveRpm { get; }
        public float[]? TorqueCurveTorqueNm { get; }
        public string? TorqueCurvePreset { get; }
        public float BrakeStrength { get; }
        public TransmissionPolicy TransmissionPolicy { get; }
        public string PhysicsModel { get; }
        public float EngineFrictionCoulombNm { get; }
        public float EngineFrictionViscousNmPerRadS { get; }
        public float EnginePumpingLossNmAtClosedThrottle { get; }
        public float EngineAccessoryTorqueNm { get; }
        public float IdleTargetRpm { get; }
        public float IdleMaxCorrectionTorqueNm { get; }
        public float IdleControlKp { get; }
        public float IdleControlKi { get; }
        public float ClutchCapacityNm { get; }
        public float ClutchEngageRatePerS { get; }
        public float ClutchReleaseRatePerS { get; }
        public float ClutchDragTorqueNm { get; }
        public float LaunchTargetSlipRpm { get; }
        public float AirDensityKgPerM3 { get; }
        public float RollingResistanceSpeedGainPerMps { get; }
        public float DrivelineCoastTorqueNm { get; }
        public float DrivelineCoastViscousNmPerRadS { get; }
        public float CoastStopSpeedKph { get; }
        public float CoastStopDecelKphps { get; }

        public VehicleParameters(
            string name,
            string? engineSound,
            string? startSound,
            string? hornSound,
            string? throttleSound,
            string? crashSound,
            string? brakeSound,
            string? backfireSound,
            int hasWipers,
            float surfaceTractionFactor,
            float deceleration,
            float topSpeed,
            int idleFreq,
            int topFreq,
            int shiftFreq,
            int gears,
            float steering,
            TransmissionType primaryTransmissionType,
            TransmissionType[] supportedTransmissionTypes,
            AutomaticDrivelineTuning automaticTuning,
            float pitchCurveExponent = VehicleDefinition.PitchCurveExponentDefault,
            float idleRpm = 800f,
            float maxRpm = 7000f,
            float revLimiter = 6500f,
            float autoShiftRpm = 0f,
            float engineBraking = 0.3f,
            float massKg = 1500f,
            float drivetrainEfficiency = 0.85f,
            float engineBrakingTorqueNm = 150f,
            float tireGripCoefficient = 0.9f,
            float peakTorqueNm = 200f,
            float peakTorqueRpm = 4000f,
            float idleTorqueNm = 60f,
            float redlineTorqueNm = 140f,
            float dragCoefficient = 0.30f,
            float frontalAreaM2 = 2.2f,
            float rollingResistanceCoefficient = 0.015f,
            float launchRpm = 1800f,
            float engineInertiaKgm2 = 0.24f,
            float engineFrictionTorqueNm = 20f,
            float drivelineCouplingRate = 12f,
            float finalDriveRatio = 3.5f,
            float reverseMaxSpeedKph = 35f,
            float reversePowerFactor = 0.55f,
            float reverseGearRatio = 3.2f,
            float tireCircumferenceM = 2.0f,
            float lateralGripCoefficient = 1.0f,
            float highSpeedStability = 0.0f,
            float wheelbaseM = 2.7f,
            float maxSteerDeg = 35f,
            float highSpeedSteerGain = 1.08f,
            float highSpeedSteerStartKph = 140f,
            float highSpeedSteerFullKph = 240f,
            float combinedGripPenalty = 0.72f,
            float slipAnglePeakDeg = 8f,
            float slipAngleFalloff = 1.25f,
            float turnResponse = 1.0f,
            float massSensitivity = 0.75f,
            float downforceGripGain = 0.05f,
            float cornerStiffnessFront = 1.0f,
            float cornerStiffnessRear = 1.0f,
            float yawInertiaScale = 1.0f,
            float steeringCurve = 1.0f,
            float transientDamping = 1.0f,
            float widthM = 1.8f,
            float lengthM = 4.5f,
            float powerFactor = 0.5f,
            float[]? gearRatios = null,
            float[]? torqueCurveRpm = null,
            float[]? torqueCurveTorqueNm = null,
            string? torqueCurvePreset = null,
            float brakeStrength = 1.0f,
            TransmissionPolicy? transmissionPolicy = null,
            bool shiftOnDemand = false,
            string physicsModel = VehicleDefinition.LegacyPhysicsModel,
            float engineFrictionCoulombNm = 20f,
            float engineFrictionViscousNmPerRadS = 0.01f,
            float enginePumpingLossNmAtClosedThrottle = 70f,
            float engineAccessoryTorqueNm = 8f,
            float idleTargetRpm = 800f,
            float idleMaxCorrectionTorqueNm = 160f,
            float idleControlKp = 0.08f,
            float idleControlKi = 0.22f,
            float clutchCapacityNm = 1200f,
            float clutchEngageRatePerS = 12f,
            float clutchReleaseRatePerS = 18f,
            float clutchDragTorqueNm = 30f,
            float launchTargetSlipRpm = 350f,
            float airDensityKgPerM3 = 1.225f,
            float rollingResistanceSpeedGainPerMps = 0f,
            float drivelineCoastTorqueNm = 0f,
            float drivelineCoastViscousNmPerRadS = 0f,
            float coastStopSpeedKph = 3f,
            float coastStopDecelKphps = 0.7f)
        {
            Name = name;
            _sounds[(int)VehicleAction.Engine] = engineSound;
            _sounds[(int)VehicleAction.Start] = startSound;
            _sounds[(int)VehicleAction.Horn] = hornSound;
            _sounds[(int)VehicleAction.Throttle] = throttleSound;
            _sounds[(int)VehicleAction.Crash] = crashSound;
            _sounds[(int)VehicleAction.Brake] = brakeSound;
            _sounds[(int)VehicleAction.Backfire] = backfireSound;

            HasWipers = hasWipers;
            SurfaceTractionFactor = surfaceTractionFactor;
            Deceleration = deceleration;
            TopSpeed = topSpeed;
            IdleFreq = idleFreq;
            TopFreq = topFreq;
            ShiftFreq = shiftFreq;
            PitchCurveExponent = VehicleDefinition.ClampPitchCurveExponent(pitchCurveExponent);
            Gears = gears;
            Steering = steering;
            var normalizedSupportedTypes = supportedTransmissionTypes ?? Array.Empty<TransmissionType>();
            if (!TransmissionTypes.TryValidate(primaryTransmissionType, normalizedSupportedTypes, out var validationError))
                throw new ArgumentException(validationError, nameof(supportedTransmissionTypes));
            PrimaryTransmissionType = primaryTransmissionType;
            SupportedTransmissionTypes = (TransmissionType[])normalizedSupportedTypes.Clone();
            ShiftOnDemand = shiftOnDemand;
            AutomaticTuning = automaticTuning;
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
            EngineInertiaKgm2 = engineInertiaKgm2;
            EngineFrictionTorqueNm = engineFrictionTorqueNm;
            DrivelineCouplingRate = drivelineCouplingRate;
            FinalDriveRatio = finalDriveRatio;
            ReverseMaxSpeedKph = reverseMaxSpeedKph;
            ReversePowerFactor = reversePowerFactor;
            ReverseGearRatio = reverseGearRatio;
            TireCircumferenceM = tireCircumferenceM;
            LateralGripCoefficient = lateralGripCoefficient;
            HighSpeedStability = highSpeedStability;
            WheelbaseM = wheelbaseM;
            MaxSteerDeg = maxSteerDeg;
            HighSpeedSteerGain = highSpeedSteerGain;
            HighSpeedSteerStartKph = highSpeedSteerStartKph;
            HighSpeedSteerFullKph = highSpeedSteerFullKph;
            CombinedGripPenalty = combinedGripPenalty;
            SlipAnglePeakDeg = slipAnglePeakDeg;
            SlipAngleFalloff = slipAngleFalloff;
            TurnResponse = turnResponse;
            MassSensitivity = massSensitivity;
            DownforceGripGain = downforceGripGain;
            CornerStiffnessFront = cornerStiffnessFront;
            CornerStiffnessRear = cornerStiffnessRear;
            YawInertiaScale = yawInertiaScale;
            SteeringCurve = steeringCurve;
            TransientDamping = transientDamping;
            WidthM = widthM;
            LengthM = lengthM;
            PowerFactor = powerFactor;
            GearRatios = gearRatios;
            TorqueCurveRpm = torqueCurveRpm;
            TorqueCurveTorqueNm = torqueCurveTorqueNm;
            TorqueCurvePreset = torqueCurvePreset;
            BrakeStrength = brakeStrength;
            TransmissionPolicy = transmissionPolicy ?? TransmissionPolicy.Default;
            PhysicsModel = string.IsNullOrWhiteSpace(physicsModel) ? VehicleDefinition.LegacyPhysicsModel : physicsModel;
            EngineFrictionCoulombNm = engineFrictionCoulombNm;
            EngineFrictionViscousNmPerRadS = engineFrictionViscousNmPerRadS;
            EnginePumpingLossNmAtClosedThrottle = enginePumpingLossNmAtClosedThrottle;
            EngineAccessoryTorqueNm = engineAccessoryTorqueNm;
            IdleTargetRpm = idleTargetRpm;
            IdleMaxCorrectionTorqueNm = idleMaxCorrectionTorqueNm;
            IdleControlKp = idleControlKp;
            IdleControlKi = idleControlKi;
            ClutchCapacityNm = clutchCapacityNm;
            ClutchEngageRatePerS = clutchEngageRatePerS;
            ClutchReleaseRatePerS = clutchReleaseRatePerS;
            ClutchDragTorqueNm = clutchDragTorqueNm;
            LaunchTargetSlipRpm = launchTargetSlipRpm;
            AirDensityKgPerM3 = airDensityKgPerM3;
            RollingResistanceSpeedGainPerMps = rollingResistanceSpeedGainPerMps;
            DrivelineCoastTorqueNm = drivelineCoastTorqueNm;
            DrivelineCoastViscousNmPerRadS = drivelineCoastViscousNmPerRadS;
            CoastStopSpeedKph = coastStopSpeedKph;
            CoastStopDecelKphps = coastStopDecelKphps;
        }
    }

    internal static class VehicleCatalog
    {
        public const int VehicleCount = OfficialVehicleCatalog.VehicleCount;

        public static readonly VehicleParameters[] Vehicles = Build();

        private static VehicleParameters[] Build()
        {
            var specs = OfficialVehicleCatalog.Vehicles;
            var result = new VehicleParameters[specs.Length];
            for (var i = 0; i < specs.Length; i++)
                result[i] = FromSpec(specs[i]);
            return result;
        }

        private static VehicleParameters FromSpec(OfficialVehicleSpec spec)
        {
            return new VehicleParameters(
                spec.Name, null, null, null, null, null, null, null,
                spec.HasWipers,
                spec.SurfaceTractionFactor,
                spec.Deceleration,
                spec.TopSpeed,
                spec.IdleFreq,
                spec.TopFreq,
                spec.ShiftFreq,
                spec.Gears,
                spec.Steering,
                spec.PrimaryTransmissionType,
                spec.SupportedTransmissionTypes,
                spec.AutomaticTuning,
                VehicleDefinition.PitchCurveExponentDefault,
                spec.IdleRpm,
                spec.MaxRpm,
                spec.RevLimiter,
                spec.AutoShiftRpm,
                spec.EngineBraking,
                spec.MassKg,
                spec.DrivetrainEfficiency,
                spec.EngineBrakingTorqueNm,
                spec.TireGripCoefficient,
                spec.PeakTorqueNm,
                spec.PeakTorqueRpm,
                spec.IdleTorqueNm,
                spec.RedlineTorqueNm,
                spec.DragCoefficient,
                spec.FrontalAreaM2,
                spec.RollingResistanceCoefficient,
                spec.LaunchRpm,
                spec.EngineInertiaKgm2,
                spec.EngineFrictionTorqueNm,
                spec.DrivelineCouplingRate,
                spec.FinalDriveRatio,
                spec.ReverseMaxSpeedKph,
                spec.ReversePowerFactor,
                spec.ReverseGearRatio,
                spec.TireCircumferenceM,
                spec.LateralGripCoefficient,
                spec.HighSpeedStability,
                spec.WheelbaseM,
                spec.MaxSteerDeg,
                spec.HighSpeedSteerGain,
                spec.HighSpeedSteerStartKph,
                spec.HighSpeedSteerFullKph,
                spec.CombinedGripPenalty,
                spec.SlipAnglePeakDeg,
                spec.SlipAngleFalloff,
                spec.TurnResponse,
                spec.MassSensitivity,
                spec.DownforceGripGain,
                spec.CornerStiffnessFront,
                spec.CornerStiffnessRear,
                spec.YawInertiaScale,
                spec.SteeringCurve,
                spec.TransientDamping,
                spec.WidthM,
                spec.LengthM,
                spec.PowerFactor,
                spec.GearRatios,
                spec.TorqueCurveRpm,
                spec.TorqueCurveTorqueNm,
                spec.TorqueCurvePreset,
                spec.BrakeStrength,
                spec.TransmissionPolicy,
                spec.ShiftOnDemand,
                spec.PhysicsModel,
                spec.EngineFrictionCoulombNm,
                spec.EngineFrictionViscousNmPerRadS,
                spec.EnginePumpingLossNmAtClosedThrottle,
                spec.EngineAccessoryTorqueNm,
                spec.IdleTargetRpm,
                spec.IdleMaxCorrectionTorqueNm,
                spec.IdleControlKp,
                spec.IdleControlKi,
                spec.ClutchCapacityNm,
                spec.ClutchEngageRatePerS,
                spec.ClutchReleaseRatePerS,
                spec.ClutchDragTorqueNm,
                spec.LaunchTargetSlipRpm,
                spec.AirDensityKgPerM3,
                spec.RollingResistanceSpeedGainPerMps,
                spec.DrivelineCoastTorqueNm,
                spec.DrivelineCoastViscousNmPerRadS,
                spec.CoastStopSpeedKph,
                spec.CoastStopDecelKphps);
        }
    }
}


