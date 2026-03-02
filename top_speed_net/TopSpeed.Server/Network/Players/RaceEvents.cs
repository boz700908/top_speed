using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void HandlePlayerFinished(PlayerConnection player, PacketPlayer finished)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                _authorityDropsPlayerFinished++;
                return;
            }
            if (!room.RaceStarted)
            {
                _authorityDropsPlayerFinished++;
                return;
            }

            if (finished.PlayerId != player.Id || finished.PlayerNumber != player.PlayerNumber)
            {
                _authorityDropsPlayerFinished++;
                _logger.Debug($"PlayerFinished payload mismatch: room={room.Id}, connectionPlayer={player.Id}/{player.PlayerNumber}, payload={finished.PlayerId}/{finished.PlayerNumber}.");
            }

            player.State = PlayerState.Finished;
            if (!room.RaceResults.Contains(player.PlayerNumber))
                room.RaceResults.Add(player.PlayerNumber);

            SendToRoomExceptOnStream(room, player.Id, PacketSerializer.WritePlayer(Command.PlayerFinished, player.Id, player.PlayerNumber), PacketStream.RaceEvent);
            _logger.Debug($"Player finished: room={room.Id}, player={player.Id}, number={player.PlayerNumber}, results={room.RaceResults.Count}.");
            if (CountActiveRaceParticipants(room) == 0)
                StopRace(room);
        }

        private void HandlePlayerCrashed(PlayerConnection player, PacketPlayer crashed)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                _authorityDropsPlayerCrashed++;
                return;
            }
            if (!room.RaceStarted)
            {
                _authorityDropsPlayerCrashed++;
                return;
            }

            if (crashed.PlayerId != player.Id || crashed.PlayerNumber != player.PlayerNumber)
            {
                _authorityDropsPlayerCrashed++;
                _logger.Debug($"PlayerCrashed payload mismatch: room={room.Id}, connectionPlayer={player.Id}/{player.PlayerNumber}, payload={crashed.PlayerId}/{crashed.PlayerNumber}.");
            }

            SendToRoomExceptOnStream(room, player.Id, PacketSerializer.WritePlayer(Command.PlayerCrashed, player.Id, player.PlayerNumber), PacketStream.RaceEvent);
        }

    }
}
