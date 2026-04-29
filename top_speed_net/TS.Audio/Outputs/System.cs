using System;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Codecs.FFMpeg;
using SfAudioEngine = SoundFlow.Abstracts.AudioEngine;

namespace TS.Audio
{
    public sealed class AudioSystem : IDisposable
    {
        private readonly AudioSystemConfig _config;
        private readonly Dictionary<string, AudioOutput> _outputs;
        private long _lastUpdateTimestamp;
        private readonly AudioDiagnostics _diagnostics;
        private readonly SfAudioEngine _backendEngine;

        public IReadOnlyDictionary<string, AudioOutput> Outputs => _outputs;
        public AudioDiagnostics Diagnostics => _diagnostics;
        public bool IsInitialized => _outputs.Count > 0;
        public bool IsHrtfActive
        {
            get
            {
                foreach (var output in _outputs.Values)
                {
                    if (output.IsHrtfActive)
                        return true;
                }
                return false;
            }
        }

        public AudioSystem(AudioSystemConfig config, AudioDiagnostics? diagnostics = null)
        {
            _config = config ?? new AudioSystemConfig();
            _outputs = new Dictionary<string, AudioOutput>(StringComparer.OrdinalIgnoreCase);
            _lastUpdateTimestamp = Stopwatch.GetTimestamp();
            _diagnostics = diagnostics ?? new AudioDiagnostics();
            _backendEngine = new MiniAudioEngine();
            _backendEngine.RegisterCodecFactory(new FFmpegCodecFactory());
        }

        public AudioOutput CreateOutput(AudioOutputConfig outputConfig)
        {
            if (outputConfig == null)
                throw new ArgumentNullException(nameof(outputConfig));

            if (_outputs.ContainsKey(outputConfig.Name))
                throw new InvalidOperationException("Output already exists: " + outputConfig.Name);

            if (outputConfig.SampleRate == 0)
                outputConfig.SampleRate = _config.SampleRate;
            if (outputConfig.Channels == 0)
                outputConfig.Channels = _config.Channels;
            if (outputConfig.PeriodSizeInFrames == 0)
                outputConfig.PeriodSizeInFrames = _config.PeriodSizeInFrames;

            var output = new AudioOutput(_backendEngine, outputConfig, _config, _diagnostics);
            _outputs.Add(outputConfig.Name, output);
            _diagnostics.Emit(
                AudioDiagnosticLevel.Info,
                AudioDiagnosticKind.OutputCreated,
                AudioDiagnosticEntityType.Output,
                output.Name,
                null,
                null,
                "Audio output created.",
                new Dictionary<string, object?>
                {
                    ["sampleRate"] = output.SampleRate,
                    ["channels"] = output.Channels,
                    ["periodSizeInFrames"] = output.PeriodSizeInFrames,
                    ["hrtfActive"] = output.IsHrtfActive
                });
            return output;
        }

        public AudioOutput GetOutput(string name)
        {
            if (!_outputs.TryGetValue(name, out var output))
                throw new KeyNotFoundException("Output not found: " + name);
            return output;
        }

        public bool TryGetOutput(string name, out AudioOutput output)
        {
            return _outputs.TryGetValue(name, out output!);
        }

        public bool RemoveOutput(string name)
        {
            if (!_outputs.TryGetValue(name, out var output))
                return false;

            output.Dispose();
            _outputs.Remove(name);
            return true;
        }

        public void Update()
        {
            var nowTimestamp = Stopwatch.GetTimestamp();
            var elapsedTicks = nowTimestamp - _lastUpdateTimestamp;
            if (elapsedTicks < 0)
                elapsedTicks = 0;
            var dt = (double)elapsedTicks / Stopwatch.Frequency;

            foreach (var output in _outputs.Values)
            {
                output.Update(dt);
            }

            _lastUpdateTimestamp = nowTimestamp;
        }

        public void UpdateListenerAll(Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity)
        {
            foreach (var output in _outputs.Values)
            {
                output.UpdateListener(position, forward, up, velocity);
            }
        }

        public void UpdateListenerFromTdv(float x, float y, float z, float vx, float vy, float vz, bool upright)
        {
            var pos = CoordinateMapper.ToAudioPosition(x, y, z);
            var vel = CoordinateMapper.ToAudioVelocity(vx, vy, vz, _config.UseVerticalVelocity);
            var forward = CoordinateMapper.ToAudioForward(vx, vy, vz);
            var up = new Vector3(0f, upright ? 1f : -1f, 0f);

            UpdateListenerAll(pos, forward, up, vel);
        }

        public void Dispose()
        {
            foreach (var output in _outputs.Values)
            {
                output.Dispose();
            }
            _outputs.Clear();
            _backendEngine.Dispose();
        }
    }
}
