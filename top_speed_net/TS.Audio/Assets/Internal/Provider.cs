using System;
using SoundFlow.Enums;
using SoundFlow.Interfaces;
using SoundFlow.Metadata.Models;

namespace TS.Audio
{
    internal sealed class ProceduralDataProvider : ISoundDataProvider
    {
        private readonly ProceduralAudioCallback _callback;
        private readonly int _sourceChannels;
        private readonly int _sourceSampleRate;
        private readonly int _targetChannels;
        private readonly int _targetSampleRate;
        private float[] _sourceScratch;
        private float[] _resampleScratch;
        private int _bufferedSourceFrames;
        private double _sourceFrameCursor;
        private ulong _frameIndex;

        public ProceduralDataProvider(ProceduralAudioCallback callback, int sourceChannels, int sourceSampleRate, int targetChannels, int targetSampleRate)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _sourceChannels = sourceChannels > 0 ? sourceChannels : 1;
            _sourceSampleRate = sourceSampleRate > 0 ? sourceSampleRate : 44100;
            _targetChannels = targetChannels > 0 ? targetChannels : _sourceChannels;
            _targetSampleRate = targetSampleRate > 0 ? targetSampleRate : _sourceSampleRate;
            _sourceScratch = Array.Empty<float>();
            _resampleScratch = Array.Empty<float>();
        }

        public int Position { get; private set; }
        public int Length => -1;
        public bool CanSeek => true;
        public SampleFormat SampleFormat => SampleFormat.F32;
        public int SampleRate => _targetSampleRate;
        public bool IsDisposed { get; private set; }
        public SoundFormatInfo? FormatInfo => null;

        public event EventHandler<EventArgs>? EndOfStreamReached;
        public event EventHandler<PositionChangedEventArgs>? PositionChanged;

        public int ReadBytes(Span<float> buffer)
        {
            if (IsDisposed || buffer.IsEmpty)
                return 0;

            if (_sourceChannels == _targetChannels && _sourceSampleRate == _targetSampleRate)
                return ReadDirect(buffer);

            buffer.Clear();
            var targetFrames = buffer.Length / _targetChannels;
            if (targetFrames <= 0)
                return 0;

            var sourceFramesPerTargetFrame = _sourceSampleRate / (double)_targetSampleRate;
            var requiredSourceFrames = Math.Max(2, (int)Math.Ceiling(_sourceFrameCursor + (targetFrames * sourceFramesPerTargetFrame)) + 1);
            EnsureBufferedSourceFrames(requiredSourceFrames);

            for (var targetFrame = 0; targetFrame < targetFrames; targetFrame++)
            {
                var sourcePosition = _sourceFrameCursor + (targetFrame * sourceFramesPerTargetFrame);
                var sourceFrame0 = (int)Math.Floor(sourcePosition);
                var sourceFrame1 = Math.Min(sourceFrame0 + 1, Math.Max(0, _bufferedSourceFrames - 1));
                var fraction = (float)(sourcePosition - sourceFrame0);
                WriteConvertedFrame(buffer, targetFrame, sourceFrame0, sourceFrame1, fraction);
            }

            AdvanceBufferedFrames(targetFrames * sourceFramesPerTargetFrame);
            Position += buffer.Length;
            PositionChanged?.Invoke(this, new PositionChangedEventArgs(Position));
            return buffer.Length;
        }

        public void Seek(int offset)
        {
            if (offset < 0)
                offset = 0;

            Position = offset;
            _frameIndex = (ulong)Math.Max(0, (int)Math.Round((offset / (double)Math.Max(1, _targetChannels)) * _sourceSampleRate / Math.Max(1, _targetSampleRate)));
            _bufferedSourceFrames = 0;
            _sourceFrameCursor = 0d;
            PositionChanged?.Invoke(this, new PositionChangedEventArgs(Position));
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            if (EndOfStreamReached != null)
                EndOfStreamReached = null;
            PositionChanged = null;
        }

        private int ReadDirect(Span<float> buffer)
        {
            if (_sourceScratch.Length < buffer.Length)
                _sourceScratch = new float[buffer.Length];

            Array.Clear(_sourceScratch, 0, buffer.Length);
            var frameIndex = _frameIndex;
            try
            {
                var frames = buffer.Length / _sourceChannels;
                _callback(_sourceScratch, frames, _sourceChannels, ref frameIndex);
            }
            catch
            {
                Array.Clear(_sourceScratch, 0, buffer.Length);
            }

            _frameIndex = frameIndex;
            _sourceScratch.AsSpan(0, buffer.Length).CopyTo(buffer);
            Position += buffer.Length;
            PositionChanged?.Invoke(this, new PositionChangedEventArgs(Position));
            return buffer.Length;
        }

        private void EnsureBufferedSourceFrames(int requiredFrames)
        {
            if (_bufferedSourceFrames >= requiredFrames)
                return;

            var requiredSamples = requiredFrames * _sourceChannels;
            if (_sourceScratch.Length < requiredSamples)
                Array.Resize(ref _sourceScratch, Math.Max(requiredSamples, _sourceScratch.Length == 0 ? requiredSamples : _sourceScratch.Length * 2));

            while (_bufferedSourceFrames < requiredFrames)
            {
                var framesToGenerate = Math.Max(64, requiredFrames - _bufferedSourceFrames);
                EnsureResampleScratch(framesToGenerate * _sourceChannels);
                Array.Clear(_resampleScratch, 0, framesToGenerate * _sourceChannels);

                var frameIndex = _frameIndex;
                try
                {
                    _callback(_resampleScratch, framesToGenerate, _sourceChannels, ref frameIndex);
                }
                catch
                {
                    Array.Clear(_resampleScratch, 0, framesToGenerate * _sourceChannels);
                }

                _frameIndex = frameIndex;
                _resampleScratch.AsSpan(0, framesToGenerate * _sourceChannels)
                    .CopyTo(_sourceScratch.AsSpan(_bufferedSourceFrames * _sourceChannels));
                _bufferedSourceFrames += framesToGenerate;
            }
        }

        private void AdvanceBufferedFrames(double framesAdvanced)
        {
            _sourceFrameCursor += framesAdvanced;
            var framesToDrop = (int)Math.Floor(_sourceFrameCursor);
            if (framesToDrop <= 0)
                return;

            framesToDrop = Math.Min(framesToDrop, Math.Max(0, _bufferedSourceFrames - 1));
            if (framesToDrop > 0)
            {
                var remainingSamples = (_bufferedSourceFrames - framesToDrop) * _sourceChannels;
                _sourceScratch.AsSpan(framesToDrop * _sourceChannels, remainingSamples)
                    .CopyTo(_sourceScratch.AsSpan(0, remainingSamples));
                _bufferedSourceFrames -= framesToDrop;
                _sourceFrameCursor -= framesToDrop;
            }
        }

        private void WriteConvertedFrame(Span<float> targetBuffer, int targetFrame, int sourceFrame0, int sourceFrame1, float fraction)
        {
            Span<float> sourceFrame = stackalloc float[Math.Min(_sourceChannels, 8)];
            if (_sourceChannels > sourceFrame.Length)
                sourceFrame = new float[_sourceChannels];

            var base0 = sourceFrame0 * _sourceChannels;
            var base1 = sourceFrame1 * _sourceChannels;
            for (var channel = 0; channel < _sourceChannels; channel++)
            {
                var sample0 = _sourceScratch[base0 + channel];
                var sample1 = _sourceScratch[base1 + channel];
                sourceFrame[channel] = sample0 + ((sample1 - sample0) * fraction);
            }

            var targetBase = targetFrame * _targetChannels;
            if (_targetChannels == _sourceChannels)
            {
                sourceFrame.Slice(0, _targetChannels).CopyTo(targetBuffer.Slice(targetBase, _targetChannels));
                return;
            }

            if (_sourceChannels == 1)
            {
                var mono = sourceFrame[0];
                for (var channel = 0; channel < _targetChannels; channel++)
                    targetBuffer[targetBase + channel] = mono;
                return;
            }

            if (_targetChannels == 1)
            {
                float sum = 0f;
                for (var channel = 0; channel < _sourceChannels; channel++)
                    sum += sourceFrame[channel];
                targetBuffer[targetBase] = sum / _sourceChannels;
                return;
            }

            float average = 0f;
            for (var channel = 0; channel < _sourceChannels; channel++)
                average += sourceFrame[channel];
            average /= _sourceChannels;

            for (var channel = 0; channel < _targetChannels; channel++)
                targetBuffer[targetBase + channel] = channel < _sourceChannels ? sourceFrame[channel] : average;
        }

        private void EnsureResampleScratch(int requiredSamples)
        {
            if (_resampleScratch.Length < requiredSamples)
                Array.Resize(ref _resampleScratch, Math.Max(requiredSamples, _resampleScratch.Length == 0 ? requiredSamples : _resampleScratch.Length * 2));
        }
    }
}
