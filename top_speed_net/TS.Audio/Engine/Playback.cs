using System;

namespace TS.Audio
{
    public sealed partial class AudioEngine
    {
        public Source CreateSource(SoundAsset asset, string? busName = null, bool? spatialize = null, bool? useHrtf = null)
        {
            return ResolveBus(busName).CreateSource(asset, spatialize, useHrtf);
        }

        public Source CreateSource(SoundAsset asset, SourceOptions options, string? busName = null)
        {
            return ResolveBus(busName).CreateSource(asset, options);
        }

        public Source CreateSpatialSource(SoundAsset asset, string? busName = null, bool? allowHrtf = null)
        {
            return ResolveBus(busName).CreateSpatialSource(asset, allowHrtf);
        }

        public Source Play(SoundAsset asset, string? busName = null, bool? loop = null, bool? spatialize = null, bool? useHrtf = null)
        {
            return ResolveBus(busName).Play(asset, loop, spatialize, useHrtf);
        }

        public void PlayOneShot(SoundAsset asset, string? busName = null, Action<Source>? configure = null, bool? spatialize = null, bool? useHrtf = null)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            if (spatialize == true)
            {
                var transient = CreateSource(asset, busName, spatialize: true, useHrtf);
                TrackTransientSource(transient);
                configure?.Invoke(transient);
                transient.Play(loop: false);
                return;
            }

            Source? source = null;
            try
            {
                source = BorrowPooledOneShot(asset, busName, useHrtf);
                configure?.Invoke(source);
                source.Play(loop: false);
            }
            catch
            {
                if (source != null)
                    DisposeFailedPooledOneShot(source);
                throw;
            }
        }

        public void PlayOneShotSpatial(SoundAsset asset, string? busName = null, Action<Source>? configure = null, bool? allowHrtf = null)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            var source = CreateSpatialSource(asset, busName, allowHrtf);
            TrackTransientSource(source);
            configure?.Invoke(source);
            source.Play(loop: false);
        }

        public Source Play(SoundAsset asset, SourceOptions options, string? busName = null)
        {
            return ResolveBus(busName).Play(asset, options);
        }

        public Source CreateProceduralSource(ProceduralAudioCallback callback, uint channels = 1, uint sampleRate = 44100, string? busName = null, bool? spatialize = null, bool? useHrtf = null)
        {
            return ResolveBus(busName).CreateProceduralSource(callback, channels, sampleRate, spatialize, useHrtf);
        }

        public TrackStream CreateStream(string? busName, params StreamAsset[] assets)
        {
            return ResolveBus(busName).CreateStream(assets);
        }

        public TrackStream CreateStream(string? busName, params string[] filePaths)
        {
            return ResolveBus(busName).CreateStream(filePaths);
        }
    }
}
