using System;
using TopSpeed.Vehicles;

namespace TopSpeed.Bots
{
    public sealed class BotPhysicsConfig
    {
        public BotPhysicsConfig(
            float surfaceTractionFactor,
            float deceleration,
            float topSpeedKph,
            float massKg,
            float drivetrainEfficiency,
            float engineBrakingTorqueNm,
            float tireGripCoefficient,
            float brakeStrength,
            float wheelRadiusM,
            float engineBraking,
            float idleRpm,
            float revLimiter,
            float finalDriveRatio,
            float powerFactor,
            float peakTorqueNm,
            float peakTorqueRpm,
            float idleTorqueNm,
            float redlineTorqueNm,
            float dragCoefficient,
            float frontalAreaM2,
            float rollingResistanceCoefficient,
            float launchRpm,
            float lateralGripCoefficient,
            float highSpeedStability,
            float wheelbaseM,
            float maxSteerDeg,
            float steering,
            int gears,
            float[]? gearRatios = null,
            TransmissionPolicy? transmissionPolicy = null)
        {
            SurfaceTractionFactor = Math.Max(0.01f, surfaceTractionFactor);
            Deceleration = Math.Max(0.01f, deceleration);
            TopSpeedKph = Math.Max(1f, topSpeedKph);
            MassKg = Math.Max(1f, massKg);
            DrivetrainEfficiency = Math.Max(0.1f, Math.Min(1.0f, drivetrainEfficiency));
            EngineBrakingTorqueNm = Math.Max(0f, engineBrakingTorqueNm);
            TireGripCoefficient = Math.Max(0.1f, tireGripCoefficient);
            BrakeStrength = Math.Max(0.1f, brakeStrength);
            WheelRadiusM = Math.Max(0.01f, wheelRadiusM);
            EngineBraking = Math.Max(0.05f, Math.Min(1.0f, engineBraking));
            IdleRpm = Math.Max(500f, idleRpm);
            RevLimiter = Math.Max(IdleRpm, revLimiter);
            FinalDriveRatio = Math.Max(0.1f, finalDriveRatio);
            PowerFactor = Math.Max(0.1f, powerFactor);
            PeakTorqueNm = Math.Max(0f, peakTorqueNm);
            PeakTorqueRpm = Math.Max(IdleRpm + 100f, peakTorqueRpm);
            IdleTorqueNm = Math.Max(0f, idleTorqueNm);
            RedlineTorqueNm = Math.Max(0f, redlineTorqueNm);
            DragCoefficient = Math.Max(0.01f, dragCoefficient);
            FrontalAreaM2 = Math.Max(0.1f, frontalAreaM2);
            RollingResistanceCoefficient = Math.Max(0.001f, rollingResistanceCoefficient);
            LaunchRpm = Math.Max(IdleRpm, Math.Min(RevLimiter, launchRpm));
            LateralGripCoefficient = Math.Max(0.1f, lateralGripCoefficient);
            HighSpeedStability = Math.Max(0f, Math.Min(1.0f, highSpeedStability));
            WheelbaseM = Math.Max(0.5f, wheelbaseM);
            MaxSteerDeg = Math.Max(5f, Math.Min(60f, maxSteerDeg));
            Steering = steering;
            Gears = Math.Max(1, gears);
            GearRatios = BuildRatios(Gears, gearRatios);
            TransmissionPolicy = transmissionPolicy ?? TransmissionPolicy.Default;
        }

        public float SurfaceTractionFactor { get; }
        public float Deceleration { get; }
        public float TopSpeedKph { get; }
        public float MassKg { get; }
        public float DrivetrainEfficiency { get; }
        public float EngineBrakingTorqueNm { get; }
        public float TireGripCoefficient { get; }
        public float BrakeStrength { get; }
        public float WheelRadiusM { get; }
        public float EngineBraking { get; }
        public float IdleRpm { get; }
        public float RevLimiter { get; }
        public float FinalDriveRatio { get; }
        public float PowerFactor { get; }
        public float PeakTorqueNm { get; }
        public float PeakTorqueRpm { get; }
        public float IdleTorqueNm { get; }
        public float RedlineTorqueNm { get; }
        public float DragCoefficient { get; }
        public float FrontalAreaM2 { get; }
        public float RollingResistanceCoefficient { get; }
        public float LaunchRpm { get; }
        public float LateralGripCoefficient { get; }
        public float HighSpeedStability { get; }
        public float WheelbaseM { get; }
        public float MaxSteerDeg { get; }
        public float Steering { get; }
        public int Gears { get; }
        public float[] GearRatios { get; }
        public TransmissionPolicy TransmissionPolicy { get; }

        public float GetGearRatio(int gear)
        {
            var clamped = Math.Max(1, Math.Min(Gears, gear));
            return GearRatios[clamped - 1];
        }

        private static float[] BuildRatios(int gears, float[]? provided)
        {
            if (provided != null && provided.Length == gears)
                return provided;

            var ratios = new float[gears];
            const float first = 3.5f;
            const float last = 0.85f;
            var logFirst = Math.Log(first);
            var logLast = Math.Log(last);
            for (var i = 0; i < gears; i++)
            {
                var t = gears > 1 ? i / (float)(gears - 1) : 0f;
                ratios[i] = (float)Math.Exp(logFirst + ((logLast - logFirst) * t));
            }

            return ratios;
        }
    }
}
