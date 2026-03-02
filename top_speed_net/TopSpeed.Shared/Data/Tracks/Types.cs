using System;
using System.Collections.Generic;

namespace TopSpeed.Data
{
    public enum TrackType
    {
        Straight = 0,
        EasyLeft = 1,
        Left = 2,
        HardLeft = 3,
        HairpinLeft = 4,
        EasyRight = 5,
        Right = 6,
        HardRight = 7,
        HairpinRight = 8
    }

    public enum TrackSurface
    {
        Asphalt = 0,
        Gravel = 1,
        Water = 2,
        Sand = 3,
        Snow = 4
    }

    public enum TrackNoise
    {
        NoNoise = 0,
        Crowd = 1,
        Ocean = 2,
        Runway = 3,
        Clock = 4,
        Jet = 5,
        Thunder = 6,
        Pile = 7,
        Construction = 8,
        River = 9,
        Helicopter = 10,
        Owl = 11
    }

    public enum TrackWeather
    {
        Sunny = 0,
        Rain = 1,
        Wind = 2,
        Storm = 3
    }

    public enum TrackAmbience
    {
        NoAmbience = 0,
        Desert = 1,
        Airport = 2
    }

    public readonly struct TrackDefinition
    {
        private static readonly IReadOnlyList<string> EmptySoundSources = Array.Empty<string>();
        private static readonly IReadOnlyDictionary<string, string> EmptyMetadata = new Dictionary<string, string>();

        public TrackType Type { get; }
        public TrackSurface Surface { get; }
        public TrackNoise Noise { get; }
        public float Length { get; }
        public string? SegmentId { get; }
        public float Width { get; }
        public float Height { get; }
        public string? RoomId { get; }
        public TrackRoomOverrides? RoomOverrides { get; }
        public IReadOnlyList<string> SoundSourceIds { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }

        public TrackDefinition(TrackType type, TrackSurface surface, TrackNoise noise, float length)
            : this(type, surface, noise, length, null, 0f, 0f, null, null, null, null)
        {
        }

        public TrackDefinition(
            TrackType type,
            TrackSurface surface,
            TrackNoise noise,
            float length,
            string? segmentId,
            float width,
            float height,
            string? roomId,
            TrackRoomOverrides? roomOverrides,
            IReadOnlyList<string>? soundSourceIds,
            IReadOnlyDictionary<string, string>? metadata)
        {
            Type = type;
            Surface = surface;
            Noise = noise;
            Length = length;
            SegmentId = segmentId;
            Width = width;
            Height = height;
            RoomId = roomId;
            RoomOverrides = roomOverrides;
            SoundSourceIds = soundSourceIds ?? EmptySoundSources;
            Metadata = metadata ?? EmptyMetadata;
        }
    }

    public sealed class TrackData
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyMetadata = new Dictionary<string, string>();
        private static readonly IReadOnlyDictionary<string, TrackRoomDefinition> EmptyRooms = new Dictionary<string, TrackRoomDefinition>();
        private static readonly IReadOnlyDictionary<string, TrackSoundSourceDefinition> EmptySounds = new Dictionary<string, TrackSoundSourceDefinition>();

        public bool UserDefined { get; }
        public string? Name { get; }
        public string? Version { get; }
        public TrackWeather Weather { get; }
        public TrackAmbience Ambience { get; }
        public TrackDefinition[] Definitions { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }
        public IReadOnlyDictionary<string, TrackRoomDefinition> RoomProfiles { get; }
        public IReadOnlyDictionary<string, TrackSoundSourceDefinition> SoundSources { get; }
        public string? SourcePath { get; }
        public int Length => Definitions.Length;
        public byte Laps { get; set; }

        public TrackData(
            bool userDefined,
            TrackWeather weather,
            TrackAmbience ambience,
            TrackDefinition[] definitions,
            byte laps = 0,
            string? name = null,
            string? version = null,
            IReadOnlyDictionary<string, string>? metadata = null,
            IReadOnlyDictionary<string, TrackRoomDefinition>? roomProfiles = null,
            IReadOnlyDictionary<string, TrackSoundSourceDefinition>? soundSources = null,
            string? sourcePath = null)
        {
            UserDefined = userDefined;
            Weather = weather;
            Ambience = ambience;
            Definitions = definitions;
            Laps = laps;
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            var trimmedVersion = version?.Trim();
            Version = string.IsNullOrWhiteSpace(trimmedVersion) ? null : trimmedVersion;
            Metadata = metadata ?? EmptyMetadata;
            RoomProfiles = roomProfiles ?? EmptyRooms;
            SoundSources = soundSources ?? EmptySounds;
            var trimmedSourcePath = sourcePath?.Trim();
            SourcePath = string.IsNullOrWhiteSpace(trimmedSourcePath) ? null : trimmedSourcePath;
        }
    }
}
