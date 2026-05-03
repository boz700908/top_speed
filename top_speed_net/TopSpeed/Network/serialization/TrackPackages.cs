using System;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        public static bool TryReadTrackPackageUploadBegin(byte[] data, out PacketTrackPackageUploadBegin packet)
        {
            packet = new PacketTrackPackageUploadBegin();
            if (data.Length < 2 + 4 + 2 + 2 + 2 + 4)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageUploadBegin)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.UploadId = reader.ReadUInt32();
                packet.TrackId = reader.ReadString16();
                packet.Version = reader.ReadString16();
                packet.Hash = TrackPackageRef.NormalizeHash(reader.ReadString16());
                packet.TotalBytes = reader.ReadUInt32();
                return PacketValidation.IsValidTrackPackageUploadBegin(packet);
            }
            catch
            {
                packet = new PacketTrackPackageUploadBegin();
                return false;
            }
        }

        public static bool TryReadTrackPackageUploadChunk(byte[] data, out PacketTrackPackageUploadChunk packet)
        {
            packet = new PacketTrackPackageUploadChunk();
            if (data.Length < 2 + 4 + 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageUploadChunk)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.UploadId = reader.ReadUInt32();
                packet.ChunkIndex = reader.ReadUInt16();
                var length = reader.ReadUInt16();
                if (length == 0 || length > ProtocolConstants.MaxTrackPackageChunkBytes)
                    return false;
                if (data.Length != 2 + 4 + 2 + 2 + length)
                    return false;

                var bytes = new byte[length];
                for (var i = 0; i < length; i++)
                    bytes[i] = reader.ReadByte();
                packet.Data = bytes;
                return PacketValidation.IsValidTrackPackageUploadChunk(packet);
            }
            catch
            {
                packet = new PacketTrackPackageUploadChunk();
                return false;
            }
        }

        public static bool TryReadTrackPackageUploadEnd(byte[] data, out PacketTrackPackageUploadEnd packet)
        {
            packet = new PacketTrackPackageUploadEnd();
            if (data.Length < 2 + 4)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageUploadEnd)
                return false;

            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.UploadId = reader.ReadUInt32();
            return PacketValidation.IsValidTrackPackageUploadEnd(packet);
        }

        public static bool TryReadTrackPackageUploadResult(byte[] data, out PacketTrackPackageUploadResult packet)
        {
            packet = new PacketTrackPackageUploadResult();
            if (data.Length < 2 + 4 + 1 + 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageUploadResult)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.UploadId = reader.ReadUInt32();
                packet.Status = (TrackPackageUploadStatus)reader.ReadByte();
                packet.Hash = TrackPackageRef.NormalizeHash(reader.ReadString16());
                packet.Message = reader.ReadString16();
                return PacketValidation.IsValidTrackPackageUploadResult(packet);
            }
            catch
            {
                packet = new PacketTrackPackageUploadResult();
                return false;
            }
        }

        public static bool TryReadTrackPackageTransferBegin(byte[] data, out PacketTrackPackageTransferBegin packet)
        {
            packet = new PacketTrackPackageTransferBegin();
            if (data.Length < 2 + 2 + 2 + 2 + 4)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageTransferBegin)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.TrackId = reader.ReadString16();
                packet.Version = reader.ReadString16();
                packet.Hash = TrackPackageRef.NormalizeHash(reader.ReadString16());
                packet.TotalBytes = reader.ReadUInt32();
                return PacketValidation.IsValidTrackPackageTransferBegin(packet);
            }
            catch
            {
                packet = new PacketTrackPackageTransferBegin();
                return false;
            }
        }

        public static bool TryReadTrackPackageTransferChunk(byte[] data, out PacketTrackPackageTransferChunk packet)
        {
            packet = new PacketTrackPackageTransferChunk();
            if (data.Length < 2 + 2 + 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageTransferChunk)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                var rawHash = reader.ReadString16();
                packet.Hash = TrackPackageRef.NormalizeHash(rawHash);
                packet.ChunkIndex = reader.ReadUInt16();
                var length = reader.ReadUInt16();
                if (length == 0 || length > ProtocolConstants.MaxTrackPackageChunkBytes)
                    return false;
                if (data.Length != 2 + 2 + PacketWriter.MeasureString16(rawHash) + 2 + 2 + length)
                    return false;

                var bytes = new byte[length];
                for (var i = 0; i < length; i++)
                    bytes[i] = reader.ReadByte();
                packet.Data = bytes;
                return PacketValidation.IsValidTrackPackageTransferChunk(packet);
            }
            catch
            {
                packet = new PacketTrackPackageTransferChunk();
                return false;
            }
        }

        public static bool TryReadTrackPackageTransferEnd(byte[] data, out PacketTrackPackageTransferEnd packet)
        {
            packet = new PacketTrackPackageTransferEnd();
            if (data.Length < 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageTransferEnd)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.Hash = TrackPackageRef.NormalizeHash(reader.ReadString16());
                return PacketValidation.IsValidTrackPackageTransferEnd(packet);
            }
            catch
            {
                packet = new PacketTrackPackageTransferEnd();
                return false;
            }
        }

        public static bool TryReadTrackPackageReady(byte[] data, out PacketTrackPackageReady packet)
        {
            packet = new PacketTrackPackageReady();
            if (data.Length < 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageReady)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.Hash = TrackPackageRef.NormalizeHash(reader.ReadString16());
                return PacketValidation.IsValidTrackPackageReady(packet);
            }
            catch
            {
                packet = new PacketTrackPackageReady();
                return false;
            }
        }

        public static bool TryReadTrackPackageCatalogRequest(byte[] data, out PacketTrackPackageCatalogRequest packet)
        {
            packet = new PacketTrackPackageCatalogRequest();
            if (data.Length != 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageCatalogRequest)
                return false;
            return PacketValidation.IsValidTrackPackageCatalogRequest(packet);
        }

        public static bool TryReadTrackPackageCatalog(byte[] data, out PacketTrackPackageCatalog packet)
        {
            packet = new PacketTrackPackageCatalog();
            if (data.Length < 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageCatalog)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                var count = reader.ReadUInt16();
                if (count > ProtocolConstants.MaxTrackPackageCatalogEntries)
                    return false;

                var tracks = new PacketTrackPackageCatalogEntry[count];
                for (var i = 0; i < count; i++)
                {
                    var track = ReadCatalogTrackRef(ref reader);
                    var displayName = reader.ReadString16();
                    tracks[i] = new PacketTrackPackageCatalogEntry
                    {
                        Track = track,
                        DisplayName = displayName
                    };
                }

                packet.Tracks = tracks;
                return PacketValidation.IsValidTrackPackageCatalog(packet);
            }
            catch
            {
                packet = new PacketTrackPackageCatalog();
                return false;
            }
        }

        public static byte[] WriteTrackPackageUploadBegin(PacketTrackPackageUploadBegin packet)
        {
            var payload = 4
                + 2 + PacketWriter.MeasureString16(packet.TrackId)
                + 2 + PacketWriter.MeasureString16(packet.Version)
                + 2 + PacketWriter.MeasureString16(packet.Hash)
                + 4;
            var buffer = WritePacketHeader(Command.TrackPackageUploadBegin, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageUploadBegin);
            writer.WriteUInt32(packet.UploadId);
            writer.WriteString16(packet.TrackId ?? string.Empty);
            writer.WriteString16(packet.Version ?? string.Empty);
            writer.WriteString16(packet.Hash ?? string.Empty);
            writer.WriteUInt32(packet.TotalBytes);
            return buffer;
        }

        public static byte[] WriteTrackPackageUploadChunk(PacketTrackPackageUploadChunk packet)
        {
            var bytes = packet.Data ?? Array.Empty<byte>();
            if (bytes.Length == 0 || bytes.Length > ProtocolConstants.MaxTrackPackageChunkBytes)
                throw new ArgumentOutOfRangeException(nameof(packet), "Invalid track package chunk size.");

            var buffer = WritePacketHeader(Command.TrackPackageUploadChunk, 4 + 2 + 2 + bytes.Length);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageUploadChunk);
            writer.WriteUInt32(packet.UploadId);
            writer.WriteUInt16(packet.ChunkIndex);
            writer.WriteUInt16((ushort)bytes.Length);
            for (var i = 0; i < bytes.Length; i++)
                writer.WriteByte(bytes[i]);
            return buffer;
        }

        public static byte[] WriteTrackPackageUploadEnd(PacketTrackPackageUploadEnd packet)
        {
            var buffer = WritePacketHeader(Command.TrackPackageUploadEnd, 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageUploadEnd);
            writer.WriteUInt32(packet.UploadId);
            return buffer;
        }

        public static byte[] WriteTrackPackageUploadResult(PacketTrackPackageUploadResult packet)
        {
            var payload = 4 + 1
                + 2 + PacketWriter.MeasureString16(packet.Hash)
                + 2 + PacketWriter.MeasureString16(packet.Message);
            var buffer = WritePacketHeader(Command.TrackPackageUploadResult, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageUploadResult);
            writer.WriteUInt32(packet.UploadId);
            writer.WriteByte((byte)packet.Status);
            writer.WriteString16(packet.Hash ?? string.Empty);
            writer.WriteString16(packet.Message ?? string.Empty);
            return buffer;
        }

        public static byte[] WriteTrackPackageTransferBegin(PacketTrackPackageTransferBegin packet)
        {
            var payload = 2 + PacketWriter.MeasureString16(packet.TrackId)
                + 2 + PacketWriter.MeasureString16(packet.Version)
                + 2 + PacketWriter.MeasureString16(packet.Hash)
                + 4;
            var buffer = WritePacketHeader(Command.TrackPackageTransferBegin, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageTransferBegin);
            writer.WriteString16(packet.TrackId ?? string.Empty);
            writer.WriteString16(packet.Version ?? string.Empty);
            writer.WriteString16(packet.Hash ?? string.Empty);
            writer.WriteUInt32(packet.TotalBytes);
            return buffer;
        }

        public static byte[] WriteTrackPackageTransferChunk(PacketTrackPackageTransferChunk packet)
        {
            var bytes = packet.Data ?? Array.Empty<byte>();
            if (bytes.Length == 0 || bytes.Length > ProtocolConstants.MaxTrackPackageChunkBytes)
                throw new ArgumentOutOfRangeException(nameof(packet), "Invalid track package chunk size.");

            var payload = 2 + PacketWriter.MeasureString16(packet.Hash) + 2 + 2 + bytes.Length;
            var buffer = WritePacketHeader(Command.TrackPackageTransferChunk, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageTransferChunk);
            writer.WriteString16(packet.Hash ?? string.Empty);
            writer.WriteUInt16(packet.ChunkIndex);
            writer.WriteUInt16((ushort)bytes.Length);
            for (var i = 0; i < bytes.Length; i++)
                writer.WriteByte(bytes[i]);
            return buffer;
        }

        public static byte[] WriteTrackPackageTransferEnd(PacketTrackPackageTransferEnd packet)
        {
            var payload = 2 + PacketWriter.MeasureString16(packet.Hash);
            var buffer = WritePacketHeader(Command.TrackPackageTransferEnd, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageTransferEnd);
            writer.WriteString16(packet.Hash ?? string.Empty);
            return buffer;
        }

        public static byte[] WriteTrackPackageReady(PacketTrackPackageReady packet)
        {
            var payload = 2 + PacketWriter.MeasureString16(packet.Hash);
            var buffer = WritePacketHeader(Command.TrackPackageReady, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageReady);
            writer.WriteString16(packet.Hash ?? string.Empty);
            return buffer;
        }

        public static byte[] WriteTrackPackageCatalogRequest()
        {
            return WritePacketHeader(Command.TrackPackageCatalogRequest, 0);
        }

        public static byte[] WriteTrackPackageCatalog(PacketTrackPackageCatalog packet)
        {
            packet ??= new PacketTrackPackageCatalog();
            var tracks = packet.Tracks ?? Array.Empty<PacketTrackPackageCatalogEntry>();
            var count = Math.Min(tracks.Length, ProtocolConstants.MaxTrackPackageCatalogEntries);

            var payload = 2;
            for (var i = 0; i < count; i++)
            {
                var entry = tracks[i] ?? new PacketTrackPackageCatalogEntry();
                payload += MeasureCatalogTrackRef(entry.Track);
                payload += 2 + PacketWriter.MeasureString16(entry.DisplayName ?? string.Empty);
            }

            var buffer = WritePacketHeader(Command.TrackPackageCatalog, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageCatalog);
            writer.WriteUInt16((ushort)count);
            for (var i = 0; i < count; i++)
            {
                var entry = tracks[i] ?? new PacketTrackPackageCatalogEntry();
                WriteCatalogTrackRef(ref writer, entry.Track);
                writer.WriteString16(entry.DisplayName ?? string.Empty);
            }

            return buffer;
        }

        private static TrackPackageRef ReadCatalogTrackRef(ref PacketReader reader)
        {
            var kind = (RoomTrackSelectionKind)reader.ReadByte();
            var builtInTrack = reader.ReadString16();
            var trackId = reader.ReadString16();
            var version = reader.ReadString16();
            var hash = reader.ReadString16();

            return new TrackPackageRef
            {
                Kind = kind,
                BuiltInTrackKey = builtInTrack ?? string.Empty,
                TrackId = trackId ?? string.Empty,
                Version = version ?? string.Empty,
                Hash = TrackPackageRef.NormalizeHash(hash)
            };
        }

        private static int MeasureCatalogTrackRef(TrackPackageRef track)
        {
            var normalized = NormalizeCatalogTrackRef(track);
            return 1
                + 2 + PacketWriter.MeasureString16(normalized.BuiltInTrackKey)
                + 2 + PacketWriter.MeasureString16(normalized.TrackId)
                + 2 + PacketWriter.MeasureString16(normalized.Version)
                + 2 + PacketWriter.MeasureString16(normalized.Hash);
        }

        private static void WriteCatalogTrackRef(ref PacketWriter writer, TrackPackageRef track)
        {
            var normalized = NormalizeCatalogTrackRef(track);
            writer.WriteByte((byte)normalized.Kind);
            writer.WriteString16(normalized.BuiltInTrackKey ?? string.Empty);
            writer.WriteString16(normalized.TrackId ?? string.Empty);
            writer.WriteString16(normalized.Version ?? string.Empty);
            writer.WriteString16(normalized.Hash ?? string.Empty);
        }

        private static TrackPackageRef NormalizeCatalogTrackRef(TrackPackageRef track)
        {
            if (track == null)
                return TrackPackageRef.BuiltIn(string.Empty);

            if (track.IsCustomPackage)
            {
                return TrackPackageRef.Custom(
                    track.TrackId ?? string.Empty,
                    track.Version ?? string.Empty,
                    track.Hash ?? string.Empty);
            }

            return TrackPackageRef.BuiltIn(track.BuiltInTrackKey ?? string.Empty);
        }
    }
}
