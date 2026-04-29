using System;
using System.Collections.Generic;
using System.IO;
using SoundFlow.Interfaces;
using SoundFlow.Metadata;
using SoundFlow.Metadata.Models;
using SoundFlow.Providers;
using SfAudioEngine = SoundFlow.Abstracts.AudioEngine;
using SfAudioFormat = SoundFlow.Structs.AudioFormat;
using SfResult = SoundFlow.Structs.Result<SoundFlow.Metadata.Models.SoundFormatInfo>;

namespace TS.Audio
{
    internal abstract class AudioAsset : IDisposable
    {
        public abstract int InputChannels { get; }
        public abstract int InputSampleRate { get; }
        public abstract float LengthSeconds { get; }
        public abstract string DebugName { get; }
        internal abstract ISoundDataProvider CreateProvider(SfAudioEngine engine, SfAudioFormat outputFormat);

        public virtual void Dispose()
        {
        }
    }

    internal sealed class FileAsset : AudioAsset
    {
        private readonly string _path;
        private readonly bool _streamFromDisk;
        private readonly int _inputChannels;
        private readonly int _inputSampleRate;
        private readonly float _lengthSeconds;
        private readonly object _decodeLock;
        private Dictionary<string, DecodedPcm>? _decodedByFormat;

        public FileAsset(string path, bool streamFromDisk)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("File path is required.", nameof(path));

            _path = path;
            _streamFromDisk = streamFromDisk;
            _decodeLock = new object();
            ReadMetadata(path, out _inputChannels, out _inputSampleRate, out _lengthSeconds);
        }

        public override int InputChannels => _inputChannels;
        public override int InputSampleRate => _inputSampleRate;
        public override float LengthSeconds => _lengthSeconds;
        public override string DebugName => _path;

        internal override ISoundDataProvider CreateProvider(SfAudioEngine engine, SfAudioFormat outputFormat)
        {
            if (_streamFromDisk)
            {
                var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
                return new StreamDataProvider(engine, outputFormat, stream);
            }

            return GetOrCreateDecoded(engine, outputFormat).CreateProvider();
        }

        private DecodedPcm GetOrCreateDecoded(SfAudioEngine engine, SfAudioFormat outputFormat)
        {
            var key = BuildFormatKey(outputFormat);
            lock (_decodeLock)
            {
                _decodedByFormat ??= new Dictionary<string, DecodedPcm>(StringComparer.Ordinal);
                if (_decodedByFormat.TryGetValue(key, out var decoded))
                    return decoded;

                decoded = Decode(engine, outputFormat);
                _decodedByFormat[key] = decoded;
                return decoded;
            }
        }

        private DecodedPcm Decode(SfAudioEngine engine, SfAudioFormat outputFormat)
        {
            using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var provider = new AssetDataProvider(engine, outputFormat, stream);
            return DecodedPcm.FromProvider(provider, outputFormat);
        }

        private static string BuildFormatKey(SfAudioFormat outputFormat)
        {
            return ((int)outputFormat.Format).ToString() + "|" + outputFormat.Channels + "|" + outputFormat.SampleRate;
        }

        private static void ReadMetadata(string path, out int channels, out int sampleRate, out float lengthSeconds)
        {
            channels = 2;
            sampleRate = 44100;
            lengthSeconds = 0f;

            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                ApplyFormatInfo(SoundMetadataReader.Read(stream, new ReadOptions()), ref channels, ref sampleRate, ref lengthSeconds);
            }
            catch
            {
            }
        }

        internal static void ApplyFormatInfo(SfResult result, ref int channels, ref int sampleRate, ref float lengthSeconds)
        {
            if (result is not { IsSuccess: true, Value: not null })
                return;

            if (result.Value.ChannelCount > 0)
                channels = result.Value.ChannelCount;
            if (result.Value.SampleRate > 0)
                sampleRate = result.Value.SampleRate;
            if (result.Value.Duration > TimeSpan.Zero)
                lengthSeconds = (float)result.Value.Duration.TotalSeconds;
        }
    }

    internal sealed class MemoryAsset : AudioAsset
    {
        private readonly byte[] _data;
        private readonly int _inputChannels;
        private readonly int _inputSampleRate;
        private readonly float _lengthSeconds;

        public MemoryAsset(byte[] data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            if (_data.Length == 0)
                throw new ArgumentException("Audio data is required.", nameof(data));

            var channels = 2;
            var sampleRate = 44100;
            var lengthSeconds = 0f;

            try
            {
                using var stream = new MemoryStream(_data, writable: false);
                FileAsset.ApplyFormatInfo(SoundMetadataReader.Read(stream, new ReadOptions()), ref channels, ref sampleRate, ref lengthSeconds);
            }
            catch
            {
            }

            _inputChannels = channels;
            _inputSampleRate = sampleRate;
            _lengthSeconds = lengthSeconds;
        }

        public override int InputChannels => _inputChannels;
        public override int InputSampleRate => _inputSampleRate;
        public override float LengthSeconds => _lengthSeconds;
        public override string DebugName => "memory";

        internal override ISoundDataProvider CreateProvider(SfAudioEngine engine, SfAudioFormat outputFormat)
        {
            return new AssetDataProvider(engine, outputFormat, new MemoryStream(_data, writable: false));
        }
    }

    internal sealed class ProceduralAsset : AudioAsset
    {
        private readonly ProceduralAudioCallback _callback;
        private readonly int _channels;
        private readonly int _sampleRate;

        public ProceduralAsset(ProceduralAudioCallback callback, uint channels, uint sampleRate)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _channels = channels > 0 ? (int)channels : 1;
            _sampleRate = sampleRate > 0 ? (int)sampleRate : 44100;
        }

        public override int InputChannels => _channels;
        public override int InputSampleRate => _sampleRate;
        public override float LengthSeconds => 0f;
        public override string DebugName => "procedural";

        internal override ISoundDataProvider CreateProvider(SfAudioEngine engine, SfAudioFormat outputFormat)
        {
            return new ProceduralDataProvider(_callback, _channels, _sampleRate, outputFormat.Channels, outputFormat.SampleRate);
        }
    }
}
