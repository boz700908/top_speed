using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using SteamAudio;

namespace TS.Audio
{
    internal sealed unsafe partial class SteamAudioRuntime : IDisposable
    {
        internal sealed class ListenerState
        {
            public ListenerState(IPL.Vector3 right, IPL.Vector3 up, IPL.Vector3 ahead, IPL.Vector3 origin)
            {
                Right = right;
                Up = up;
                Ahead = ahead;
                Origin = origin;
            }

            public IPL.Vector3 Right { get; }
            public IPL.Vector3 Up { get; }
            public IPL.Vector3 Ahead { get; }
            public IPL.Vector3 Origin { get; }
        }

        private const float ReflectionDurationSeconds = 0.5f;
        private const int ReflectionOrder = 0;
        private readonly object _simulationLock = new object();
        private readonly Dictionary<AudioSourceHandle, IPL.Source> _sources = new Dictionary<AudioSourceHandle, IPL.Source>();
        private readonly HashSet<AudioSourceHandle> _activeSources = new HashSet<AudioSourceHandle>();
        private readonly List<AudioSourceHandle> _sourcesToRemove = new List<AudioSourceHandle>();
        private IPL.Context _context;
        private IPL.Hrtf _hrtf;
        private IPL.Simulator _simulator;
        private IPL.Scene _scene;
        private bool _disposed;
        private volatile ListenerState _listenerState;

        private SteamAudioRuntime(IPL.Context context, IPL.Hrtf hrtf, IPL.AudioSettings audioSettings, bool hrtfAvailable)
        {
            _context = context;
            _hrtf = hrtf;
            AudioSettings = audioSettings;
            HrtfAvailable = hrtfAvailable;
            ReflectionChannels = (ReflectionOrder + 1) * (ReflectionOrder + 1);
            ReflectionType = IPL.ReflectionEffectType.Parametric;
            _listenerState = CreateIdentityListener();
        }

        public IPL.Context Context => _context;
        public IPL.Hrtf Hrtf => _hrtf;
        public IPL.AudioSettings AudioSettings { get; }
        public bool HrtfAvailable { get; }
        public bool IsAvailable => Context.Handle != IntPtr.Zero;
        public int ReflectionChannels { get; }
        public IPL.ReflectionEffectType ReflectionType { get; }
        public bool HasScene => _scene.Handle != IntPtr.Zero;
        internal bool SupportsReflections => ReflectionType == IPL.ReflectionEffectType.Parametric;
        internal ListenerState ListenerSnapshot => _listenerState;

        public static SteamAudioRuntime? TryCreate(AudioSystemConfig config, int sampleRate, int channels, int frameSize)
        {
            if (sampleRate <= 0 || channels <= 0 || frameSize <= 0)
                return null;

            IPL.Context context = default;
            IPL.Hrtf hrtf = default;

            try
            {
                var contextSettings = new IPL.ContextSettings
                {
                    Version = IPL.Version
                };

                if (IPL.ContextCreate(in contextSettings, out context) != IPL.Error.Success || context.Handle == IntPtr.Zero)
                    return null;

                var audioSettings = new IPL.AudioSettings
                {
                    SamplingRate = sampleRate,
                    FrameSize = frameSize
                };

                var hrtfAvailable = false;
                if (config.UseHrtf && channels >= 2)
                {
                    var hrtfSettings = CreateHrtfSettings(config);
                    if (IPL.HrtfCreate(context, in audioSettings, in hrtfSettings, out hrtf) == IPL.Error.Success && hrtf.Handle != IntPtr.Zero)
                        hrtfAvailable = true;
                }

                return new SteamAudioRuntime(context, hrtf, audioSettings, hrtfAvailable);
            }
            catch
            {
                if (hrtf.Handle != IntPtr.Zero)
                    IPL.HrtfRelease(ref hrtf);
                if (context.Handle != IntPtr.Zero)
                    IPL.ContextRelease(ref context);
                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            lock (_simulationLock)
            {
                foreach (var entry in _sources)
                {
                    var source = entry.Value;
                    if (source.Handle == IntPtr.Zero)
                        continue;

                    if (_simulator.Handle != IntPtr.Zero)
                        IPL.SourceRemove(source, _simulator);
                    IPL.SourceRelease(ref source);
                }
                _sources.Clear();

                if (_simulator.Handle != IntPtr.Zero)
                    IPL.SimulatorRelease(ref _simulator);

                _scene = default;
            }

            if (_hrtf.Handle != IntPtr.Zero)
                IPL.HrtfRelease(ref _hrtf);
            if (_context.Handle != IntPtr.Zero)
                IPL.ContextRelease(ref _context);
        }

        private static ListenerState CreateIdentityListener()
        {
            return new ListenerState(
                new IPL.Vector3(1f, 0f, 0f),
                new IPL.Vector3(0f, 1f, 0f),
                new IPL.Vector3(0f, 0f, -1f),
                new IPL.Vector3(0f, 0f, 0f));
        }

        private static IPL.HrtfSettings CreateHrtfSettings(AudioSystemConfig config)
        {
            var settings = new IPL.HrtfSettings
            {
                Type = IPL.HrtfType.Default,
                SofaFileName = string.Empty,
                SofaData = IntPtr.Zero,
                SofaDataSize = 0,
                Volume = 1f,
                NormType = IPL.HrtfNormType.None
            };

            if (!string.IsNullOrWhiteSpace(config.HrtfSofaPath))
            {
                var sofaPath = Path.GetFullPath(config.HrtfSofaPath);
                if (File.Exists(sofaPath))
                {
                    settings.Type = IPL.HrtfType.Sofa;
                    settings.SofaFileName = sofaPath;
                }
            }

            return settings;
        }

        private static IPL.Vector3 ToIpl(Vector3 value)
        {
            return new IPL.Vector3(value.X, value.Y, value.Z);
        }

        private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback)
        {
            var lengthSquared = value.LengthSquared();
            if (lengthSquared <= 1e-6f)
                return fallback;
            return Vector3.Normalize(value);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        private static float Lerp(float from, float to, float amount)
        {
            return from + ((to - from) * amount);
        }
    }
}
