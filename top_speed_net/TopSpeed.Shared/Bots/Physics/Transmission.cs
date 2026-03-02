using System;

namespace TopSpeed.Bots
{
    public static partial class BotPhysics
    {
        private static void UpdateAutomaticGear(
            BotPhysicsConfig config,
            ref BotPhysicsState state,
            float elapsed,
            float speedMps,
            float throttle,
            float surfaceTractionMod,
            float longitudinalGripFactor)
        {
            if (config.Gears <= 1)
                return;

            if (state.AutoShiftCooldownSeconds > 0f)
            {
                state.AutoShiftCooldownSeconds -= elapsed;
                return;
            }

            var currentAccel = ComputeNetAccelForGear(config, state.Gear, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor);
            var currentRpm = SpeedToRpm(config, speedMps, state.Gear);
            var upAccel = state.Gear < config.Gears
                ? ComputeNetAccelForGear(config, state.Gear + 1, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor)
                : float.NegativeInfinity;
            var downAccel = state.Gear > 1
                ? ComputeNetAccelForGear(config, state.Gear - 1, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor)
                : float.NegativeInfinity;

            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    state.Gear,
                    config.Gears,
                    speedMps,
                    config.TopSpeedKph / 3.6f,
                    config.IdleRpm,
                    config.RevLimiter,
                    currentRpm,
                    currentAccel,
                    upAccel,
                    downAccel),
                config.TransmissionPolicy);

            if (decision.Changed)
            {
                state.Gear = decision.NewGear;
                state.AutoShiftCooldownSeconds = decision.CooldownSeconds;
            }
        }

        private static float ComputeNetAccelForGear(
            BotPhysicsConfig config,
            int gear,
            float speedMps,
            float throttle,
            float surfaceTractionMod,
            float longitudinalGripFactor)
        {
            var rpm = SpeedToRpm(config, speedMps, gear);
            if (rpm <= 0f)
                return float.NegativeInfinity;
            if (rpm > config.RevLimiter && gear < config.Gears)
                return float.NegativeInfinity;

            var engineTorque = CalculateEngineTorqueNm(config, rpm) * throttle * config.PowerFactor;
            var gearRatio = config.GetGearRatio(gear);
            var wheelTorque = engineTorque * gearRatio * config.FinalDriveRatio * config.DrivetrainEfficiency;
            var wheelForce = wheelTorque / config.WheelRadiusM;
            var tractionLimit = config.TireGripCoefficient * surfaceTractionMod * config.MassKg * 9.80665f;
            if (wheelForce > tractionLimit)
                wheelForce = tractionLimit;
            wheelForce *= longitudinalGripFactor;

            var dragForce = 0.5f * 1.225f * config.DragCoefficient * config.FrontalAreaM2 * speedMps * speedMps;
            var rollingForce = config.RollingResistanceCoefficient * config.MassKg * 9.80665f;
            var netForce = wheelForce - dragForce - rollingForce;
            return netForce / config.MassKg;
        }

        private static float SpeedToRpm(BotPhysicsConfig config, float speedMps, int gear)
        {
            var wheelCircumference = config.WheelRadiusM * 2.0f * (float)Math.PI;
            if (wheelCircumference <= 0f)
                return 0f;
            var gearRatio = config.GetGearRatio(gear);
            return (speedMps / wheelCircumference) * 60f * gearRatio * config.FinalDriveRatio;
        }
    }
}
