using System;
using TopSpeed.Bots;
using TopSpeed.Vehicles.Events;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private float CalculateDriveRpm(float speedMps, float throttle)
        {
            var wheelCircumference = _wheelRadiusM * 2.0f * (float)Math.PI;
            var gearRatio = _gear == ReverseGear ? _reverseGearRatio : _engine.GetGearRatio(GetDriveGear());
            var speedBasedRpm = wheelCircumference > 0f
                ? (speedMps / wheelCircumference) * 60f * gearRatio * _finalDriveRatio
                : 0f;
            var launchTarget = _idleRpm + (throttle * (_launchRpm - _idleRpm));
            var rpm = Math.Max(speedBasedRpm, launchTarget);
            if (rpm < _idleRpm)
                rpm = _idleRpm;
            if (rpm > _revLimiter)
                rpm = _revLimiter;
            return rpm;
        }

        private void UpdateAutomaticGear(float elapsed, float speedMps, float throttle, float surfaceTractionMod, float longitudinalGripFactor)
        {
            if (_gears <= 1)
                return;

            if (_autoShiftCooldown > 0f)
            {
                _autoShiftCooldown -= elapsed;
                return;
            }

            var currentAccel = ComputeNetAccelForGear(_gear, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor);
            var currentRpm = SpeedToRpm(speedMps, _gear);
            var upAccel = _gear < _gears
                ? ComputeNetAccelForGear(_gear + 1, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor)
                : float.NegativeInfinity;
            var downAccel = _gear > 1
                ? ComputeNetAccelForGear(_gear - 1, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor)
                : float.NegativeInfinity;

            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    _gear,
                    _gears,
                    speedMps,
                    _topSpeed / 3.6f,
                    _idleRpm,
                    _revLimiter,
                    currentRpm,
                    currentAccel,
                    upAccel,
                    downAccel),
                _transmissionPolicy);

            if (decision.Changed)
                ShiftAutomaticGear(decision.NewGear, decision.CooldownSeconds);
        }

        private void ShiftAutomaticGear(int newGear, float cooldownSeconds)
        {
            if (newGear == _gear)
                return;
            var upshift = newGear > _gear;
            _switchingGear = upshift ? 1 : -1;
            _gear = newGear;
            var inGearDelay = upshift ? Math.Max(0.2f, cooldownSeconds) : 0.2f;
            PushEvent(EventType.InGear, inGearDelay);
            _autoShiftCooldown = Math.Max(0f, cooldownSeconds);
        }

        private float ComputeNetAccelForGear(int gear, float speedMps, float throttle, float surfaceTractionMod, float longitudinalGripFactor)
        {
            var rpm = SpeedToRpm(speedMps, gear);
            if (rpm <= 0f)
                return float.NegativeInfinity;
            if (rpm > _revLimiter && gear < _gears)
                return float.NegativeInfinity;

            var engineTorque = CalculateEngineTorqueNm(rpm) * throttle * _powerFactor;
            var gearRatio = _engine.GetGearRatio(gear);
            var wheelTorque = engineTorque * gearRatio * _finalDriveRatio * _drivetrainEfficiency;
            var wheelForce = wheelTorque / _wheelRadiusM;
            var tractionLimit = _tireGripCoefficient * surfaceTractionMod * _massKg * 9.80665f;
            if (wheelForce > tractionLimit)
                wheelForce = tractionLimit;
            wheelForce *= longitudinalGripFactor;

            var dragForce = 0.5f * 1.225f * _dragCoefficient * _frontalAreaM2 * speedMps * speedMps;
            var rollingForce = _rollingResistanceCoefficient * _massKg * 9.80665f;
            var netForce = wheelForce - dragForce - rollingForce;
            return netForce / _massKg;
        }

        private float SpeedToRpm(float speedMps, int gear)
        {
            var wheelCircumference = _wheelRadiusM * 2.0f * (float)Math.PI;
            if (wheelCircumference <= 0f)
                return 0f;
            var gearRatio = _engine.GetGearRatio(gear);
            return (speedMps / wheelCircumference) * 60f * gearRatio * _finalDriveRatio;
        }

        private float CalculateEngineTorqueNm(float rpm)
        {
            if (_peakTorqueNm <= 0f)
                return 0f;
            var clampedRpm = Math.Max(_idleRpm, Math.Min(_revLimiter, rpm));
            if (clampedRpm <= _peakTorqueRpm)
            {
                var denom = _peakTorqueRpm - _idleRpm;
                var t = denom > 0f ? (clampedRpm - _idleRpm) / denom : 0f;
                return SmoothStep(_idleTorqueNm, _peakTorqueNm, t);
            }

            {
                var denom = _revLimiter - _peakTorqueRpm;
                var t = denom > 0f ? (clampedRpm - _peakTorqueRpm) / denom : 0f;
                return SmoothStep(_peakTorqueNm, _redlineTorqueNm, t);
            }
        }

        private static float SmoothStep(float a, float b, float t)
        {
            var clamped = Math.Max(0f, Math.Min(1f, t));
            clamped = clamped * clamped * (3f - 2f * clamped);
            return a + (b - a) * clamped;
        }
    }
}
