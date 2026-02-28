using System;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Vehicles.Control;
using TopSpeed.Vehicles.Events;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        internal void RunDynamics(float elapsed, in CarControlIntent controlIntent)
        {
            if (_state == CarState.Running && _started())
            {
                if (!IsFinite(_speed))
                    _speed = 0f;
                if (!IsFinite(_positionX))
                    _positionX = 0f;
                if (!IsFinite(_positionY))
                    _positionY = 0f;
                if (_positionY < 0f)
                    _positionY = 0f;

                _currentSteering = controlIntent.Steering;
                _currentThrottle = controlIntent.Throttle;
                _currentBrake = controlIntent.Brake;
                var gearUp = controlIntent.GearUp;
                var gearDown = controlIntent.GearDown;

                _currentSurfaceTractionFactor = _surfaceTractionFactor;
                _currentDeceleration = _deceleration;
                _speedDiff = 0;
                switch (_surface)
                {
                    case TrackSurface.Gravel:
                        _currentSurfaceTractionFactor = (_currentSurfaceTractionFactor * 2) / 3;
                        _currentDeceleration = (_currentDeceleration * 2) / 3;
                        break;
                    case TrackSurface.Water:
                        _currentSurfaceTractionFactor = (_currentSurfaceTractionFactor * 3) / 5;
                        _currentDeceleration = (_currentDeceleration * 3) / 5;
                        break;
                    case TrackSurface.Sand:
                        _currentSurfaceTractionFactor = _currentSurfaceTractionFactor / 2;
                        _currentDeceleration = (_currentDeceleration * 3) / 2;
                        break;
                    case TrackSurface.Snow:
                        _currentDeceleration = _currentDeceleration / 2;
                        break;
                }

                _factor1 = 100;
                if (_manualTransmission)
                {
                    if (!gearUp && !gearDown)
                        s_stickReleased = true;

                    if (gearDown && s_stickReleased)
                    {
                        if (_gear > FirstForwardGear)
                        {
                            s_stickReleased = false;
                            _switchingGear = -1;
                            --_gear;
                            if (_soundEngine.GetPitch() > 3f * _topFreq / (2f * _soundEngine.InputSampleRate))
                                _soundBadSwitch.Play(loop: false);
                            if (!AnyBackfirePlaying() && Algorithm.RandomInt(5) == 1)
                                PlayRandomBackfire();
                            PushEvent(EventType.InGear, 0.2f);
                        }
                        else if (_gear == FirstForwardGear)
                        {
                            s_stickReleased = false;
                            if (_speed <= ReverseShiftMaxSpeedKmh)
                            {
                                _switchingGear = -1;
                                _gear = ReverseGear;
                                PushEvent(EventType.InGear, 0.2f);
                            }
                            else
                            {
                                _soundBadSwitch.Play(loop: false);
                            }
                        }
                    }
                    else if (gearUp && s_stickReleased)
                    {
                        if (_gear == ReverseGear)
                        {
                            s_stickReleased = false;
                            if (_speed <= ReverseShiftMaxSpeedKmh)
                            {
                                _switchingGear = 1;
                                _gear = FirstForwardGear;
                                PushEvent(EventType.InGear, 0.2f);
                            }
                            else
                            {
                                _soundBadSwitch.Play(loop: false);
                            }
                        }
                        else if (_gear < _gears)
                        {
                            s_stickReleased = false;
                            _switchingGear = 1;
                            ++_gear;
                            if (_soundEngine.GetPitch() < _idleFreq / (float)_soundEngine.InputSampleRate)
                                _soundBadSwitch.Play(loop: false);
                            if (!AnyBackfirePlaying() && Algorithm.RandomInt(5) == 1)
                                PlayRandomBackfire();
                            PushEvent(EventType.InGear, 0.2f);
                        }
                    }
                }
                else
                {
                    var reverseRequested = controlIntent.ReverseRequested;
                    var forwardRequested = controlIntent.ForwardRequested;

                    if (reverseRequested && _gear != ReverseGear)
                    {
                        if (_speed <= ReverseShiftMaxSpeedKmh)
                        {
                            _switchingGear = -1;
                            _gear = ReverseGear;
                            PushEvent(EventType.InGear, 0.2f);
                        }
                        else
                        {
                            _currentThrottle = 0;
                            _currentBrake = -100;
                            _soundBadSwitch.Play(loop: false);
                        }
                    }
                    else if (forwardRequested && _gear == ReverseGear)
                    {
                        if (_speed <= ReverseShiftMaxSpeedKmh)
                        {
                            _switchingGear = 1;
                            _gear = FirstForwardGear;
                            PushEvent(EventType.InGear, 0.2f);
                        }
                        else
                        {
                            _soundBadSwitch.Play(loop: false);
                        }
                    }
                }

                if (_soundThrottle != null)
                {
                    if (_soundEngine.IsPlaying)
                    {
                        if (_currentThrottle > 50)
                        {
                            if (!_soundThrottle.IsPlaying)
                            {
                                if (_throttleVolume < 80.0f)
                                    _throttleVolume = 80.0f;
                                SetPlayerEngineVolumePercent(_soundThrottle, (int)_throttleVolume);
                                _prevThrottleVolume = _throttleVolume;
                                _soundThrottle.Play(loop: true);
                            }
                            else
                            {
                                if (_throttleVolume >= 80.0f)
                                    _throttleVolume += (100.0f - _throttleVolume) * elapsed;
                                else
                                    _throttleVolume = 80.0f;
                                if (_throttleVolume > 100.0f)
                                    _throttleVolume = 100.0f;
                                if ((int)_throttleVolume != (int)_prevThrottleVolume)
                                {
                                    SetPlayerEngineVolumePercent(_soundThrottle, (int)_throttleVolume);
                                    _prevThrottleVolume = _throttleVolume;
                                }
                            }
                        }
                        else
                        {
                            _throttleVolume -= 10.0f * elapsed;
                            var min = _speed * 95 / _topSpeed;
                            if (_throttleVolume < min)
                                _throttleVolume = min;
                            if ((int)_throttleVolume != (int)_prevThrottleVolume)
                            {
                                SetPlayerEngineVolumePercent(_soundThrottle, (int)_throttleVolume);
                                _prevThrottleVolume = _throttleVolume;
                            }
                        }
                    }
                    else if (_soundThrottle.IsPlaying)
                    {
                        _soundThrottle.Stop();
                    }
                }

                _thrust = _currentThrottle;
                if (_currentThrottle == 0)
                    _thrust = _currentBrake;
                else if (_currentBrake == 0)
                    _thrust = _currentThrottle;
                else if (-_currentBrake > _currentThrottle)
                    _thrust = _currentBrake;

                var speedMpsCurrent = _speed / 3.6f;
                var throttle = Math.Max(0f, Math.Min(100f, _currentThrottle)) / 100f;
                var inReverse = _gear == ReverseGear;
                var currentLapStart = GetLapStartPosition(_positionY);
                var reverseBlockedAtLapStart = inReverse && _positionY <= currentLapStart + 0.001f;
                var surfaceTractionMod = _surfaceTractionFactor > 0f
                    ? _currentSurfaceTractionFactor / _surfaceTractionFactor
                    : 1.0f;
                var longitudinalGripFactor = 1.0f;

                if (_thrust > 10)
                {
                    if (reverseBlockedAtLapStart)
                    {
                        _speedDiff = 0f;
                        _lastDriveRpm = 0f;
                    }
                    else
                    {
                        var steeringCommandAccel = (_currentSteering / 100.0f) * _steering;
                        if (steeringCommandAccel > 1.0f)
                            steeringCommandAccel = 1.0f;
                        else if (steeringCommandAccel < -1.0f)
                            steeringCommandAccel = -1.0f;
                        var steerRadAccel = (float)(Math.PI / 180.0) * (_maxSteerDeg * steeringCommandAccel);
                        var curvatureAccel = (float)Math.Tan(steerRadAccel) / _wheelbaseM;
                        var desiredLatAccel = curvatureAccel * speedMpsCurrent * speedMpsCurrent;
                        var desiredLatAccelAbs = Math.Abs(desiredLatAccel);
                        var grip = _tireGripCoefficient * surfaceTractionMod * _lateralGripCoefficient;
                        var maxLatAccel = grip * 9.80665f;
                        var lateralRatio = maxLatAccel > 0f ? Math.Min(1.0f, desiredLatAccelAbs / maxLatAccel) : 0f;
                        longitudinalGripFactor = (float)Math.Sqrt(Math.Max(0.0, 1.0 - (lateralRatio * lateralRatio)));
                        var driveRpm = CalculateDriveRpm(speedMpsCurrent, throttle);
                        var engineTorque = CalculateEngineTorqueNm(driveRpm) * throttle * _powerFactor;
                        var gearRatio = inReverse ? _reverseGearRatio : _engine.GetGearRatio(GetDriveGear());
                        var wheelTorque = engineTorque * gearRatio * _finalDriveRatio * _drivetrainEfficiency;
                        var wheelForce = wheelTorque / _wheelRadiusM;
                        var tractionLimit = _tireGripCoefficient * surfaceTractionMod * _massKg * 9.80665f;
                        if (wheelForce > tractionLimit)
                            wheelForce = tractionLimit;
                        wheelForce *= (float)longitudinalGripFactor;
                        wheelForce *= (_factor1 / 100f);
                        if (inReverse)
                            wheelForce *= _reversePowerFactor;

                        var dragForce = 0.5f * 1.225f * _dragCoefficient * _frontalAreaM2 * speedMpsCurrent * speedMpsCurrent;
                        var rollingForce = _rollingResistanceCoefficient * _massKg * 9.80665f;
                        var netForce = wheelForce - dragForce - rollingForce;
                        var accelMps2 = netForce / _massKg;
                        var newSpeedMps = speedMpsCurrent + (accelMps2 * elapsed);
                        if (newSpeedMps < 0f)
                            newSpeedMps = 0f;
                        _speedDiff = (newSpeedMps - speedMpsCurrent) * 3.6f;
                        _lastDriveRpm = CalculateDriveRpm(newSpeedMps, throttle);

                        if (_backfirePlayed)
                            _backfirePlayed = false;
                    }
                }
                else
                {
                    var surfaceDecelMod = _deceleration > 0f ? _currentDeceleration / _deceleration : 1.0f;
                    var brakeInput = Math.Max(0f, Math.Min(100f, -_currentBrake)) / 100f;
                    var brakeDecel = CalculateBrakeDecel(brakeInput, surfaceDecelMod);
                    var engineBrakeDecel = CalculateEngineBrakingDecel(surfaceDecelMod);
                    var totalDecel = _thrust < -10 ? (brakeDecel + engineBrakeDecel) : engineBrakeDecel;
                    _speedDiff = -totalDecel * elapsed;
                    _lastDriveRpm = 0f;
                }

                _speed += _speedDiff;
                if (_speed > _topSpeed)
                    _speed = _topSpeed;
                if (_speed < 0)
                    _speed = 0;
                if (!IsFinite(_speed))
                {
                    _speed = 0f;
                    _speedDiff = 0f;
                }
                if (!IsFinite(_lastDriveRpm))
                    _lastDriveRpm = _idleRpm;

                if (reverseBlockedAtLapStart && _thrust > 10)
                {
                    _speed = 0f;
                    _speedDiff = 0f;
                    _lastDriveRpm = 0f;
                }

                if (_gear == ReverseGear)
                {
                    var reverseMax = Math.Max(5.0f, _reverseMaxSpeedKph);
                    if (_speed > reverseMax)
                        _speed = reverseMax;
                }
                else if (_manualTransmission)
                {
                    var gearMax = _engine.GetGearMaxSpeedKmh(_gear);
                    if (_speed > gearMax)
                        _speed = gearMax;
                }
                else if (_gear != ReverseGear)
                {
                    UpdateAutomaticGear(elapsed, _speed / 3.6f, throttle, surfaceTractionMod, longitudinalGripFactor);
                }

                _engine.SyncFromSpeed(_speed, GetDriveGear(), elapsed, _currentThrottle);
                if (_lastDriveRpm > 0f && _lastDriveRpm > _engine.Rpm)
                    _engine.OverrideRpm(_lastDriveRpm);

                if (_thrust <= 0)
                {
                    if (!AnyBackfirePlaying() && !_backfirePlayed)
                    {
                        if (Algorithm.RandomInt(5) == 1)
                            PlayRandomBackfire();
                    }
                    _backfirePlayed = true;
                }

                if (_thrust < -50 && _speed > 0)
                {
                    BrakeSound();
                    _vibration?.Gain(VibrationEffectType.Spring, (int)(50.0f * _speed / _topSpeed));
                    _currentSteering = _currentSteering * 2 / 3;
                }
                else if (_currentSteering != 0 && _speed > _topSpeed / 2)
                {
                    if (_thrust > -50)
                        BrakeCurveSound();
                }
                else
                {
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    SetSurfaceLoopVolumePercent(_soundAsphalt, 90);
                    SetSurfaceLoopVolumePercent(_soundGravel, 90);
                    SetSurfaceLoopVolumePercent(_soundWater, 90);
                    SetSurfaceLoopVolumePercent(_soundSand, 90);
                    SetSurfaceLoopVolumePercent(_soundSnow, 90);
                }

                var speedMps = _speed / 3.6f;
                var longitudinalDelta = speedMps * elapsed;
                if (_gear == ReverseGear)
                {
                    var nextPositionY = _positionY - longitudinalDelta;
                    if (nextPositionY < currentLapStart)
                        nextPositionY = currentLapStart;
                    if (nextPositionY < 0f)
                        nextPositionY = 0f;
                    _positionY = nextPositionY;
                }
                else
                {
                    _positionY += longitudinalDelta;
                }
                var surfaceMultiplier = _surface == TrackSurface.Snow ? 1.44f : 1.0f;
                var steeringCommandLat = (_currentSteering / 100.0f) * _steering;
                if (steeringCommandLat > 1.0f)
                    steeringCommandLat = 1.0f;
                else if (steeringCommandLat < -1.0f)
                    steeringCommandLat = -1.0f;
                var steerRadLat = (float)(Math.PI / 180.0) * (_maxSteerDeg * steeringCommandLat);
                var curvatureLat = (float)Math.Tan(steerRadLat) / _wheelbaseM;
                var surfaceTractionModLat = _surfaceTractionFactor > 0f ? _currentSurfaceTractionFactor / _surfaceTractionFactor : 1.0f;
                var gripLat = _tireGripCoefficient * surfaceTractionModLat * _lateralGripCoefficient;
                var maxLatAccelLat = gripLat * 9.80665f;
                var desiredLatAccelLat = curvatureLat * speedMps * speedMps;
                var massFactor = (float)Math.Sqrt(1500f / _massKg);
                if (massFactor > 3.0f)
                    massFactor = 3.0f;
                var stabilityScale = 1.0f - (_highSpeedStability * (speedMps / StabilitySpeedRef) * massFactor);
                if (stabilityScale < 0.2f)
                    stabilityScale = 0.2f;
                else if (stabilityScale > 1.0f)
                    stabilityScale = 1.0f;
                var responseTime = BaseLateralSpeed / 20.0f;
                var maxLatSpeed = maxLatAccelLat * responseTime * stabilityScale;
                var desiredLatSpeed = desiredLatAccelLat * responseTime;
                if (desiredLatSpeed > maxLatSpeed)
                    desiredLatSpeed = maxLatSpeed;
                else if (desiredLatSpeed < -maxLatSpeed)
                    desiredLatSpeed = -maxLatSpeed;
                var lateralSpeed = desiredLatSpeed * surfaceMultiplier;
                _positionX += (lateralSpeed * elapsed);

                if (_frame % 4 == 0)
                {
                    _frame = 0;
                    _brakeFrequency = (int)(11025 + 22050 * _speed / _topSpeed);
                    if (_brakeFrequency != _prevBrakeFrequency)
                    {
                        _soundBrake.SetFrequency(_brakeFrequency);
                        _prevBrakeFrequency = _brakeFrequency;
                    }
                    if (_speed <= 50.0f)
                        SetPlayerEventVolumePercent(_soundBrake, (int)(100 - (50 - (_speed))));
                    else
                        SetPlayerEventVolumePercent(_soundBrake, 100);
                    if (_manualTransmission)
                        UpdateEngineFreqManual();
                    else
                        UpdateEngineFreq();
                    UpdateSoundRoad();
                    if (_vibration != null)
                    {
                        if (_surface == TrackSurface.Gravel)
                            _vibration.Gain(VibrationEffectType.Gravel, (int)(_speed * 10000 / _topSpeed));
                        else
                            _vibration.Gain(VibrationEffectType.Gravel, 0);

                        if (_speed == 0)
                            _vibration.Gain(VibrationEffectType.Spring, 10000);
                        else
                            _vibration.Gain(VibrationEffectType.Spring, (int)(10000 * _speed / _topSpeed));

                        if (_speed < _topSpeed / 10)
                            _vibration.Gain(VibrationEffectType.Engine, (int)(10000 - _speed * 10 / _topSpeed));
                        else
                            _vibration.Gain(VibrationEffectType.Engine, 0);
                    }
                }

                switch (_surface)
                {
                    case TrackSurface.Asphalt:
                        if (!_soundAsphalt.IsPlaying)
                        {
                            _soundAsphalt.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                            _soundAsphalt.Play(loop: true);
                        }
                        break;
                    case TrackSurface.Gravel:
                        if (!_soundGravel.IsPlaying)
                        {
                            _soundGravel.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                            _soundGravel.Play(loop: true);
                        }
                        break;
                    case TrackSurface.Water:
                        if (!_soundWater.IsPlaying)
                        {
                            _soundWater.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                            _soundWater.Play(loop: true);
                        }
                        break;
                    case TrackSurface.Sand:
                        if (!_soundSand.IsPlaying)
                        {
                            _soundSand.SetFrequency((int)(_surfaceFrequency / 2.5f));
                            _soundSand.Play(loop: true);
                        }
                        break;
                    case TrackSurface.Snow:
                        if (!_soundSnow.IsPlaying)
                        {
                            _soundSnow.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                            _soundSnow.Play(loop: true);
                        }
                        break;
                }
            }
            else if (_state == CarState.Stopping)
            {
                _speed -= (elapsed * 100 * _deceleration);
                if (_speed < 0)
                    _speed = 0;
                if (_frame % 4 == 0)
                {
                    _frame = 0;
                    UpdateEngineFreq();
                    UpdateSoundRoad();
                }
            }
        }
    }
}
