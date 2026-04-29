using System;
using SoundFlow.Enums;
using SoundFlow.Interfaces;
using SoundFlow.Metadata.Models;

namespace TS.Audio
{
    internal sealed class PcmDataProvider : ISoundDataProvider
    {
        private readonly float[] _samples;
        private int _position;

        public PcmDataProvider(float[] samples, int channels, int sampleRate, SoundFormatInfo? formatInfo)
        {
            _samples = samples ?? throw new ArgumentNullException(nameof(samples));
            Channels = channels > 0 ? channels : throw new ArgumentOutOfRangeException(nameof(channels));
            SampleRate = sampleRate > 0 ? sampleRate : throw new ArgumentOutOfRangeException(nameof(sampleRate));
            FormatInfo = formatInfo;
        }

        public int Channels { get; }
        public int Position => _position;
        public int Length => _samples.Length;
        public bool CanSeek => true;
        public SampleFormat SampleFormat => SampleFormat.F32;
        public int SampleRate { get; }
        public bool IsDisposed { get; private set; }
        public SoundFormatInfo? FormatInfo { get; }

        public event EventHandler<EventArgs>? EndOfStreamReached;
        public event EventHandler<PositionChangedEventArgs>? PositionChanged;

        public int ReadBytes(Span<float> buffer)
        {
            if (IsDisposed || buffer.IsEmpty)
                return 0;

            var samplesToRead = Math.Min(buffer.Length, _samples.Length - _position);
            if (samplesToRead <= 0)
            {
                EndOfStreamReached?.Invoke(this, EventArgs.Empty);
                return 0;
            }

            _samples.AsSpan(_position, samplesToRead).CopyTo(buffer);
            _position += samplesToRead;
            PositionChanged?.Invoke(this, new PositionChangedEventArgs(_position));
            return samplesToRead;
        }

        public void Seek(int sampleOffset)
        {
            if (IsDisposed)
                return;

            var alignedOffset = (sampleOffset / Channels) * Channels;
            _position = Math.Clamp(alignedOffset, 0, _samples.Length);
            PositionChanged?.Invoke(this, new PositionChangedEventArgs(_position));
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            EndOfStreamReached = null;
            PositionChanged = null;
        }
    }
}
