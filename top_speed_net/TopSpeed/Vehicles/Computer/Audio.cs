using System;
using TopSpeed.Audio;
using TopSpeed.Drive.Session.Audio;
using TopSpeed.Input;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
        private void BindAudio(RemoteVehicleAudio audio)
        {
            _soundEngine = audio.Engine;
            _soundHorn = audio.Horn;
            _soundStart = audio.Start;
            _soundCrash = audio.Crash;
            _soundBrake = audio.Brake;
            _soundMiniCrash = audio.MiniCrash;
            _soundBump = audio.Bump;
            _soundBackfire = audio.Backfire;
        }

        private void ConfigureAudioState()
        {
            var enableStereoWidening = _settings.StereoWidening;

            _soundEngine.SetDopplerFactor(1f);
            _soundHorn.SetDopplerFactor(0f);
            _soundBrake.SetDopplerFactor(0f);
            _soundCrash.SetDopplerFactor(0f);
            _soundMiniCrash.SetDopplerFactor(0f);
            _soundBump.SetDopplerFactor(0f);
            _soundBackfire?.SetDopplerFactor(0f);

            _soundEngine.SetStereoWidening(enableStereoWidening);
            _soundHorn.SetStereoWidening(enableStereoWidening);
            _soundStart.SetStereoWidening(enableStereoWidening);
            _soundCrash.SetStereoWidening(enableStereoWidening);
            _soundBrake.SetStereoWidening(enableStereoWidening);
            _soundMiniCrash.SetStereoWidening(enableStereoWidening);
            _soundBump.SetStereoWidening(enableStereoWidening);
            _soundBackfire?.SetStereoWidening(enableStereoWidening);
        }

        private void UpdateEngineFreq()
        {
            _frequency = EnginePitch.FromRpm(
                _engine.Rpm,
                _engine.StallRpm,
                _engine.IdleRpm,
                _engine.RevLimiter,
                _idleFreq,
                _topFreq,
                _pitchCurveExponent);

            if (_frequency != _prevFrequency)
            {
                _soundEngine.SetFrequency(_frequency);
                _prevFrequency = _frequency;
            }
        }

        private void RefreshCategoryVolumes(bool force = false)
        {
            var enginePercent = _settings.AudioVolumes?.OtherVehicleEnginePercent ?? 80;
            var eventsPercent = _settings.AudioVolumes?.OtherVehicleEventsPercent ?? 100;
            var receiverRadioPercent = _settings.AudioVolumes?.RadioPercent ?? 100;
            if (!force &&
                enginePercent == _lastOtherEngineVolumePercent &&
                eventsPercent == _lastOtherEventsVolumePercent &&
                receiverRadioPercent == _lastRadioVolumePercent)
                return;

            _lastOtherEngineVolumePercent = enginePercent;
            _lastOtherEventsVolumePercent = eventsPercent;
            _lastRadioVolumePercent = receiverRadioPercent;

            SetOtherEngineVolumePercent(_soundEngine, 80);
            SetOtherEngineVolumePercent(_soundStart, 100);
            SetOtherEventVolumePercent(_soundHorn, 100);
            SetOtherEventVolumePercent(_soundCrash, 100);
            SetOtherEventVolumePercent(_soundBrake, 100);
            SetOtherEventVolumePercent(_soundMiniCrash, 100);
            SetOtherEventVolumePercent(_soundBump, 100);
            SetOtherEventVolumePercent(_soundBackfire, 100);
            ApplyRemoteRadioVolume();
        }

        private void SetOtherEngineVolumePercent(Source? sound, int percent)
        {
            sound.SetVolumePercent(_settings, AudioVolumeCategory.OtherVehicleEngine, percent);
        }

        private void SetOtherEventVolumePercent(Source? sound, int percent)
        {
            sound.SetVolumePercent(_settings, AudioVolumeCategory.OtherVehicleEvents, percent);
        }

        private void SetRemoteRadioSenderVolumePercent(int senderRadioPercent)
        {
            var clamped = senderRadioPercent;
            if (clamped < 0)
                clamped = 0;
            else if (clamped > 100)
                clamped = 100;
            if (clamped == _remoteRadioSenderVolumePercent)
                return;

            _remoteRadioSenderVolumePercent = clamped;
            ApplyRemoteRadioVolume();
        }

        private void ApplyRemoteRadioVolume()
        {
            var receiverRadioPercent = _lastRadioVolumePercent >= 0
                ? _lastRadioVolumePercent
                : _settings.AudioVolumes?.RadioPercent ?? 100;
            if (receiverRadioPercent < 0)
                receiverRadioPercent = 0;
            else if (receiverRadioPercent > 100)
                receiverRadioPercent = 100;

            var effectiveBufferedPercent = (_remoteRadioSenderVolumePercent * receiverRadioPercent + 50) / 100;
            _radio.SetVolumePercent(effectiveBufferedPercent);

            // Live radio applies receiver-side category scaling via AudioHelpers.
            _liveRadio.SetVolumePercent(_remoteRadioSenderVolumePercent);
        }

        private float NormalizeSpeedByTopSpeed(float speedKph, float maxRatio = 1f)
        {
            var referenceTopSpeed = Math.Max(1f, _topSpeed);
            var ratio = speedKph / referenceTopSpeed;
            if (ratio <= 0f)
                return 0f;
            if (ratio >= maxRatio)
                return maxRatio;
            return ratio;
        }
    }
}

