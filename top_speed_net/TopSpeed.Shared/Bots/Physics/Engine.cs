using System;

namespace TopSpeed.Bots
{
    public static partial class BotPhysics
    {
        private static float CalculateDriveRpm(BotPhysicsConfig config, int gear, float speedMps, float throttle)
        {
            var wheelCircumference = config.WheelRadiusM * 2.0f * (float)Math.PI;
            var gearRatio = config.GetGearRatio(gear);
            var speedBasedRpm = wheelCircumference > 0f
                ? (speedMps / wheelCircumference) * 60f * gearRatio * config.FinalDriveRatio
                : 0f;
            var launchTarget = config.IdleRpm + (throttle * (config.LaunchRpm - config.IdleRpm));
            var rpm = Math.Max(speedBasedRpm, launchTarget);
            if (rpm < config.IdleRpm)
                rpm = config.IdleRpm;
            if (rpm > config.RevLimiter)
                rpm = config.RevLimiter;
            return rpm;
        }

        private static float CalculateEngineTorqueNm(BotPhysicsConfig config, float rpm)
        {
            if (config.PeakTorqueNm <= 0f)
                return 0f;

            var clampedRpm = Math.Max(config.IdleRpm, Math.Min(config.RevLimiter, rpm));
            if (clampedRpm <= config.PeakTorqueRpm)
            {
                var denom = config.PeakTorqueRpm - config.IdleRpm;
                var t = denom > 0f ? (clampedRpm - config.IdleRpm) / denom : 0f;
                return SmoothStep(config.IdleTorqueNm, config.PeakTorqueNm, t);
            }

            var highDenom = config.RevLimiter - config.PeakTorqueRpm;
            var highT = highDenom > 0f ? (clampedRpm - config.PeakTorqueRpm) / highDenom : 0f;
            return SmoothStep(config.PeakTorqueNm, config.RedlineTorqueNm, highT);
        }
    }
}
