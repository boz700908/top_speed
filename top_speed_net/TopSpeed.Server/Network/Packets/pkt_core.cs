using System.Net;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void RegisterCorePackets()
        {
            _pktReg.Add("core", Command.KeepAlive, (_, _, _) => { });
            _pktReg.Add("core", Command.Ping, (player, _, _) =>
            {
                SendStream(player, PacketSerializer.WriteGeneral(Command.Pong), PacketStream.Control);
            });
            _pktReg.Add("core", Command.PlayerHello, (player, payload, endPoint) =>
            {
                if (PacketSerializer.TryReadPlayerHello(payload, out var hello))
                    HandlePlayerHello(player, hello);
                else
                    PacketFail(endPoint, Command.PlayerHello);
            });
        }

        private void PacketFail(IPEndPoint endPoint, Command command)
        {
            _logger.Warning($"Failed to parse {command} packet from {endPoint}.");
        }

        private PlayerConnection? GetOrAddPlayer(IPEndPoint endpoint)
        {
            var key = endpoint.ToString();
            if (_endpointIndex.TryGetValue(key, out var id) && _players.TryGetValue(id, out var existing))
                return existing;

            if (_players.Count >= _config.MaxPlayers)
            {
                SendStream(endpoint, PacketSerializer.WriteDisconnect("This server is full."), PacketStream.Control);
                _logger.Warning($"Refused connection from {endpoint}: server is full.");
                return null;
            }

            var playerId = _nextPlayerId++;
            var player = new PlayerConnection(endpoint, playerId);
            _players[playerId] = player;
            _endpointIndex[key] = playerId;

            _logger.Info($"Connection pending protocol negotiation: playerId={player.Id}, endpoint={endpoint}.");
            return player;
        }
    }
}
