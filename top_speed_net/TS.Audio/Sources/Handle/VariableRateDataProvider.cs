using System;
using System.Threading;
using SoundFlow.Enums;
using SoundFlow.Interfaces;
using SoundFlow.Metadata.Models;

namespace TS.Audio
{
    internal sealed class VariableRateDataProvider : ISoundDataProvider
    {
        private readonly ISoundDataProvider _inner;
        private readonly int _channels;
        private readonly object _lock;
        private float[] _inputBuffer;
        private int _validSamples;
        private double _readPosition;
        private bool _sourceEnded;
        private bool _endOfStreamRaised;
        private float _rate;

        public VariableRateDataProvider(ISoundDataProvider inner, int channels)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _channels = channels > 0 ? channels : throw new ArgumentOutOfRangeException(nameof(channels));
            _lock = new object();
            _inputBuffer = Array.Empty<float>();
            _rate = 1f;
        }

        public int Position { get; private set; }
        public int Length => _inner.Length;
        public bool CanSeek => _inner.CanSeek;
        public SampleFormat SampleFormat => _inner.SampleFormat;
        public int SampleRate => _inner.SampleRate;
        public bool IsDisposed { get; private set; }
        public SoundFormatInfo? FormatInfo => _inner.FormatInfo;

        public event EventHandler<EventArgs>? EndOfStreamReached;
        public event EventHandler<PositionChangedEventArgs>? PositionChanged;

        public void SetRate(float rate)
        {
            Volatile.Write(ref _rate, rate <= 0f ? 0.001f : rate);
        }

        public void Prime(int minimumFrames)
        {
            if (IsDisposed || minimumFrames <= 0)
                return;

            lock (_lock)
            {
                if (IsDisposed)
                    return;

                EnsureBufferedFrames(minimumFrames);
            }
        }

        public void CaptureCursorState(out int providerPositionSamples, out int innerPositionSamples, out int bufferedFrames)
        {
            lock (_lock)
            {
                providerPositionSamples = Position;
                innerPositionSamples = _inner.Position;
                bufferedFrames = _channels > 0 ? _validSamples / _channels : 0;
            }
        }

        public int ReadBytes(Span<float> buffer)
        {
            if (IsDisposed || buffer.IsEmpty)
                return 0;

            lock (_lock)
            {
                if (IsDisposed)
                    return 0;

                var outputFrames = buffer.Length / _channels;
                if (outputFrames <= 0)
                    return 0;

                var rate = Math.Max(0.001f, Volatile.Read(ref _rate));
                if (CanReadDirect(rate))
                    return ReadDirect(buffer.Slice(0, outputFrames * _channels));

                var requiredFrames = Math.Max(2, (int)Math.Ceiling(_readPosition + (outputFrames * rate)) + 1);
                EnsureBufferedFrames(requiredFrames);

                var availableFrames = _validSamples / _channels;
                if (availableFrames <= 0)
                {
                    RaiseEndOfStreamOnce();
                    return 0;
                }

                var outputFrameIndex = 0;
                for (; outputFrameIndex < outputFrames; outputFrameIndex++)
                {
                    var frameIndex0 = (int)Math.Floor(_readPosition);
                    if (frameIndex0 >= availableFrames)
                        break;

                    var frameIndex1 = Math.Min(frameIndex0 + 1, Math.Max(0, availableFrames - 1));
                    var fraction = (float)(_readPosition - frameIndex0);
                    var sourceBase0 = frameIndex0 * _channels;
                    var sourceBase1 = frameIndex1 * _channels;
                    var outputBase = outputFrameIndex * _channels;

                    for (var channel = 0; channel < _channels; channel++)
                    {
                        var sample0 = _inputBuffer[sourceBase0 + channel];
                        var sample1 = _inputBuffer[sourceBase1 + channel];
                        buffer[outputBase + channel] = sample0 + ((sample1 - sample0) * fraction);
                    }

                    _readPosition += rate;
                }

                var framesConsumed = (int)Math.Floor(_readPosition);
                if (framesConsumed > 0)
                {
                    var samplesConsumed = Math.Min(framesConsumed * _channels, _validSamples);
                    var remainingSamples = _validSamples - samplesConsumed;
                    if (remainingSamples > 0)
                    {
                        Array.Copy(_inputBuffer, samplesConsumed, _inputBuffer, 0, remainingSamples);
                    }

                    _validSamples = remainingSamples;
                    _readPosition -= framesConsumed;
                }

                var samplesGenerated = outputFrameIndex * _channels;
                Position += samplesGenerated;
                if (samplesGenerated > 0)
                    PositionChanged?.Invoke(this, new PositionChangedEventArgs(Position));
                else if (_sourceEnded)
                    RaiseEndOfStreamOnce();

                return samplesGenerated;
            }
        }

        private bool CanReadDirect(float rate)
        {
            return !_sourceEnded
                && _validSamples == 0
                && Math.Abs(rate - 1f) <= 0.0001f;
        }

        private int ReadDirect(Span<float> buffer)
        {
            var samplesRead = _inner.ReadBytes(buffer);
            if (samplesRead > 0)
            {
                Position += samplesRead;
                PositionChanged?.Invoke(this, new PositionChangedEventArgs(Position));
                return samplesRead;
            }

            _sourceEnded = true;
            RaiseEndOfStreamOnce();
            return 0;
        }

        public void Seek(int sampleOffset)
        {
            if (IsDisposed)
                return;

            lock (_lock)
            {
                if (IsDisposed)
                    return;

                _inner.Seek(sampleOffset);
                Position = sampleOffset;
                ResetState();
                PositionChanged?.Invoke(this, new PositionChangedEventArgs(Position));
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            lock (_lock)
            {
                if (IsDisposed)
                    return;

                IsDisposed = true;
                _inner.Dispose();
                EndOfStreamReached = null;
                PositionChanged = null;
                _inputBuffer = Array.Empty<float>();
                _validSamples = 0;
                _readPosition = 0d;
            }
        }

        private void EnsureBufferedFrames(int requiredFrames)
        {
            while (!_sourceEnded && (_validSamples / _channels) < requiredFrames)
            {
                var availableFrames = _validSamples / _channels;
                var missingFrames = Math.Max(64, requiredFrames - availableFrames);
                var writeOffset = _validSamples;
                EnsureCapacity(writeOffset + (missingFrames * _channels));

                var samplesRead = _inner.ReadBytes(_inputBuffer.AsSpan(writeOffset, missingFrames * _channels));
                if (samplesRead <= 0)
                {
                    _sourceEnded = true;
                    break;
                }

                _validSamples += samplesRead;
            }
        }

        private void EnsureCapacity(int requiredSamples)
        {
            if (_inputBuffer.Length >= requiredSamples)
                return;

            var nextSize = _inputBuffer.Length == 0 ? requiredSamples : _inputBuffer.Length;
            while (nextSize < requiredSamples)
                nextSize *= 2;

            Array.Resize(ref _inputBuffer, nextSize);
        }

        private void ResetState()
        {
            _validSamples = 0;
            _readPosition = 0d;
            _sourceEnded = false;
            _endOfStreamRaised = false;
        }

        private void RaiseEndOfStreamOnce()
        {
            if (_endOfStreamRaised)
                return;

            _endOfStreamRaised = true;
            EndOfStreamReached?.Invoke(this, EventArgs.Empty);
        }
    }
}
