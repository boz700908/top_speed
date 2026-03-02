using System;
using System.Collections.Generic;
using System.Net;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed class MediaBlob
    {
        public uint MediaId { get; set; }
        public string Extension { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    internal sealed class InMedia
    {
        public uint MediaId { get; set; }
        public string Extension { get; set; } = string.Empty;
        public uint TotalBytes { get; set; }
        public ushort NextChunk { get; set; }
        public bool BufferEnabled { get; set; }
        public byte[] Buffer { get; set; } = Array.Empty<byte>();
        public int Offset { get; set; }

        public bool IsComplete => TotalBytes > 0 && Offset >= (int)TotalBytes;
    }

}
