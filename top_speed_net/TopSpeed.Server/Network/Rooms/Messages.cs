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
        private void SendProtocolMessage(PlayerConnection player, ProtocolMessageCode code, string text)
        {
            SendStream(player, PacketSerializer.WriteProtocolMessage(new PacketProtocolMessage
            {
                Code = code,
                Message = text ?? string.Empty
            }), PacketStream.Direct);
        }

        private void SendProtocolMessageToRoom(RaceRoom room, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var payload = PacketSerializer.WriteProtocolMessage(new PacketProtocolMessage
            {
                Code = ProtocolMessageCode.Ok,
                Message = text
            });

            SendToRoomOnStream(room, payload, PacketStream.Chat);
        }

        private void BroadcastLobbyAnnouncement(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            foreach (var player in _players.Values)
            {
                if (player.RoomId.HasValue)
                    continue;

                SendProtocolMessage(player, ProtocolMessageCode.Ok, text);
            }
        }

        private static string DescribePlayer(PlayerConnection player)
        {
            if (!string.IsNullOrWhiteSpace(player.Name))
                return player.Name;
            return "A player";
        }

    }
}
