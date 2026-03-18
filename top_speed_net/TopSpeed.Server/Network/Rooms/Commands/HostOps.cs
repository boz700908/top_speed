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
        private void HandleSetTrack(PlayerConnection player, PacketRoomSetTrack packet)
        {
            if (!TryGetHostedRoom(player, out var room))
                return;
            if (room.RaceStarted || room.PreparingRace)
            {
                _roomMutationDenied++;
                _logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Room track change denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                    room.Id,
                    player.Id,
                    room.RaceStarted,
                    room.PreparingRace));
                SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot change track while race setup or race is active."));
                return;
            }

            var trackName = (packet.TrackName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trackName))
            {
                SendProtocolMessage(player, ProtocolMessageCode.InvalidTrack, LocalizationService.Mark("Track cannot be empty."));
                return;
            }

            SetTrack(room, trackName);
            SendTrackToNotReady(room);
            TouchRoomVersion(room);
            EmitRoomLifecycleEvent(room, RoomEventKind.TrackChanged);
        }

        private void HandleSetLaps(PlayerConnection player, PacketRoomSetLaps packet)
        {
            if (!TryGetHostedRoom(player, out var room))
                return;
            if (room.RaceStarted || room.PreparingRace)
            {
                _roomMutationDenied++;
                _logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Room laps change denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                    room.Id,
                    player.Id,
                    room.RaceStarted,
                    room.PreparingRace));
                SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot change laps while race setup or race is active."));
                return;
            }

            if (packet.Laps < 1 || packet.Laps > 16)
            {
                SendProtocolMessage(player, ProtocolMessageCode.InvalidLaps, LocalizationService.Mark("Laps must be between 1 and 16."));
                return;
            }

            room.Laps = packet.Laps;
            if (room.TrackSelected)
                SetTrack(room, room.TrackName);
            SendTrackToNotReady(room);
            TouchRoomVersion(room);
            EmitRoomLifecycleEvent(room, RoomEventKind.LapsChanged);
        }

        private void HandleStartRace(PlayerConnection player)
        {
            if (!TryGetHostedRoom(player, out var room))
                return;

            var minimumParticipants = GetMinimumParticipantsToStart(room);
            if (GetRoomParticipantCount(room) < minimumParticipants)
            {
                SendProtocolMessage(
                    player,
                    ProtocolMessageCode.Failed,
                    LocalizationService.Format(
                        LocalizationService.Mark("Not enough players. {0} required to start."),
                        minimumParticipants));
                return;
            }

            if (room.RaceStarted)
            {
                SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("A race is already in progress."));
                return;
            }

            if (room.PreparingRace)
            {
                SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Race setup is already in progress."));
                return;
            }

            room.PreparingRace = true;
            room.PendingLoadouts.Clear();
            room.PrepareSkips.Clear();
            AssignRandomBotLoadouts(room);
            AnnounceBotsReady(room);
            TouchRoomVersion(room);
            _logger.Info(LocalizationService.Format(
                LocalizationService.Mark("Race prepare started: room={0} \"{1}\", requestedBy={2}, humans={3}, bots={4}, capacity={5}, minStart={6}."),
                room.Id,
                room.Name,
                player.Id,
                room.PlayerIds.Count,
                room.Bots.Count,
                room.PlayersToStart,
                minimumParticipants));

            SendProtocolMessageToRoom(
                room,
                LocalizationService.Format(
                    LocalizationService.Mark("{0} is about to start the game. Choose your vehicle and transmission mode."),
                    DescribePlayer(player)));
            EmitRoomLifecycleEvent(room, RoomEventKind.PrepareStarted);
            TryStartRaceAfterLoadout(room);
        }

        private void HandleSetPlayersToStart(PlayerConnection player, PacketRoomSetPlayersToStart packet)
        {
            if (!TryGetHostedRoom(player, out var room))
                return;
            if (room.RaceStarted || room.PreparingRace)
            {
                _roomMutationDenied++;
                _logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Room player-limit change denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                    room.Id,
                    player.Id,
                    room.RaceStarted,
                    room.PreparingRace));
                SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot change player limit while race setup or race is active."));
                return;
            }

            var value = packet.PlayersToStart;
            if (value < 2 || value > ProtocolConstants.MaxRoomPlayersToStart)
            {
                SendProtocolMessage(player, ProtocolMessageCode.InvalidPlayersToStart, LocalizationService.Mark("Player limit must be between 2 and 10."));
                return;
            }

            if (room.RoomType == GameRoomType.OneOnOne && value != 2)
            {
                SendProtocolMessage(player, ProtocolMessageCode.InvalidPlayersToStart, LocalizationService.Mark("One-on-one rooms always allow a maximum of 2 players."));
                return;
            }

            if (GetRoomParticipantCount(room) > value)
            {
                SendProtocolMessage(player, ProtocolMessageCode.InvalidPlayersToStart, LocalizationService.Mark("Cannot set lower than current players in room."));
                return;
            }

            room.PlayersToStart = value;
            TouchRoomVersion(room);
            EmitRoomLifecycleEvent(room, RoomEventKind.PlayersToStartChanged);
            EmitRoomLifecycleEvent(room, RoomEventKind.RoomSummaryUpdated);
        }

    }
}
