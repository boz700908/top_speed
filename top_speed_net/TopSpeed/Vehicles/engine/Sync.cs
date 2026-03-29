using System;
using TopSpeed.Physics.Powertrain;

namespace TopSpeed.Vehicles
{
    internal sealed partial class EngineModel
    {
        private const float CompressionRpmFactorFloor = 0.22f;
        private const float OffThrottleCompressionLossFloor = 0.55f;
        private const float FreeRevExtraCompressionScale = 0.45f;
        private const float MinDeltaTime = 0.0001f;
        private const float TwoPi = (float)(Math.PI * 2.0);

        public void SyncFromSpeed(
            float speedGameUnits,
            int gear,
            float elapsed,
            int throttleInput = 0,
            bool inReverse = false,
            float reverseGearRatio = 3.2f,
            EngineCouplingMode couplingMode = EngineCouplingMode.Blended,
            float couplingFactor = 1f,
            float? driveRatioOverride = null)
        {
            if (_useStrictEngineClutchModel)
            {
                SyncFromSpeedStrict(
                    speedGameUnits,
                    gear,
                    elapsed,
                    throttleInput,
                    inReverse,
                    reverseGearRatio,
                    couplingMode,
                    couplingFactor,
                    driveRatioOverride);
                return;
            }

            SyncFromSpeedLegacy(
                speedGameUnits,
                gear,
                elapsed,
                throttleInput,
                inReverse,
                reverseGearRatio,
                couplingMode,
                couplingFactor,
                driveRatioOverride);
        }

        private void SyncFromSpeedLegacy(
            float speedGameUnits,
            int gear,
            float elapsed,
            int throttleInput,
            bool inReverse,
            float reverseGearRatio,
            EngineCouplingMode couplingMode,
            float couplingFactor,
            float? driveRatioOverride)
        {
            const float IdleControlRpmWindow = 150f;
            const float IdleGovernorTorqueGainNmPerRpm = 0.08f;

            var clampedGear = Math.Max(1, Math.Min(_gearCount, gear));
            var throttle = Math.Max(0, throttleInput) / 100f;
            var speedMps = speedGameUnits / 3.6f;
            var wheelCircumference = _tireCircumferenceM;
            var gearRatio = inReverse
                ? Math.Max(0.1f, reverseGearRatio)
                : (driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                    ? driveRatioOverride.Value
                    : _gearRatios[clampedGear - 1]);
            var coupledRpm = wheelCircumference > 0f
                ? (speedMps / wheelCircumference) * 60f * gearRatio * _finalDriveRatio
                : _idleRpm;
            coupledRpm = Math.Max(_stallRpm, Math.Min(_revLimiter, coupledRpm));

            var lockToDriveline = couplingMode == EngineCouplingMode.Locked;
            var disengaged = couplingMode == EngineCouplingMode.Disengaged;
            var clampedCouplingFactor = Clamp(couplingFactor, 0f, 1f);
            var baseRpm = lockToDriveline ? coupledRpm : (_rpm > 0f ? _rpm : coupledRpm);
            var clampedBaseRpm = Clamp(baseRpm, _stallRpm, _revLimiter);
            var torqueAvailable = _torqueCurve.EvaluateTorque(clampedBaseRpm);
            var maximumEngineTorque = torqueAvailable * _powerFactor;
            var requestedEngineTorque = maximumEngineTorque * throttle;
            var grossEngineTorque = requestedEngineTorque;

            var parasiticFrictionTorque = _engineFrictionTorqueNm;
            var rpmRange = Math.Max(1f, _revLimiter - _idleRpm);
            var rpmFactor = Clamp((clampedBaseRpm - _idleRpm) / rpmRange, 0f, 1f);
            var drivelineLoadFactor = lockToDriveline
                ? 1f
                : (disengaged ? 0f : clampedCouplingFactor * clampedCouplingFactor);
            var lossTorque = parasiticFrictionTorque;
            if (throttle <= 0.1f)
            {
                var compressionFactor = CompressionRpmFactorFloor + ((1f - CompressionRpmFactorFloor) * rpmFactor);
                var compressionLoadFactor = OffThrottleCompressionLossFloor + ((1f - OffThrottleCompressionLossFloor) * drivelineLoadFactor);
                lossTorque += _engineBrakingTorqueNm * _engineBraking * compressionFactor * compressionLoadFactor;
                if (disengaged)
                    lossTorque += _engineBrakingTorqueNm * _engineBraking * FreeRevExtraCompressionScale * rpmFactor;
            }

            var idleControlActive = throttle <= 0.10f && clampedBaseRpm <= _idleRpm + IdleControlRpmWindow;
            if (idleControlActive)
            {
                var idleRpmDeficit = Math.Max(0f, _idleRpm - clampedBaseRpm);
                var idleTargetTorque = lossTorque + (idleRpmDeficit * IdleGovernorTorqueGainNmPerRpm);
                var idleCompensationTorque = Math.Min(maximumEngineTorque, idleTargetTorque);
                if (grossEngineTorque < idleCompensationTorque)
                    grossEngineTorque = idleCompensationTorque;
            }

            var netEngineTorque = grossEngineTorque - lossTorque;
            var rpmPerSecond = (netEngineTorque / _engineInertiaKgm2) * (60f / TwoPi);
            var torqueIntegratedRpm = clampedBaseRpm + (rpmPerSecond * elapsed);
            torqueIntegratedRpm = Clamp(torqueIntegratedRpm, _stallRpm, _maxRpm);

            if (lockToDriveline)
            {
                _rpm = coupledRpm;
            }
            else if (disengaged || clampedCouplingFactor <= 0.001f)
            {
                _rpm = torqueIntegratedRpm;
            }
            else
            {
                var couplingAlpha = Clamp(_drivelineCouplingRate * elapsed * clampedCouplingFactor, 0f, 1f);
                var blendedRpm = torqueIntegratedRpm + ((coupledRpm - torqueIntegratedRpm) * couplingAlpha);
                _rpm = Clamp(blendedRpm, _stallRpm, _maxRpm);
            }

            if (_rpm > _revLimiter)
                _rpm = _revLimiter;

            _grossHorsepower = Calculator.Horsepower(Math.Max(0f, grossEngineTorque), _rpm);
            _netHorsepower = Calculator.Horsepower(Math.Max(0f, netEngineTorque), _rpm);

            _distanceMeters += speedMps * elapsed;
            _speedMps = speedMps;
        }

        private void SyncFromSpeedStrict(
            float speedGameUnits,
            int gear,
            float elapsed,
            int throttleInput,
            bool inReverse,
            float reverseGearRatio,
            EngineCouplingMode couplingMode,
            float couplingFactor,
            float? driveRatioOverride)
        {
            var clampedElapsed = Math.Max(MinDeltaTime, elapsed);
            var clampedGear = Math.Max(1, Math.Min(_gearCount, gear));
            var throttle = Clamp(Math.Max(0, throttleInput) / 100f, 0f, 1f);
            var speedMps = speedGameUnits / 3.6f;
            var wheelCircumference = _tireCircumferenceM;
            var gearRatio = inReverse
                ? Math.Max(0.1f, reverseGearRatio)
                : (driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                    ? driveRatioOverride.Value
                    : _gearRatios[clampedGear - 1]);
            var coupledRpm = wheelCircumference > 0f
                ? (speedMps / wheelCircumference) * 60f * gearRatio * _finalDriveRatio
                : _idleTargetRpm;
            coupledRpm = Clamp(coupledRpm, _stallRpm, _revLimiter);

            var lockToDriveline = couplingMode == EngineCouplingMode.Locked;
            var disengaged = couplingMode == EngineCouplingMode.Disengaged;
            var targetCoupling = lockToDriveline ? 1f : (disengaged ? 0f : Clamp(couplingFactor, 0f, 1f));
            var couplingRate = targetCoupling >= _effectiveClutchCoupling ? _clutchEngageRatePerS : _clutchReleaseRatePerS;
            _effectiveClutchCoupling = MoveTowards(_effectiveClutchCoupling, targetCoupling, couplingRate * clampedElapsed);

            var baseRpm = lockToDriveline ? coupledRpm : (_rpm > 0f ? _rpm : _idleTargetRpm);
            var clampedBaseRpm = Clamp(baseRpm, _stallRpm, _revLimiter);
            var baseOmega = clampedBaseRpm * TwoPi / 60f;
            var coupledOmega = coupledRpm * TwoPi / 60f;

            var availableTorque = _torqueCurve.EvaluateTorque(clampedBaseRpm) * _powerFactor;
            var combustionTorque = availableTorque * throttle;

            var rpmRange = Math.Max(1f, _revLimiter - _idleRpm);
            var rpmFactor = Clamp((clampedBaseRpm - _idleRpm) / rpmRange, 0f, 1f);
            var pumpingScale = CompressionRpmFactorFloor + ((1f - CompressionRpmFactorFloor) * rpmFactor);
            var lossTorque = _engineFrictionCoulombNm
                + (_engineFrictionViscousNmPerRadS * Math.Max(0f, baseOmega))
                + _engineAccessoryTorqueNm
                + (_enginePumpingLossNmAtClosedThrottle * pumpingScale * (1f - throttle));

            var idleCorrectionTorque = 0f;
            if (throttle <= 0.12f)
            {
                var idleErrorRpm = _idleTargetRpm - clampedBaseRpm;
                _idleIntegrator = Clamp(_idleIntegrator + (idleErrorRpm * clampedElapsed), 0f, 10000f);
                var correction = (_idleControlKp * Math.Max(0f, idleErrorRpm)) + (_idleControlKi * _idleIntegrator);
                idleCorrectionTorque = Clamp(correction, 0f, _idleMaxCorrectionTorqueNm);
            }
            else
            {
                _idleIntegrator = 0f;
            }

            var clutchTorqueOnEngine = 0f;
            if (!disengaged && _effectiveClutchCoupling > 0.0001f)
            {
                var demandedTorque = _engineInertiaKgm2 * (coupledOmega - baseOmega) / clampedElapsed;
                var transferableTorque = _clutchCapacityNm * _effectiveClutchCoupling;
                clutchTorqueOnEngine = Clamp(demandedTorque, -transferableTorque, transferableTorque);

                var slipRpm = Math.Abs(clampedBaseRpm - coupledRpm);
                var slipWindow = Math.Max(50f, _launchTargetSlipRpm);
                var slipFactor = Clamp(slipRpm / slipWindow, 0f, 1f);
                var slipDirection = Math.Sign(baseOmega - coupledOmega);
                if (slipDirection != 0)
                    clutchTorqueOnEngine -= _clutchDragTorqueNm * _effectiveClutchCoupling * slipFactor * slipDirection;
            }

            var grossEngineTorque = combustionTorque + idleCorrectionTorque;
            var netEngineTorque = grossEngineTorque - lossTorque + clutchTorqueOnEngine;
            var rpmPerSecond = (netEngineTorque / _engineInertiaKgm2) * (60f / TwoPi);
            var integratedRpm = Clamp(clampedBaseRpm + (rpmPerSecond * clampedElapsed), _stallRpm, _maxRpm);

            _rpm = lockToDriveline ? coupledRpm : integratedRpm;
            if (_rpm > _revLimiter)
                _rpm = _revLimiter;

            var indicatedNetTorque = grossEngineTorque - lossTorque;
            _grossHorsepower = Calculator.Horsepower(Math.Max(0f, grossEngineTorque), _rpm);
            _netHorsepower = Calculator.Horsepower(Math.Max(0f, indicatedNetTorque), _rpm);

            _distanceMeters += speedMps * clampedElapsed;
            _speedMps = speedMps;
        }

        private static float MoveTowards(float current, float target, float maxDelta)
        {
            if (maxDelta <= 0f || current == target)
                return current;
            if (current < target)
                return Math.Min(target, current + maxDelta);
            return Math.Max(target, current - maxDelta);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
