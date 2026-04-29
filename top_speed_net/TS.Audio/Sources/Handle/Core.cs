using System;
using System.Collections.Generic;
using System.Numerics;
using SoundFlow.Enums;

namespace TS.Audio
{
    public sealed partial class AudioSourceHandle : IDisposable
    {
        private static int _nextId;
        private readonly AudioOutput _output;
        private readonly AudioAsset _asset;
        private readonly bool _ownsAsset;
        private readonly AudioBus _bus;
        private readonly object _stateSync;
        private readonly SourcePlayer _player;
        private readonly VariableRateDataProvider _provider;
        private readonly bool _spatialize;
        private readonly bool _allowHrtf;
        private SteamAudioSpatialModifier? _steamAudioSpatial;
        private Action? _onEnd;
        private readonly List<Action> _endObservers;
        private float _userVolume = 1f;
        private float _currentVolume = 1f;
        private float _pitch = 1f;
        private float _pan;
        private float _spatialPan;
        private float _spatialPitch = 1f;
        private float _spatialGain = 1f;
        private float _distanceAttenuation = 1f;
        private float _fadeDuration;
        private float _fadeRemaining;
        private float _fadeStartVolume;
        private float _fadeTargetVolume;
        private bool _stopAfterFade;
        private bool _looping;
        private bool _stereoWidening;
        private Vector3 _position;
        private Vector3 _velocity;
        private DistanceModel _distanceModel;
        private float _minDistance;
        private float _maxDistance;
        private float _rollOff;
        private float? _curveDistanceScaler;
        private float _dopplerFactor = 1f;
        private RoomAcoustics _roomAcoustics;
        private bool _disposed;
        private bool _reachedEnd;
        private bool _pendingStartDiagnostic;

        internal AudioSourceHandle(AudioOutput output, AudioAsset asset, bool spatialize, bool useHrtf, AudioBus bus, bool ownsAsset = true)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _asset = asset ?? throw new ArgumentNullException(nameof(asset));
            _spatialize = spatialize;
            _allowHrtf = useHrtf;
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _ownsAsset = ownsAsset;
            SourceId = System.Threading.Interlocked.Increment(ref _nextId);
            _endObservers = new List<Action>();
            _stateSync = new object();
            _provider = new VariableRateDataProvider(_asset.CreateProvider(output.BackendEngine, output.SoundFlowFormat), output.SoundFlowFormat.Channels);
            _player = new SourcePlayer(output.BackendEngine, output.SoundFlowFormat, _provider)
            {
                Name = $"Source {SourceId}"
            };
            InitializeVolumeState();
            _player.PlaybackSpeed = 1f;
            _distanceModel = output.SystemConfig.DistanceModel;
            _minDistance = output.SystemConfig.MinDistance;
            _maxDistance = output.SystemConfig.MaxDistance;
            _rollOff = output.SystemConfig.RollOff;
            _curveDistanceScaler = output.SystemConfig.UseCurveDistanceScaler
                ? output.SystemConfig.CurveDistanceScaler
                : null;
            _dopplerFactor = output.SystemConfig.DopplerFactor;
            _steamAudioSpatial = CreateSpatialModifier();
            if (_steamAudioSpatial != null)
                _player.AddModifier(_steamAudioSpatial);
            _bus.Mixer.AddComponent(_player);
            ApplyPan();
            ApplyPlaybackSpeed();

            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceCreated,
                AudioDiagnosticEntityType.Source,
                _output.Name,
                _bus.Name,
                null,
                "Audio source created.",
                new Dictionary<string, object?>
                {
                    ["sourceId"] = SourceId,
                    ["inputChannels"] = InputChannels,
                    ["inputSampleRate"] = InputSampleRate,
                    ["spatialize"] = spatialize,
                    ["useHrtf"] = useHrtf
                },
                () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
        }

        public int SourceId { get; }
        public bool IsPlaying => !_disposed && _player.State == PlaybackState.Playing;
        public bool IsPaused => !_disposed && _player.State == PlaybackState.Paused;
        public int InputChannels => _asset.InputChannels;
        public int InputSampleRate => _asset.InputSampleRate;
        internal bool UsesSteamAudio => _steamAudioSpatial?.UsesSteamAudio == true;
        internal bool IsSpatialized => _spatialize;
        internal Vector3 WorldPosition => _position;
        internal Vector3 WorldVelocity => _velocity;
        internal DistanceModel DistanceModel => _distanceModel;
        internal RoomAcoustics RoomAcoustics => _roomAcoustics;
        internal bool StereoWideningEnabled => _stereoWidening;
        internal float ReferenceDistance => GetEffectiveMinDistance(_minDistance, _curveDistanceScaler);
        internal float MaximumDistance => GetEffectiveMaxDistance(ReferenceDistance, _maxDistance, _curveDistanceScaler);
        internal float Rolloff => _rollOff;

        internal AudioSourceSnapshot CaptureSnapshot()
        {
            var busVolume = _bus.GetEffectiveVolume();
            var effectiveVolume = _currentVolume * _spatialGain;
            var estimatedMix = effectiveVolume * busVolume * _output.GetMasterVolume();
            _provider.CaptureCursorState(out var providerPositionSamples, out var innerPositionSamples, out var bufferedFrames);
            return new AudioSourceSnapshot(
                SourceId,
                _bus.Name,
                IsPlaying,
                _spatialize,
                UsesSteamAudio,
                InputChannels,
                InputSampleRate,
                _looping,
                _currentVolume,
                AudioMath.GainToDecibels(_currentVolume),
                _spatialGain,
                _distanceAttenuation,
                _pitch,
                _pan,
                busVolume,
                AudioMath.GainToDecibels(busVolume),
                estimatedMix,
                AudioMath.GainToDecibels(estimatedMix),
                _bus.CaptureGainStages(),
                GetLengthSeconds(),
                _asset.DebugName,
                _output.SampleRate,
                _provider.SampleRate,
                providerPositionSamples,
                innerPositionSamples,
                bufferedFrames,
                _player.Time);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            var snapshot = CaptureSnapshot();
            if (_steamAudioSpatial != null)
            {
                _player.RemoveModifier(_steamAudioSpatial);
                _steamAudioSpatial.Dispose();
            }
            _output.RemoveSource(this);
            _player.Dispose();
            _provider.Dispose();
            if (_ownsAsset)
                _asset.Dispose();
            _output.Diagnostics.Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceDisposed,
                AudioDiagnosticEntityType.Source,
                _output.Name,
                _bus.Name,
                null,
                "Audio source disposed.",
                null,
                new AudioDiagnosticSnapshot(source: snapshot));
        }

        private void DispatchPlaybackEndedIfNeeded()
        {
            if (_disposed || !_player.ConsumePlaybackEnded())
                return;

            OnPlaybackEnded();
        }

        private void OnPlaybackEnded()
        {
            if (_disposed)
                return;

            _pendingStartDiagnostic = false;
            _reachedEnd = true;
            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceEnded,
                AudioDiagnosticEntityType.Source,
                _output.Name,
                _bus.Name,
                null,
                "Audio source reached the end of playback.",
                new Dictionary<string, object?> { ["sourceId"] = SourceId },
                () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
            _onEnd?.Invoke();

            if (_endObservers.Count == 0)
                return;

            var snapshot = _endObservers.ToArray();
            for (var i = 0; i < snapshot.Length; i++)
                snapshot[i]();
        }

        private void EmitStartedDiagnosticIfNeeded()
        {
            if (_disposed || !_pendingStartDiagnostic)
                return;

            _provider.CaptureCursorState(out var providerPositionSamples, out _, out _);
            if (providerPositionSamples <= 0)
                return;

            lock (_stateSync)
            {
                if (!_pendingStartDiagnostic)
                    return;

                _pendingStartDiagnostic = false;
            }

            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceStarted,
                AudioDiagnosticEntityType.Source,
                _output.Name,
                _bus.Name,
                SourceId,
                "Audio source playback started advancing.",
                new Dictionary<string, object?>
                {
                    ["sourceId"] = SourceId,
                    ["providerPositionSamples"] = providerPositionSamples
                },
                () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AudioSourceHandle));
        }

        private bool ShouldEmitSourceDiagnostic(AudioDiagnosticKind kind, AudioDiagnosticLevel level = AudioDiagnosticLevel.Debug)
        {
            return _output.Diagnostics.ShouldEmit(level, kind, AudioDiagnosticEntityType.Source, _output.Name, _bus.Name, SourceId);
        }

        private void InitializeVolumeState()
        {
            _userVolume = 1f;
            _currentVolume = _userVolume;
            _fadeDuration = 0f;
            _fadeRemaining = 0f;
            _fadeStartVolume = _currentVolume;
            _fadeTargetVolume = _currentVolume;
            _stopAfterFade = false;
        }

        private SteamAudioSpatialModifier? CreateSpatialModifier()
        {
            if (!_spatialize)
                return null;

            var runtime = _output.SteamAudioRuntime;
            if (runtime == null || !runtime.IsAvailable)
                return null;

            return new SteamAudioSpatialModifier(runtime, _output.Channels, _allowHrtf && _output.IsHrtfActive, _output.SystemConfig.HrtfDownmixMode);
        }

        internal void RefreshSteamAudioSpatial()
        {
            if (_disposed || !_spatialize)
                return;

            var replacement = CreateSpatialModifier();
            if (ReferenceEquals(replacement, _steamAudioSpatial))
                return;

            if (_steamAudioSpatial != null)
            {
                _player.RemoveModifier(_steamAudioSpatial);
                _steamAudioSpatial.Dispose();
            }

            _steamAudioSpatial = replacement;
            if (_steamAudioSpatial != null)
                _player.AddModifier(_steamAudioSpatial);

            lock (_stateSync)
                ApplyPan();
        }

        internal void ApplyDirectSimulation(float occlusion, float airLow, float airMid, float airHigh, float transmissionLow, float transmissionMid, float transmissionHigh)
        {
            _steamAudioSpatial?.ApplyDirectSimulation(occlusion, airLow, airMid, airHigh, transmissionLow, transmissionMid, transmissionHigh);
        }

        internal void ClearDirectSimulation()
        {
            _steamAudioSpatial?.ClearDirectSimulation();
        }

        internal void ApplyReverbSimulation(float timeLow, float timeMid, float timeHigh, float eqLow, float eqMid, float eqHigh, int delay, float wetScale)
        {
            _steamAudioSpatial?.ApplyReverbSimulation(timeLow, timeMid, timeHigh, eqLow, eqMid, eqHigh, delay, wetScale);
        }

        internal void ClearReverbSimulation()
        {
            _steamAudioSpatial?.ClearReverbSimulation();
        }

        private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback)
        {
            var lengthSquared = value.LengthSquared();
            if (lengthSquared <= 1e-6f)
                return fallback;
            return Vector3.Normalize(value);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }
    }
}
