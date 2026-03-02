using System;
using System.Linq;
using LiteNetLib;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;
using TopSpeed.Server.Tracks;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void HandleRoomStateRequest(PlayerConnection player)
        {
            if (!player.RoomId.HasValue)
            {
                SendRoomState(player, null);
                return;
            }

            if (_rooms.TryGetValue(player.RoomId.Value, out var room))
                SendRoomState(player, room);
            else
                SendRoomState(player, null);
        }

        private void HandleRoomGetRequest(PlayerConnection player, PacketRoomGetRequest packet)
        {
            if (!_rooms.TryGetValue(packet.RoomId, out var room))
            {
                SendRoomGet(player, null);
                return;
            }

            SendRoomGet(player, room);
        }

        private void SendRoomList(PlayerConnection player)
        {
            var list = new PacketRoomList
            {
                Rooms = _rooms.Values
                    .OrderBy(r => r.Id)
                    .Take(ProtocolConstants.MaxRoomListEntries)
                    .Select(BuildRoomSummary)
                    .ToArray()
            };

            SendStream(player, PacketSerializer.WriteRoomList(list), PacketStream.Query);
        }

        private void SendRoomState(PlayerConnection player, RaceRoom? room)
        {
            if (room == null)
            {
                SendStream(player, PacketSerializer.WriteRoomState(new PacketRoomState
                {
                    RoomVersion = 0,
                    InRoom = false,
                    HostPlayerId = 0,
                    RoomType = GameRoomType.BotsRace,
                    PlayersToStart = 0,
                    PreparingRace = false,
                    Players = Array.Empty<PacketRoomPlayer>()
                }), PacketStream.Query);
                return;
            }

            SendStream(player, PacketSerializer.WriteRoomState(new PacketRoomState
            {
                RoomVersion = room.Version,
                RoomId = room.Id,
                HostPlayerId = room.HostId,
                RoomName = room.Name,
                RoomType = room.RoomType,
                PlayersToStart = room.PlayersToStart,
                InRoom = true,
                IsHost = room.HostId == player.Id,
                RaceStarted = room.RaceStarted,
                PreparingRace = room.PreparingRace,
                TrackName = room.TrackName,
                Laps = room.Laps,
                Players = BuildRoomPlayers(room)
            }), PacketStream.Query);
        }

        private void SendRoomGet(PlayerConnection player, RaceRoom? room)
        {
            if (room == null)
            {
                SendStream(player, PacketSerializer.WriteRoomGet(new PacketRoomGet
                {
                    Found = false,
                    Players = Array.Empty<PacketRoomPlayer>()
                }), PacketStream.Query);
                return;
            }

            SendStream(player, PacketSerializer.WriteRoomGet(new PacketRoomGet
            {
                Found = true,
                RoomVersion = room.Version,
                RoomId = room.Id,
                HostPlayerId = room.HostId,
                RoomName = room.Name,
                RoomType = room.RoomType,
                PlayersToStart = room.PlayersToStart,
                RaceStarted = room.RaceStarted,
                PreparingRace = room.PreparingRace,
                TrackName = room.TrackName,
                Laps = room.Laps,
                Players = BuildRoomPlayers(room)
            }), PacketStream.Query);
        }

        private PacketRoomSummary BuildRoomSummary(RaceRoom room)
        {
            return new PacketRoomSummary
            {
                RoomId = room.Id,
                RoomName = room.Name,
                RoomType = room.RoomType,
                PlayerCount = (byte)Math.Min(ProtocolConstants.MaxPlayers, GetRoomParticipantCount(room)),
                PlayersToStart = room.PlayersToStart,
                RaceStarted = room.RaceStarted,
                TrackName = room.TrackName
            };
        }

        private PacketRoomPlayer[] BuildRoomPlayers(RaceRoom room)
        {
            return room.PlayerIds
                .Where(id => _players.ContainsKey(id))
                .Select(id => _players[id])
                .Select(p => new PacketRoomPlayer
                {
                    PlayerId = p.Id,
                    PlayerNumber = p.PlayerNumber,
                    State = p.State,
                    Name = string.IsNullOrWhiteSpace(p.Name) ? $"Player {p.PlayerNumber + 1}" : p.Name
                })
                .Concat(room.Bots.Select(bot => new PacketRoomPlayer
                {
                    PlayerId = bot.Id,
                    PlayerNumber = bot.PlayerNumber,
                    State = bot.State,
                    Name = FormatBotDisplayName(bot)
                }))
                .OrderBy(p => p.PlayerNumber)
                .ToArray();
        }

        private uint TouchRoomVersion(RaceRoom room)
        {
            if (room == null)
                return 0;

            room.Version++;
            if (room.Version == 0)
                room.Version = 1;
            return room.Version;
        }

    }
}
