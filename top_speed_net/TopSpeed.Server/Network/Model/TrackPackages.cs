using System;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed class TrackPackageRecord
    {
        public TrackPackageRef Ref { get; set; } = new TrackPackageRef();
        public TrackPackagePayload Payload { get; set; } = new TrackPackagePayload();
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
        public TrackData TrackData { get; set; } = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, Array.Empty<TrackDefinition>());
        public DateTime LastAccessUtc { get; set; } = DateTime.UtcNow;
        public bool FromServerTracksFolder { get; set; }
        public string SourcePath { get; set; } = string.Empty;
        public DateTime SourceLastWriteUtc { get; set; } = DateTime.MinValue;
    }

    internal sealed class TrackPackageUploadSession
    {
        public uint UploadId { get; set; }
        public uint OwnerPlayerId { get; set; }
        public uint RoomId { get; set; }
        public string TrackId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public uint TotalBytes { get; set; }
        public ushort NextChunkIndex { get; set; }
        public int Offset { get; set; }
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
    }
}
