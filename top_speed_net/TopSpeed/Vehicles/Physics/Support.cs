using System;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private int CalculateAcceleration()
        {
            var driveGear = GetDriveGear();
            var gearRange = _engine.GetGearRangeKmh(driveGear);
            var gearMin = _engine.GetGearMinSpeedKmh(driveGear);
            var gearCenter = gearMin + (gearRange * 0.18f);
            _speedDiff = _speed - gearCenter;
            var relSpeedDiff = _speedDiff / gearRange;
            if (Math.Abs(relSpeedDiff) < 1.9f)
            {
                var acceleration = (int)(100.0f * (0.5f + Math.Cos(relSpeedDiff * Math.PI * 0.5f)));
                return acceleration < 5 ? 5 : acceleration;
            }

            {
                var acceleration = (int)(100.0f * (0.5f + Math.Cos(0.95f * Math.PI)));
                return acceleration < 5 ? 5 : acceleration;
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float SanitizeFinite(float value, float fallback)
        {
            return IsFinite(value) ? value : fallback;
        }

        private float CalculateBrakeDecel(float brakeInput, float surfaceDecelMod)
        {
            if (brakeInput <= 0f)
                return 0f;
            var grip = Math.Max(0.1f, _tireGripCoefficient * surfaceDecelMod);
            var decelMps2 = brakeInput * _brakeStrength * grip * 9.80665f;
            return decelMps2 * 3.6f;
        }

        private float CalculateEngineBrakingDecel(float surfaceDecelMod)
        {
            if (_engineBrakingTorqueNm <= 0f || _massKg <= 0f || _wheelRadiusM <= 0f)
                return 0f;
            var rpmRange = _revLimiter - _idleRpm;
            if (rpmRange <= 0f)
                return 0f;
            var rpmFactor = (_engine.Rpm - _idleRpm) / rpmRange;
            if (rpmFactor <= 0f)
                return 0f;
            rpmFactor = Math.Max(0f, Math.Min(1f, rpmFactor));
            var gearRatio = _gear == ReverseGear ? _reverseGearRatio : _engine.GetGearRatio(GetDriveGear());
            var drivelineTorque = _engineBrakingTorqueNm * _engineBraking * rpmFactor;
            var wheelTorque = drivelineTorque * gearRatio * _finalDriveRatio * _drivetrainEfficiency;
            var wheelForce = wheelTorque / _wheelRadiusM;
            var decelMps2 = (wheelForce / _massKg) * surfaceDecelMod;
            return Math.Max(0f, decelMps2 * 3.6f);
        }

        private float GetLapStartPosition(float position)
        {
            var lapLength = _track.Length;
            if (lapLength <= 0f)
                return 0f;
            var lapIndex = (float)Math.Floor(position / lapLength);
            if (lapIndex < 0f)
                lapIndex = 0f;
            return lapIndex * lapLength;
        }

        private int GetDriveGear()
        {
            return _gear < FirstForwardGear ? FirstForwardGear : _gear;
        }
    }
}
