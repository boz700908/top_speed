using System;
using System.Numerics;
using System.Threading;
using MiniAudioEx.Native;

namespace TS.Audio
{
    public sealed partial class AudioSourceHandle : IDisposable
    {
        private const float MaxDistanceInfinite = 1000000000f;

        private readonly AudioOutput _output;
        private readonly AudioAsset _asset;
        private readonly bool _ownsAsset;
        private readonly SourcePlayback _playback;
        private readonly AudioBus _bus;
        private readonly IntPtr _sourceHandle;
        private readonly ma_sound_group_ptr _group;
        private readonly AudioSourceSpatialParams _spatial;
        private readonly bool _spatialize;
        private readonly SourceGraph _graph;
        private bool _disposed;
        private bool _disposeRequested;
        private bool _looping;
        private bool _notifiedEnd;
        private Action? _onEnd;
        private float _basePitch = 1.0f;
        private float _dopplerFactor = 1.0f;
        private float _pan;
        private float _userVolume = 1.0f;
        private float _currentVolume = 1.0f;
        private float _fadeDuration;
        private float _fadeRemaining;
        private float _fadeStartVolume;
        private float _fadeTargetVolume;
        private bool _stopAfterFade;

        internal AudioSourceHandle(AudioOutput output, AudioAsset asset, bool spatialize, bool useHrtf, AudioBus bus, bool ownsAsset = true)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _asset = asset ?? throw new ArgumentNullException(nameof(asset));
            _ownsAsset = ownsAsset;
            _playback = asset.CreatePlayback();
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _spatial = new AudioSourceSpatialParams();
            _spatialize = spatialize;
            _sourceHandle = MiniAudioExNative.ma_ex_audio_source_init(output.Runtime.ContextHandle);
            if (_sourceHandle == IntPtr.Zero)
                throw new InvalidOperationException("Failed to initialize audio source.");

            _group = new ma_sound_group_ptr(true);
            var groupInit = MiniAudioNative.ma_sound_group_init(output.Runtime.EngineHandle, 0, default, _group);
            if (groupInit != ma_result.success)
            {
                MiniAudioExNative.ma_ex_audio_source_uninit(_sourceHandle);
                throw new InvalidOperationException("Failed to initialize audio source group: " + groupInit);
            }

            var setGroup = MiniAudioExNative.ma_ex_audio_source_set_group(_sourceHandle, _group.pointer);
            if (setGroup != ma_result.success)
            {
                MiniAudioNative.ma_sound_group_uninit(_group);
                _group.Free();
                MiniAudioExNative.ma_ex_audio_source_uninit(_sourceHandle);
                throw new InvalidOperationException("Failed to bind audio source group: " + setGroup);
            }

            _graph = new SourceGraph(output, bus, _group, _spatial, spatialize, useHrtf);

            InitializeVolumeState();
            _graph.Configure();
            ApplyPersistedState();
        }

        public bool IsPlaying => !_disposeRequested && MiniAudioExNative.ma_ex_audio_source_get_is_playing(_sourceHandle) > 0;
        public int InputChannels => _asset.InputChannels;
        public int InputSampleRate => _asset.InputSampleRate;
        internal bool UsesSteamAudio => _graph.UsesHrtf;
        internal bool IsSpatialized => _spatialize;
        internal AudioSourceSpatialParams SpatialParams => _spatial;

        public void Play(bool loop)
        {
            Play(loop, 0f);
        }

        public void Play(bool loop, float fadeInSeconds)
        {
            ThrowIfDisposed();

            _looping = loop;
            _notifiedEnd = false;
            SetLooping(loop);

            if (fadeInSeconds > 0f)
            {
                CancelFade();
                _currentVolume = 0f;
                SetRuntimeVolume(0f);
                StartPlayback();
                BeginFade(_userVolume, fadeInSeconds, stopAfter: false);
                return;
            }

            CancelFade();
            _currentVolume = _userVolume;
            SetRuntimeVolume(_currentVolume);
            StartPlayback();
        }

        public void Stop()
        {
            Stop(0f);
        }

        public void Stop(float fadeOutSeconds)
        {
            ThrowIfDisposed();

            if (fadeOutSeconds <= 0f || !IsPlaying)
            {
                CancelFade();
                MiniAudioExNative.ma_ex_audio_source_stop(_sourceHandle);
                _notifiedEnd = false;
                return;
            }

            BeginFade(0f, fadeOutSeconds, stopAfter: true);
        }

        public void FadeIn(float seconds)
        {
            ThrowIfDisposed();

            if (seconds <= 0f)
            {
                CancelFade();
                _currentVolume = _userVolume;
                SetRuntimeVolume(_currentVolume);
                return;
            }

            if (!IsPlaying)
                Play(_looping, seconds);
            else
                BeginFade(_userVolume, seconds, stopAfter: false);
        }

        public void FadeOut(float seconds)
        {
            ThrowIfDisposed();
            if (seconds <= 0f)
            {
                Stop();
                return;
            }

            BeginFade(0f, seconds, stopAfter: true);
        }

        public void SetVolume(float volume)
        {
            ThrowIfDisposed();
            _userVolume = Math.Max(0f, volume);
            if (_fadeRemaining > 0f && !_stopAfterFade)
            {
                _fadeTargetVolume = _userVolume;
                return;
            }

            _currentVolume = _userVolume;
            SetRuntimeVolume(_currentVolume);
        }

        public float GetVolume()
        {
            return MiniAudioNative.ma_sound_group_get_volume(_group);
        }

        public void SetPitch(float pitch)
        {
            ThrowIfDisposed();
            _basePitch = pitch;
            MiniAudioNative.ma_sound_group_set_pitch(_group, pitch);
        }

        public float GetPitch()
        {
            return MiniAudioNative.ma_sound_group_get_pitch(_group);
        }

        public void SetPan(float pan)
        {
            ThrowIfDisposed();
            _pan = pan;
            if (_spatialize)
                return;

            MiniAudioNative.ma_sound_group_set_pan(_group, pan);
        }

        public void SetStereoWidening(bool enabled)
        {
            if (!_spatialize)
                return;

            Volatile.Write(ref _spatial.StereoWidening, enabled ? 1 : 0);
        }

        public void SetLooping(bool loop)
        {
            _looping = loop;
            MiniAudioExNative.ma_ex_audio_source_set_loop(_sourceHandle, loop ? 1u : 0u);
        }

        public void SeekToStart()
        {
            if (!_playback.SupportsSeeking)
                return;

            MiniAudioExNative.ma_ex_audio_source_set_pcm_position(_sourceHandle, 0);
        }

        public float GetLengthSeconds()
        {
            if (_asset.LengthSeconds > 0f)
                return _asset.LengthSeconds;

            var frames = MiniAudioExNative.ma_ex_audio_source_get_pcm_length(_sourceHandle);
            if (frames > 0 && _asset.InputSampleRate > 0)
                return (float)(frames / (double)_asset.InputSampleRate);

            return 0f;
        }

        public void SetOnEnd(Action onEnd)
        {
            _onEnd = onEnd;
        }

        internal AudioSourceSnapshot CaptureSnapshot()
        {
            return new AudioSourceSnapshot(_bus.Name, IsPlaying, _spatialize, _graph.UsesHrtf, InputChannels, InputSampleRate, GetLengthSeconds());
        }

        private void ThrowIfDisposed()
        {
            if (_disposed || _disposeRequested)
                throw new ObjectDisposedException(nameof(AudioSourceHandle));
        }
    }
}
