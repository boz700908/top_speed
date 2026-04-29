using System;
using System.Collections.Generic;
using System.Numerics;

namespace TS.Audio
{
    public sealed partial class AudioEngine : IDisposable
    {
        private const int OneShotPoolLimitPerKey = 8;
        private readonly AudioSystem _system;
        private readonly AudioOutput _primaryOutput;
        private readonly AudioOutput _speechOutput;
        private readonly Dictionary<string, AudioBus> _buses;
        private readonly PlaybackPolicy _defaults;
        private readonly object _transientLock;
        private readonly HashSet<Source> _transientSources;
        private readonly Queue<Source> _retiredSources;
        private readonly object _oneShotPoolLock;
        private readonly Dictionary<OneShotPoolKey, Stack<Source>> _pooledOneShots;
        private readonly Dictionary<Source, OneShotPoolKey> _activePooledOneShots;
        private readonly Queue<PooledOneShotReturn> _pendingPooledOneShotReturns;
        private readonly bool _hasDedicatedSpeechOutput;
        private readonly Action<Source> _transientRetireHandler;
        private readonly Action<Source> _pooledOneShotReturnHandler;
        private bool _disposed;

        public AssetLibrary Assets { get; }
        public AudioSystem System => _system;
        public AudioDiagnostics Diagnostics => _system.Diagnostics;
        public AudioOutput PrimaryOutput => _primaryOutput;
        public AudioOutput SpeechOutput => _speechOutput;
        public IReadOnlyDictionary<string, AudioBus> Buses => _buses;
        public PlaybackPolicy Defaults => _defaults;

        public AudioBus MainBus => _primaryOutput.MainBus;
        public AudioBus WorldBus => GetBus(AudioEngineOptions.WorldBusName);
        public AudioBus UiBus => GetBus(AudioEngineOptions.UiBusName);
        public AudioBus MusicBus => GetBus(AudioEngineOptions.MusicBusName);
        public AudioBus VehiclesBus => GetBus(AudioEngineOptions.VehiclesBusName);
        public AudioBus TrackBus => GetBus(AudioEngineOptions.TrackBusName);
        public AudioBus CopilotBus => GetBus(AudioEngineOptions.CopilotBusName);
        public AudioBus RadioBus => GetBus(AudioEngineOptions.RadioBusName);
        public AudioBus SpeechBus => GetBus(AudioEngineOptions.SpeechBusName);

        public AudioEngine(AudioEngineOptions? options = null)
        {
            var resolved = options ?? new AudioEngineOptions();
            var systemConfig = resolved.SystemConfig ?? new AudioSystemConfig();
            _system = new AudioSystem(systemConfig);
            Assets = new AssetLibrary();
            _buses = new Dictionary<string, AudioBus>(StringComparer.OrdinalIgnoreCase);
            _defaults = ResolveSystemDefaults(systemConfig, resolved.Defaults);
            _transientLock = new object();
            _transientSources = new HashSet<Source>();
            _retiredSources = new Queue<Source>();
            _oneShotPoolLock = new object();
            _pooledOneShots = new Dictionary<OneShotPoolKey, Stack<Source>>();
            _activePooledOneShots = new Dictionary<Source, OneShotPoolKey>();
            _pendingPooledOneShotReturns = new Queue<PooledOneShotReturn>();
            _hasDedicatedSpeechOutput = resolved.UseDedicatedSpeechOutput;
            _transientRetireHandler = QueueTransientRetire;
            _pooledOneShotReturnHandler = QueuePooledOneShotReturn;

            _primaryOutput = _system.CreateOutput(resolved.PrimaryOutput ?? new AudioOutputConfig { Name = "main" });
            _speechOutput = _hasDedicatedSpeechOutput
                ? _system.CreateOutput(resolved.SpeechOutput ?? new AudioOutputConfig { Name = "speech" })
                : _primaryOutput;

            _buses["main"] = _primaryOutput.MainBus;
            _primaryOutput.MainBus.ApplyDefaults(_defaults);
            if (_hasDedicatedSpeechOutput)
            {
                _buses[AudioEngineOptions.SpeechBusName] = _speechOutput.MainBus;
                _speechOutput.MainBus.ApplyDefaults(_defaults);
            }

            for (var i = 0; i < resolved.Buses.Count; i++)
                CreateConfiguredBus(resolved.Buses[i]);
        }

        public AudioBus GetBus(string name)
        {
            if (!_buses.TryGetValue(name, out var bus))
                throw new KeyNotFoundException("Audio bus not found: " + name);
            return bus;
        }

        public bool TryGetBus(string name, out AudioBus bus)
        {
            return _buses.TryGetValue(name, out bus!);
        }

        public bool TryResolveFile(string path, out string fullPath)
        {
            return Assets.TryResolveFile(path, out fullPath!);
        }

        public Clip LoadClip(string filePath, bool streamFromDisk = true, bool cache = true)
        {
            return Assets.LoadClip(filePath, streamFromDisk, cache);
        }

        public StreamAsset LoadStream(string filePath, bool cache = true)
        {
            return Assets.LoadStream(filePath, cache);
        }

        public BufferAsset CreateBufferAsset(byte[] data, string? name = null)
        {
            return Assets.CreateBuffer(data, name);
        }

        public GeneratorAsset CreateGeneratorAsset(ProceduralAudioCallback callback, uint channels = 1, uint sampleRate = 44100, string? name = null)
        {
            return Assets.CreateProcedural(callback, channels, sampleRate, name);
        }

        public void SetListener(Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity)
        {
            _system.UpdateListenerAll(position, forward, up, velocity);
        }

        public void Update()
        {
            DrainRetiredVoices();
            DrainReturnedPooledOneShots();
            _system.Update();
            DrainReturnedPooledOneShots();
            DrainRetiredVoices();
        }

        public AudioEngineSnapshot CaptureSnapshot()
        {
            var outputs = new List<AudioOutputSnapshot>(_system.Outputs.Count);
            foreach (var output in _system.Outputs.Values)
                outputs.Add(output.CaptureSnapshot());
            return new AudioEngineSnapshot(DateTime.UtcNow, outputs);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ClearTransientVoices();
            ClearPooledOneShots();
            Assets.Dispose();
            _system.Dispose();
            _buses.Clear();
        }

        private AudioBus ResolveBus(string? busName)
        {
            if (string.IsNullOrWhiteSpace(busName))
                return MainBus;
            return GetBus(busName!);
        }

        private void CreateConfiguredBus(AudioBusOptions options)
        {
            if (options == null || string.IsNullOrWhiteSpace(options.Name))
                return;

            if (_buses.ContainsKey(options.Name))
            {
                var existing = _buses[options.Name];
                existing.SetVolume(options.Volume);
                existing.SetMuted(options.Muted);
                existing.ApplyDefaults(ResolveDefaults(options.Defaults));
                return;
            }

            var output = options.UseSpeechOutput && _hasDedicatedSpeechOutput
                ? _speechOutput
                : _primaryOutput;
            AudioBus? parent = null;
            if (!string.IsNullOrWhiteSpace(options.ParentName))
                _buses.TryGetValue(options.ParentName!, out parent);
            parent ??= output.MainBus;

            var bus = output.CreateBus(options.Name, parent, ResolveDefaults(options.Defaults));
            bus.SetVolume(options.Volume);
            bus.SetMuted(options.Muted);
            _buses[options.Name] = bus;
        }

        private PlaybackPolicy ResolveDefaults(PlaybackPolicy? busDefaults)
        {
            var merged = _defaults.Clone();
            if (busDefaults == null)
                return merged;

            if (busDefaults.Spatialize.HasValue)
                merged.Spatialize = busDefaults.Spatialize;
            if (busDefaults.UseHrtf.HasValue)
                merged.UseHrtf = busDefaults.UseHrtf;
            if (busDefaults.Loop.HasValue)
                merged.Loop = busDefaults.Loop;
            if (busDefaults.FadeInSeconds.HasValue)
                merged.FadeInSeconds = busDefaults.FadeInSeconds;
            if (busDefaults.Volume.HasValue)
                merged.Volume = busDefaults.Volume;
            if (busDefaults.Pitch.HasValue)
                merged.Pitch = busDefaults.Pitch;
            if (busDefaults.Pan.HasValue)
                merged.Pan = busDefaults.Pan;
            if (busDefaults.StereoWidening.HasValue)
                merged.StereoWidening = busDefaults.StereoWidening;
            if (busDefaults.Position.HasValue)
                merged.Position = busDefaults.Position;
            if (busDefaults.Velocity.HasValue)
                merged.Velocity = busDefaults.Velocity;
            if (busDefaults.CurveDistanceScaler.HasValue)
                merged.CurveDistanceScaler = busDefaults.CurveDistanceScaler;
            if (busDefaults.DopplerFactor.HasValue)
                merged.DopplerFactor = busDefaults.DopplerFactor;
            if (busDefaults.RoomAcoustics.HasValue)
                merged.RoomAcoustics = busDefaults.RoomAcoustics;
            if (busDefaults.DistanceModel.HasValue)
                merged.DistanceModel = busDefaults.DistanceModel;
            if (busDefaults.RefDistance.HasValue)
                merged.RefDistance = busDefaults.RefDistance;
            if (busDefaults.MaxDistance.HasValue)
                merged.MaxDistance = busDefaults.MaxDistance;
            if (busDefaults.RollOff.HasValue)
                merged.RollOff = busDefaults.RollOff;

            return merged;
        }

        private static PlaybackPolicy ResolveSystemDefaults(AudioSystemConfig systemConfig, PlaybackPolicy defaults)
        {
            var resolved = defaults.Clone();
            resolved.UseHrtf ??= systemConfig.UseHrtf;
            resolved.DopplerFactor ??= systemConfig.DopplerFactor;
            resolved.DistanceModel ??= systemConfig.DistanceModel;
            resolved.RefDistance ??= systemConfig.MinDistance;
            resolved.MaxDistance ??= systemConfig.MaxDistance;
            resolved.RollOff ??= systemConfig.RollOff;

            if (systemConfig.UseCurveDistanceScaler)
                resolved.CurveDistanceScaler ??= systemConfig.CurveDistanceScaler;

            return resolved;
        }

        private static string ResolveBusName(string? busName)
        {
            return string.IsNullOrWhiteSpace(busName) ? "main" : busName!;
        }

        private static void ResetOneShotSource(Source source)
        {
            source.Stop();
            source.SeekToStart();
            source.SetLooping(false);
            source.SetVolume(1f);
            source.SetPitch(1f);
            source.SetPan(0f);
        }
    }
}
