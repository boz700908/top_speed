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
        private void AssignRandomBotLoadouts(RaceRoom room)
        {
            foreach (var bot in room.Bots)
            {
                bot.Car = (CarType)_random.Next((int)CarType.Vehicle1, (int)CarType.CustomVehicle);
                bot.AutomaticTransmission = _random.Next(0, 2) == 0;
                ApplyVehicleDimensions(bot, bot.Car);
            }
        }

        private void AnnounceBotsReady(RaceRoom room)
        {
            foreach (var bot in room.Bots.OrderBy(b => b.PlayerNumber))
            {
                SendProtocolMessageToRoom(room, $"{FormatBotJoinName(bot)} is ready.");
            }
        }

        private void TryStartRaceAfterLoadout(RaceRoom room)
        {
            if (!room.PreparingRace)
                return;
            var minimumParticipants = GetMinimumParticipantsToStart(room);
            if (GetRoomParticipantCount(room) < minimumParticipants)
            {
                room.PreparingRace = false;
                room.PendingLoadouts.Clear();
                TouchRoomVersion(room);
                EmitRoomLifecycleEvent(room, RoomEventKind.PrepareCancelled);
                SendProtocolMessageToRoom(room, "Race start cancelled because there are not enough players.");
                _logger.Info($"Race prepare cancelled: room={room.Id} \"{room.Name}\", participants={GetRoomParticipantCount(room)}, minStart={minimumParticipants}, capacity={room.PlayersToStart}.");
                return;
            }
            if (room.PendingLoadouts.Count < room.PlayerIds.Count)
            {
                _logger.Debug($"Waiting for loadouts: room={room.Id}, ready={room.PendingLoadouts.Count}/{room.PlayerIds.Count}.");
                return;
            }

            room.PreparingRace = false;
            SendProtocolMessageToRoom(room, "All players are ready. Starting game.");
            _logger.Info($"All loadouts ready: room={room.Id} \"{room.Name}\", starting race.");
            StartRace(room);
        }

        private static int GetMinimumParticipantsToStart(RaceRoom room)
        {
            if (room == null)
                return 1;

            // Room player count now acts as capacity. One-on-one still requires two racers.
            return 2;
        }

    }
}
