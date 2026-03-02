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
        private PacketRoomEvent CreateRoomEvent(RaceRoom room, RoomEventKind kind)
        {
            return new PacketRoomEvent
            {
                RoomId = room.Id,
                RoomVersion = room.Version,
                Kind = kind,
                HostPlayerId = room.HostId,
                RoomType = room.RoomType,
                PlayerCount = (byte)Math.Min(ProtocolConstants.MaxPlayers, GetRoomParticipantCount(room)),
                PlayersToStart = room.PlayersToStart,
                RaceStarted = room.RaceStarted,
                PreparingRace = room.PreparingRace,
                TrackName = room.TrackName,
                Laps = room.Laps,
                RoomName = room.Name
            };
        }

        private void EmitRoomLifecycleEvent(RaceRoom room, RoomEventKind kind)
        {
            var evt = CreateRoomEvent(room, kind);
            var payload = PacketSerializer.WriteRoomEvent(evt);
            var roomOnly =
                kind == RoomEventKind.HostChanged ||
                kind == RoomEventKind.TrackChanged ||
                kind == RoomEventKind.LapsChanged ||
                kind == RoomEventKind.PlayersToStartChanged ||
                kind == RoomEventKind.PrepareStarted ||
                kind == RoomEventKind.PrepareCancelled;

            if (roomOnly)
            {
                foreach (var id in room.PlayerIds)
                {
                    if (_players.TryGetValue(id, out var player))
                        SendStream(player, payload, PacketStream.Room);
                }
                return;
            }

            foreach (var player in _players.Values)
                SendStream(player, payload, PacketStream.Room);
        }

        private void EmitRoomParticipantEvent(RaceRoom room, RoomEventKind kind, uint playerId, byte playerNumber, PlayerState state, string name)
        {
            var evt = CreateRoomEvent(room, kind);
            evt.SubjectPlayerId = playerId;
            evt.SubjectPlayerNumber = playerNumber;
            evt.SubjectPlayerState = state;
            evt.SubjectPlayerName = name ?? string.Empty;
            var payload = PacketSerializer.WriteRoomEvent(evt);

            foreach (var id in room.PlayerIds)
            {
                if (_players.TryGetValue(id, out var player))
                    SendStream(player, payload, PacketStream.Room);
            }
        }

        private void EmitRoomRemovedEvent(uint roomId, string roomName)
        {
            var evt = new PacketRoomEvent
            {
                RoomId = roomId,
                RoomVersion = 0,
                Kind = RoomEventKind.RoomRemoved,
                RoomName = roomName ?? string.Empty
            };

            var payload = PacketSerializer.WriteRoomEvent(evt);
            foreach (var player in _players.Values)
                SendStream(player, payload, PacketStream.Room);
        }

    }
}
