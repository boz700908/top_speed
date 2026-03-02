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
        private bool TryGetHostedRoom(PlayerConnection player, out RaceRoom room)
        {
            room = null!;
            if (!player.RoomId.HasValue)
            {
                SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, "You are not in a game room.");
                return false;
            }

            if (!_rooms.TryGetValue(player.RoomId.Value, out var foundRoom) || foundRoom == null)
            {
                SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, "You are not in a game room.");
                return false;
            }

            room = foundRoom;

            if (room.HostId != player.Id)
            {
                SendProtocolMessage(player, ProtocolMessageCode.NotHost, "Only host can do this.");
                return false;
            }

            return true;
        }

        private void SetTrack(RaceRoom room, string trackName)
        {
            room.TrackName = trackName;
            room.TrackData = TrackLoader.LoadTrack(room.TrackName, room.Laps, _logger);
            room.TrackSelected = true;
        }

    }
}
