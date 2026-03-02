using System;

namespace TopSpeed.Bots
{
    public static partial class BotPhysics
    {
        private static float CalculateBrakeDecel(BotPhysicsConfig config, float brakeInput, float surfaceDecelMod)
        {
            if (brakeInput <= 0f)
                return 0f;
            var grip = Math.Max(0.1f, config.TireGripCoefficient * surfaceDecelMod);
            var decelMps2 = brakeInput * config.BrakeStrength * grip * 9.80665f;
            return decelMps2 * 3.6f;
        }

        private static float CalculateEngineBrakingDecel(BotPhysicsConfig config, int gear, float speedMps, float surfaceDecelMod)
        {
            if (config.EngineBrakingTorqueNm <= 0f || config.MassKg <= 0f || config.WheelRadiusM <= 0f)
                return 0f;

            var rpmRange = config.RevLimiter - config.IdleRpm;
            if (rpmRange <= 0f)
                return 0f;
            var rpm = SpeedToRpm(config, speedMps, gear);
            var rpmFactor = (rpm - config.IdleRpm) / rpmRange;
            if (rpmFactor <= 0f)
                return 0f;
            rpmFactor = Clamp(rpmFactor, 0f, 1f);
            var gearRatio = config.GetGearRatio(gear);
            var drivelineTorque = config.EngineBrakingTorqueNm * config.EngineBraking * rpmFactor;
            var wheelTorque = drivelineTorque * gearRatio * config.FinalDriveRatio * config.DrivetrainEfficiency;
            var wheelForce = wheelTorque / config.WheelRadiusM;
            var decelMps2 = (wheelForce / config.MassKg) * surfaceDecelMod;
            return Math.Max(0f, decelMps2 * 3.6f);
        }
    }
}
