using System;
using System.Collections.Generic;
using SoundFlow.Enums;

namespace TS.Audio
{
    public sealed partial class AudioSourceHandle
    {
        public void Play(bool loop = false)
        {
            Play(loop, 0f);
        }

        public void Play(bool loop, float fadeInSeconds)
        {
            ThrowIfDisposed();
            lock (_stateSync)
            {
                _looping = loop;
                _player.IsLooping = loop;
                _pendingStartDiagnostic = true;
                if (ShouldCullOneShotForDistanceUnsafe(loop))
                {
                    StopImmediatelyUnsafe();
                    _pendingStartDiagnostic = false;
                    return;
                }

                if (ShouldRestartForPlayUnsafe(loop) && _player.Seek(0))
                {
                    PrimeForImmediatePlayback();
                    _reachedEnd = false;
                }

                if (fadeInSeconds > 0f)
                {
                    CancelFadeUnsafe();
                    _currentVolume = 0f;
                    ApplyPan();
                    _player.Play();
                    BeginFadeUnsafe(_userVolume, fadeInSeconds, stopAfter: false);
                }
                else
                {
                    CancelFadeUnsafe();
                    _currentVolume = _userVolume;
                    ApplyPan();
                    _player.Play();
                }
            }

            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourcePlayRequested,
                AudioDiagnosticEntityType.Source,
                _output.Name,
                _bus.Name,
                null,
                "Audio source playback requested.",
                new Dictionary<string, object?>
                {
                    ["sourceId"] = SourceId,
                    ["loop"] = loop,
                    ["fadeInSeconds"] = fadeInSeconds
                },
                () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
        }

        public void Stop()
        {
            Stop(0f);
        }

        public void Stop(float fadeOutSeconds)
        {
            if (_disposed)
                return;

            lock (_stateSync)
            {
                _pendingStartDiagnostic = false;
                if (fadeOutSeconds > 0f && _player.State == PlaybackState.Playing)
                {
                    BeginFadeUnsafe(0f, fadeOutSeconds, stopAfter: true);
                }
                else
                {
                    StopImmediatelyUnsafe();
                }
            }

            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceStopped,
                AudioDiagnosticEntityType.Source,
                _output.Name,
                _bus.Name,
                null,
                "Audio source stopped.",
                new Dictionary<string, object?>
                {
                    ["sourceId"] = SourceId,
                    ["fadeOutSeconds"] = fadeOutSeconds
                },
                () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
        }

        public void Pause()
        {
            ThrowIfDisposed();
            _player.Pause();
        }

        public void Resume()
        {
            ThrowIfDisposed();
            _player.Play();
        }

        public void FadeIn(float seconds)
        {
            Play(_looping, seconds);
        }

        public void FadeOut(float seconds)
        {
            Stop(seconds);
        }

        public void SeekToStart()
        {
            ThrowIfDisposed();
            _pendingStartDiagnostic = false;
            lock (_stateSync)
            {
                if (_provider.CanSeek && _player.Seek(0))
                {
                    CancelFadeUnsafe();
                    _currentVolume = _userVolume;
                    ApplyPan();
                    PrimeForImmediatePlayback();
                    _steamAudioSpatial?.ClearSimulationState();
                    _steamAudioSpatial?.Reset();
                    _reachedEnd = false;
                    _output.Diagnostics.EmitDeferred(
                        AudioDiagnosticLevel.Debug,
                        AudioDiagnosticKind.SourceSeeked,
                        AudioDiagnosticEntityType.Source,
                        _output.Name,
                        _bus.Name,
                        null,
                        "Audio source seeked.",
                        new Dictionary<string, object?>
                        {
                            ["sourceId"] = SourceId,
                            ["timeSeconds"] = 0f
                        },
                        () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
                }
            }
        }

        public void SetLooping(bool looping)
        {
            ThrowIfDisposed();
            _looping = looping;
            _player.IsLooping = looping;
            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceLoopingChanged,
                AudioDiagnosticEntityType.Source,
                _output.Name,
                _bus.Name,
                null,
                "Audio source looping changed.",
                new Dictionary<string, object?>
                {
                    ["sourceId"] = SourceId,
                    ["looping"] = looping
                },
                () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
        }

        public void SetOnEnd(Action onEnd)
        {
            ThrowIfDisposed();
            _onEnd = onEnd;
        }

        internal void AddEndObserver(Action onEnd)
        {
            ThrowIfDisposed();
            if (onEnd == null)
                throw new ArgumentNullException(nameof(onEnd));

            if (!_endObservers.Contains(onEnd))
                _endObservers.Add(onEnd);
        }

        internal void RemoveEndObserver(Action onEnd)
        {
            if (_disposed || onEnd == null)
                return;

            _endObservers.Remove(onEnd);
        }

        private void ApplyPan()
        {
            var effectivePan = _steamAudioSpatial?.IsBinauralActive == true
                ? 0f
                : Clamp(_pan + _spatialPan, -1f, 1f);
            var soundFlowPan = (effectivePan + 1f) * 0.5f;
            _player.Pan = soundFlowPan;
            _player.Volume = _currentVolume * _spatialGain;
        }

        private void ApplyPlaybackSpeed()
        {
            _player.PlaybackSpeed = 1f;
            _provider.SetRate(Math.Max(0.001f, _pitch * _spatialPitch));
        }

        private void PrimeForImmediatePlayback()
        {
            if (_asset.LengthSeconds <= 0f)
                return;

            var primeFrames = Math.Max(256, (int)_output.PeriodSizeInFrames * 2);
            _provider.Prime(primeFrames);
        }

        private void StopImmediatelyUnsafe()
        {
            CancelFadeUnsafe();
            _player.Stop();
            _steamAudioSpatial?.ClearSimulationState();
            _steamAudioSpatial?.Reset();
            _reachedEnd = false;
            _currentVolume = _userVolume;
            ApplyPan();
        }

        private bool ShouldRestartForPlayUnsafe(bool loop)
        {
            if (!_provider.CanSeek)
                return false;

            return _reachedEnd
                || _player.State == PlaybackState.Stopped
                || !loop;
        }

        private bool ShouldCullOneShotForDistanceUnsafe(bool loop)
        {
            return !loop
                && _spatialize
                && _distanceAttenuation <= 0.0001f;
        }

        private void BeginFadeUnsafe(float targetVolume, float durationSeconds, bool stopAfter)
        {
            _fadeDuration = Math.Max(0.0001f, durationSeconds);
            _fadeRemaining = _fadeDuration;
            _fadeStartVolume = _currentVolume;
            _fadeTargetVolume = Math.Max(0f, targetVolume);
            _stopAfterFade = stopAfter;
        }

        private void CancelFadeUnsafe()
        {
            _fadeDuration = 0f;
            _fadeRemaining = 0f;
            _fadeStartVolume = _currentVolume;
            _fadeTargetVolume = _currentVolume;
            _stopAfterFade = false;
        }

        private void UpdateFade(double deltaTime)
        {
            lock (_stateSync)
            {
                if (_fadeRemaining <= 0f)
                    return;

                _fadeRemaining -= (float)deltaTime;
                if (_fadeRemaining <= 0f)
                {
                    _currentVolume = _fadeTargetVolume;
                    ApplyPan();

                    var shouldStop = _stopAfterFade;
                    CancelFadeUnsafe();
                    if (shouldStop)
                        StopImmediatelyUnsafe();

                    return;
                }

                var progress = 1f - (_fadeRemaining / _fadeDuration);
                _currentVolume = _fadeStartVolume + ((_fadeTargetVolume - _fadeStartVolume) * progress);
                ApplyPan();
            }
        }
    }
}
