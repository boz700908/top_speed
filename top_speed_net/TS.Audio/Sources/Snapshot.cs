using System.Collections.Generic;

namespace TS.Audio
{
    public sealed class AudioSourceSnapshot
    {
        public int SourceId { get; }
        public string BusName { get; }
        public bool IsPlaying { get; }
        public bool IsSpatialized { get; }
        public bool UsesSteamAudio { get; }
        public string? Name { get; }
        public int InputChannels { get; }
        public int InputSampleRate { get; }
        public int OutputSampleRate { get; }
        public int ProviderSampleRate { get; }
        public int ProviderPositionSamples { get; }
        public int InnerPositionSamples { get; }
        public int BufferedFrames { get; }
        public float PlaybackTimeSeconds { get; }
        public bool Looping { get; }
        public float Volume { get; }
        public float VolumeDb { get; }
        public float SpatialGain { get; }
        public float DistanceAttenuation { get; }
        public float Pitch { get; }
        public float Pan { get; }
        public float BusEffectiveVolume { get; }
        public float BusEffectiveVolumeDb { get; }
        public float EstimatedMixVolume { get; }
        public float EstimatedMixVolumeDb { get; }
        public IReadOnlyList<AudioGainStageSnapshot> BusGainStages { get; }
        public float LengthSeconds { get; }

        public AudioSourceSnapshot(int sourceId, string busName, bool isPlaying, bool isSpatialized, bool usesSteamAudio, int inputChannels, int inputSampleRate, bool looping, float volume, float volumeDb, float spatialGain, float distanceAttenuation, float pitch, float pan, float busEffectiveVolume, float busEffectiveVolumeDb, float estimatedMixVolume, float estimatedMixVolumeDb, IReadOnlyList<AudioGainStageSnapshot> busGainStages, float lengthSeconds, string? name = null, int outputSampleRate = 0, int providerSampleRate = 0, int providerPositionSamples = 0, int innerPositionSamples = 0, int bufferedFrames = 0, float playbackTimeSeconds = 0f)
        {
            SourceId = sourceId;
            BusName = busName;
            IsPlaying = isPlaying;
            IsSpatialized = isSpatialized;
            UsesSteamAudio = usesSteamAudio;
            Name = name;
            InputChannels = inputChannels;
            InputSampleRate = inputSampleRate;
            OutputSampleRate = outputSampleRate;
            ProviderSampleRate = providerSampleRate;
            ProviderPositionSamples = providerPositionSamples;
            InnerPositionSamples = innerPositionSamples;
            BufferedFrames = bufferedFrames;
            PlaybackTimeSeconds = playbackTimeSeconds;
            Looping = looping;
            Volume = volume;
            VolumeDb = volumeDb;
            SpatialGain = spatialGain;
            DistanceAttenuation = distanceAttenuation;
            Pitch = pitch;
            Pan = pan;
            BusEffectiveVolume = busEffectiveVolume;
            BusEffectiveVolumeDb = busEffectiveVolumeDb;
            EstimatedMixVolume = estimatedMixVolume;
            EstimatedMixVolumeDb = estimatedMixVolumeDb;
            BusGainStages = busGainStages;
            LengthSeconds = lengthSeconds;
        }
    }
}
