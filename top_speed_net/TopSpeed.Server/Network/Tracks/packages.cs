using System;
using System.Collections.Generic;
using System.Linq;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private bool TryGetTrackPackage(string hash, out TrackPackageRecord package)
        {
            package = null!;
            var key = TrackPackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(key))
                return false;
            if (!_trackPackageCache.TryGetValue(key, out var found) || found == null)
                return false;

            package = found;
            package.LastAccessUtc = DateTime.UtcNow;
            return true;
        }

        private bool StoreTrackPackage(
            TrackPackagePayload payload,
            byte[] bytes,
            bool fromServerTracksFolder = false,
            string sourcePath = "",
            DateTime? sourceLastWriteUtc = null)
        {
            if (payload == null || bytes == null)
                return false;

            var hash = TrackPackageRef.NormalizeHash(payload.Manifest.Hash);
            if (string.IsNullOrWhiteSpace(hash))
                return false;

            var trackData = TrackPackageCodec.ToTrackData(payload, userDefined: true, sourcePath: string.Empty);
            _trackPackageCache[hash] = new TrackPackageRecord
            {
                Ref = TrackPackageRef.Custom(payload.Manifest.TrackId, payload.Manifest.Version, hash),
                Payload = payload,
                Bytes = bytes,
                TrackData = trackData,
                LastAccessUtc = DateTime.UtcNow,
                FromServerTracksFolder = fromServerTracksFolder,
                SourcePath = sourcePath ?? string.Empty,
                SourceLastWriteUtc = sourceLastWriteUtc ?? DateTime.MinValue
            };

            EvictTrackPackages();
            return true;
        }

        private void EvictTrackPackages()
        {
            if (_trackPackageCache.Count <= ProtocolConstants.MaxTrackPackageCacheEntries)
                return;

            var protectedHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var room in _rooms.Values)
            {
                if (room.TrackSelection != null
                    && room.TrackSelection.IsCustomPackage
                    && !string.IsNullOrWhiteSpace(room.TrackSelection.Hash))
                {
                    protectedHashes.Add(TrackPackageRef.NormalizeHash(room.TrackSelection.Hash));
                }
            }

            var candidates = _trackPackageCache
                .Where(pair => !protectedHashes.Contains(pair.Key))
                .OrderBy(pair => pair.Value.LastAccessUtc)
                .Select(pair => pair.Key)
                .ToList();

            for (var i = 0; i < candidates.Count && _trackPackageCache.Count > ProtocolConstants.MaxTrackPackageCacheEntries; i++)
                _trackPackageCache.Remove(candidates[i]);
        }

        private void SendTrackPackageUploadResult(PlayerConnection player, uint uploadId, TrackPackageUploadStatus status, string hash, string message)
        {
            SendStream(player, PacketSerializer.WriteTrackPackageUploadResult(new PacketTrackPackageUploadResult
            {
                UploadId = uploadId,
                Status = status,
                Hash = TrackPackageRef.NormalizeHash(hash),
                Message = message ?? string.Empty
            }), PacketStream.Room);
        }

        private PacketTrackPackageCatalog BuildTrackPackageCatalog()
        {
            RefreshServerTrackPackages();
            var entries = _trackPackageCache.Values
                .Where(record => record != null && record.Ref != null && record.Ref.IsCustomPackage)
                .OrderBy(record => record.Ref.TrackId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(record => record.Ref.Version, StringComparer.OrdinalIgnoreCase)
                .Select(record => new PacketTrackPackageCatalogEntry
                {
                    Track = TrackPackageRef.Custom(record.Ref.TrackId, record.Ref.Version, record.Ref.Hash),
                    DisplayName = ResolveTrackPackageDisplayName(record)
                })
                .Take(ProtocolConstants.MaxTrackPackageCatalogEntries)
                .ToArray();

            return new PacketTrackPackageCatalog
            {
                Tracks = entries
            };
        }

        private void SendTrackPackageCatalog(PlayerConnection player, PacketTrackPackageCatalog packet)
        {
            if (player == null)
                return;

            SendStream(player, PacketSerializer.WriteTrackPackageCatalog(packet ?? new PacketTrackPackageCatalog()), PacketStream.Room);
        }

        private static string ResolveTrackPackageDisplayName(TrackPackageRecord record)
        {
            var metadata = record.Payload?.Metadata;
            if (metadata != null && metadata.TryGetValue("name", out var name))
            {
                var trimmed = (name ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    return ClampTrackPackageDisplayName(trimmed);
            }

            var trackId = (record.Ref?.TrackId ?? string.Empty).Trim();
            var version = (record.Ref?.Version ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(trackId) && !string.IsNullOrWhiteSpace(version))
                return ClampTrackPackageDisplayName(trackId + " (" + version + ")");
            if (!string.IsNullOrWhiteSpace(trackId))
                return ClampTrackPackageDisplayName(trackId);

            return ClampTrackPackageDisplayName(LocalizationService.Mark("Custom track"));
        }

        private static string ClampTrackPackageDisplayName(string value)
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length <= ProtocolConstants.MaxTrackPackageDisplayNameLength)
                return trimmed;
            return trimmed.Substring(0, ProtocolConstants.MaxTrackPackageDisplayNameLength);
        }

        private void SendTrackPackageToPlayer(PlayerConnection player, TrackPackageRecord package)
        {
            if (player == null || package == null || package.Bytes == null || package.Bytes.Length == 0)
                return;

            SendStream(player, PacketSerializer.WriteTrackPackageTransferBegin(new PacketTrackPackageTransferBegin
            {
                TrackId = package.Ref.TrackId,
                Version = package.Ref.Version,
                Hash = package.Ref.Hash,
                TotalBytes = (uint)package.Bytes.Length
            }), PacketStream.Room);

            var chunkSize = ProtocolConstants.MaxTrackPackageChunkBytes;
            var chunkIndex = 0;
            var offset = 0;
            while (offset < package.Bytes.Length)
            {
                var length = Math.Min(chunkSize, package.Bytes.Length - offset);
                var chunk = new byte[length];
                Buffer.BlockCopy(package.Bytes, offset, chunk, 0, length);
                SendStream(player, PacketSerializer.WriteTrackPackageTransferChunk(new PacketTrackPackageTransferChunk
                {
                    Hash = package.Ref.Hash,
                    ChunkIndex = (ushort)chunkIndex,
                    Data = chunk
                }), PacketStream.Room);
                offset += length;
                chunkIndex++;
            }

            SendStream(player, PacketSerializer.WriteTrackPackageTransferEnd(new PacketTrackPackageTransferEnd
            {
                Hash = package.Ref.Hash
            }), PacketStream.Room);
        }

        private bool EnsureRoomTrackPackageReady(RaceRoom room, IEnumerable<uint> participantIds)
        {
            if (room == null || room.TrackSelection == null || !room.TrackSelection.IsCustomPackage)
                return true;

            if (!TryGetTrackPackage(room.TrackSelection.Hash, out var package))
                return false;

            var allReady = true;
            foreach (var id in participantIds)
            {
                if (room.TrackReadyPlayers.Contains(id))
                    continue;

                allReady = false;
                if (_players.TryGetValue(id, out var player))
                    SendTrackPackageToPlayer(player, package);
            }

            return allReady;
        }

        private void ResetRoomTrackReadiness(RaceRoom room)
        {
            room.TrackReadyPlayers.Clear();
        }

        private void MarkPlayerTrackReady(RaceRoom room, uint playerId)
        {
            room.TrackReadyPlayers.Add(playerId);
        }
    }
}
