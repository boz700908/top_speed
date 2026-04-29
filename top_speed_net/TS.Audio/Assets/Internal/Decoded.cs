using System;
using System.Collections.Generic;
using SoundFlow.Interfaces;
using SoundFlow.Metadata.Models;
using SfAudioFormat = SoundFlow.Structs.AudioFormat;

namespace TS.Audio
{
    internal sealed class DecodedPcm
    {
        private readonly float[] _samples;
        private readonly int _channels;
        private readonly int _sampleRate;
        private readonly SoundFormatInfo? _formatInfo;

        private DecodedPcm(float[] samples, int channels, int sampleRate, SoundFormatInfo? formatInfo)
        {
            _samples = samples;
            _channels = channels;
            _sampleRate = sampleRate;
            _formatInfo = formatInfo;
        }

        public static DecodedPcm FromProvider(ISoundDataProvider provider, SfAudioFormat outputFormat)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var length = provider.Length;
            var samples = length > 0
                ? DecodeKnownLength(provider, length)
                : DecodeUnknownLength(provider, Math.Max(1, outputFormat.Channels));

            return new DecodedPcm(samples, outputFormat.Channels, outputFormat.SampleRate, provider.FormatInfo);
        }

        public ISoundDataProvider CreateProvider()
        {
            return new PcmDataProvider(_samples, _channels, _sampleRate, _formatInfo);
        }

        private static float[] DecodeKnownLength(ISoundDataProvider provider, int length)
        {
            var samples = new float[length];
            var totalRead = 0;
            while (totalRead < samples.Length)
            {
                var read = provider.ReadBytes(samples.AsSpan(totalRead));
                if (read <= 0)
                    break;
                totalRead += read;
            }

            if (totalRead < samples.Length)
                Array.Resize(ref samples, totalRead);
            return samples;
        }

        private static float[] DecodeUnknownLength(ISoundDataProvider provider, int channels)
        {
            var blockSize = Math.Max(4096, 4096 * channels);
            var blocks = new List<float[]>();
            var totalSamples = 0;

            while (true)
            {
                var block = new float[blockSize];
                var read = provider.ReadBytes(block);
                if (read <= 0)
                    break;

                if (read < block.Length)
                    Array.Resize(ref block, read);
                blocks.Add(block);
                totalSamples += read;
            }

            var samples = new float[totalSamples];
            var offset = 0;
            for (var i = 0; i < blocks.Count; i++)
            {
                blocks[i].CopyTo(samples, offset);
                offset += blocks[i].Length;
            }

            return samples;
        }
    }
}
