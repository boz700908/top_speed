using System;
using TopSpeed.Common;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private Source SelectRandomCrashHandle()
        {
            if (_soundCrashVariants.Length == 0)
                return _soundCrash;
            return _soundCrashVariants[Algorithm.RandomInt(_soundCrashVariants.Length)];
        }

        private bool AnyBackfirePlaying()
        {
            for (var i = 0; i < _soundBackfireVariants.Length; i++)
            {
                if (_soundBackfireVariants[i].IsPlaying)
                    return true;
            }
            return false;
        }

        private void PlayRandomBackfire()
        {
            if (_soundBackfireVariants.Length == 0)
                return;
            _soundBackfire = _soundBackfireVariants[Algorithm.RandomInt(_soundBackfireVariants.Length)];
            _soundBackfire.Play(loop: false);
        }

        private void StopResetBackfireVariants()
        {
            for (var i = 0; i < _soundBackfireVariants.Length; i++)
            {
                if (_soundBackfireVariants[i].IsPlaying)
                    _soundBackfireVariants[i].Stop();
                _soundBackfireVariants[i].SeekToStart();
            }
        }
    }
}

