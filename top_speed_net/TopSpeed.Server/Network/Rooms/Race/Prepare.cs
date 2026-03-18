using System;
using System.Linq;
using LiteNetLib;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Localization;
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
                SendProtocolMessageToRoom(
                    room,
                    LocalizationService.Format(
                        LocalizationService.Mark("{0} is ready."),
                        FormatBotJoinName(bot)));
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
                room.PrepareSkips.Clear();
                TouchRoomVersion(room);
                EmitRoomLifecycleEvent(room, RoomEventKind.PrepareCancelled);
                SendProtocolMessageToRoom(room, LocalizationService.Mark("Race start cancelled because there are not enough players."));
                _logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Race prepare cancelled: room={0} \"{1}\", participants={2}, minStart={3}, capacity={4}."),
                    room.Id,
                    room.Name,
                    GetRoomParticipantCount(room),
                    minimumParticipants,
                    room.PlayersToStart));
                return;
            }

            var readyHumans = CountReadyHumans(room);
            var skippedHumans = CountSkippedHumans(room);
            var unresolvedHumans = Math.Max(0, room.PlayerIds.Count - (readyHumans + skippedHumans));
            if (unresolvedHumans > 0)
            {
                _logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Waiting for loadouts: room={0}, ready={1}, skipped={2}, totalHumans={3}."),
                    room.Id,
                    readyHumans,
                    skippedHumans,
                    room.PlayerIds.Count));
                return;
            }

            var activeParticipants = readyHumans + room.Bots.Count;
            if (activeParticipants < minimumParticipants)
            {
                room.PreparingRace = false;
                room.PendingLoadouts.Clear();
                room.PrepareSkips.Clear();
                TouchRoomVersion(room);
                EmitRoomLifecycleEvent(room, RoomEventKind.PrepareCancelled);
                SendProtocolMessageToRoom(room, LocalizationService.Mark("Race start cancelled because there are not enough ready players."));
                _logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Race prepare cancelled after loadout: room={0} \"{1}\", active={2}, minStart={3}."),
                    room.Id,
                    room.Name,
                    activeParticipants,
                    minimumParticipants));
                return;
            }

            room.PreparingRace = false;
            SendProtocolMessageToRoom(room, LocalizationService.Mark("All players are ready. Starting game."));
            _logger.Info(LocalizationService.Format(
                LocalizationService.Mark("All loadouts ready: room={0} \"{1}\", starting race."),
                room.Id,
                room.Name));
            StartRace(room);
        }

        private int CountReadyHumans(RaceRoom room)
        {
            return room.PendingLoadouts.Keys.Count(id => room.PlayerIds.Contains(id));
        }

        private int CountSkippedHumans(RaceRoom room)
        {
            return room.PrepareSkips.Count(id => room.PlayerIds.Contains(id));
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
