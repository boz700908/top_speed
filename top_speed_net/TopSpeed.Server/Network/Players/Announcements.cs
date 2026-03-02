using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void BroadcastServerConnectAnnouncement(PlayerConnection connected)
        {
            var name = string.IsNullOrWhiteSpace(connected.Name) ? "A player" : connected.Name;
            var text = $"{name} has connected to the server.";
            foreach (var player in _players.Values)
            {
                if (player.Id == connected.Id || !player.ServerPresenceAnnounced)
                    continue;

                SendProtocolMessage(player, ProtocolMessageCode.ServerPlayerConnected, text);
            }
        }

        private void BroadcastServerDisconnectAnnouncement(PlayerConnection disconnected, string reason)
        {
            var name = string.IsNullOrWhiteSpace(disconnected.Name) ? "A player" : disconnected.Name;
            var normalizedReason = (reason ?? string.Empty).Trim();
            var text = string.Equals(normalizedReason, "timeout", System.StringComparison.OrdinalIgnoreCase)
                ? $"{name} has lost connection to the server."
                : $"{name} has disconnected from the server.";

            foreach (var player in _players.Values)
            {
                if (player.Id == disconnected.Id || !player.ServerPresenceAnnounced)
                    continue;

                SendProtocolMessage(player, ProtocolMessageCode.ServerPlayerDisconnected, text);
            }
        }

    }
}
