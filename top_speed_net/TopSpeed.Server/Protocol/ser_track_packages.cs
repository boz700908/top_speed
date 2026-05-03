using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Protocol
{
    internal static partial class PacketSerializer
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

        public static byte[] WriteTrackPackageUploadResult(PacketTrackPackageUploadResult packet)
        {
            var payload = 4 + 1 + 2 + PacketWriter.MeasureString16(packet.Hash) + 2 + PacketWriter.MeasureString16(packet.Message);
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
