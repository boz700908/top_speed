using System;
using SteamAudio;

namespace TS.Audio
{
    internal sealed unsafe partial class SteamAudioSpatialModifier
    {
        private void WriteDownmixedMono(Span<float> source, int channels, int frames, IPL.AudioBuffer buffer)
        {
            if (buffer.Data == IntPtr.Zero)
                return;

            var target = GetChannelPointer(buffer, 0);
            for (var frame = 0; frame < frames; frame++)
                target[frame] = DownmixFrame(source, frame * channels, channels);
        }

        private static void WriteInterleavedToBuffer(Span<float> source, int channels, int frames, IPL.AudioBuffer buffer)
        {
            for (var channel = 0; channel < channels; channel++)
            {
                var target = GetChannelPointer(buffer, channel);
                var cursor = channel;
                for (var frame = 0; frame < frames; frame++, cursor += channels)
                    target[frame] = source[cursor];
            }
        }

        private static void ReadBufferToInterleaved(IPL.AudioBuffer buffer, Span<float> destination, int channels, int frames)
        {
            for (var channel = 0; channel < channels; channel++)
            {
                var source = GetChannelPointer(buffer, channel);
                var cursor = channel;
                for (var frame = 0; frame < frames; frame++, cursor += channels)
                    destination[cursor] = source[frame];
            }
        }

        private static void CopyBuffer(IPL.AudioBuffer source, IPL.AudioBuffer destination, int frames)
        {
            if (source.Data == IntPtr.Zero || destination.Data == IntPtr.Zero)
                return;

            var sourceChannel = GetChannelPointer(source, 0);
            var destinationChannel = GetChannelPointer(destination, 0);
            for (var frame = 0; frame < frames; frame++)
                destinationChannel[frame] = sourceChannel[frame];
        }

        private static float* GetChannelPointer(IPL.AudioBuffer buffer, int channel)
        {
            return ((float**)buffer.Data)[channel];
        }

        private static void GetStereoWideningGains(IPL.Vector3 direction, out float leftGain, out float rightGain)
        {
            const float fullCutoffAtDirectionX = 0.9f;
            var normalizedX = Clamp(direction.X / fullCutoffAtDirectionX, -1f, 1f);
            leftGain = normalizedX > 0f ? 1f - normalizedX : 1f;
            rightGain = normalizedX < 0f ? 1f + normalizedX : 1f;
        }
    }
}
