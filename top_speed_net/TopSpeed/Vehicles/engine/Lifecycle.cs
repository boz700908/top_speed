using System;

namespace TopSpeed.Vehicles
{
    internal sealed partial class EngineModel
    {
        public void Reset()
        {
            _rpm = 0f;
            _speedMps = 0f;
            _distanceMeters = 0f;
            _idleIntegrator = 0f;
            _effectiveClutchCoupling = 0f;
        }

        public void ResetForCrash()
        {
            _rpm = 0f;
            _speedMps = 0f;
            _idleIntegrator = 0f;
            _effectiveClutchCoupling = 0f;
        }

        public void StartEngine()
        {
            _rpm = _idleRpm;
            _idleIntegrator = 0f;
            _effectiveClutchCoupling = 0f;
        }

        public void StopEngine()
        {
            _rpm = 0f;
            _idleIntegrator = 0f;
            _effectiveClutchCoupling = 0f;
        }

        public void SetSpeed(float speedMps)
        {
            _speedMps = Math.Max(0f, speedMps);
        }

        public void UpdateKinematicsOnly(float speedGameUnits, float elapsed)
        {
            var speedMps = Math.Max(0f, speedGameUnits / 3.6f);
            _speedMps = speedMps;
            _distanceMeters += speedMps * Math.Max(0f, elapsed);
            _grossHorsepower = 0f;
            _netHorsepower = 0f;
        }
    }
}
