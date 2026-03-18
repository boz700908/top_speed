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
        private void HandlePlayerReady(PlayerConnection player, PacketRoomPlayerReady ready)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, LocalizationService.Mark("You are not in a game room."));
                return;
            }

            if (!room.PreparingRace)
            {
                SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Race setup has not started yet."));
                return;
            }

            if (!room.PlayerIds.Contains(player.Id))
            {
                SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, LocalizationService.Mark("You are not in this game room."));
                return;
            }

            var selectedCar = NormalizeNetworkCar(ready.Car);
            player.Car = selectedCar;
            ApplyVehicleDimensions(player, selectedCar);
            room.PrepareSkips.Remove(player.Id);
            room.PendingLoadouts[player.Id] = new PlayerLoadout(selectedCar, ready.AutomaticTransmission);
            _logger.Debug(LocalizationService.Format(
                LocalizationService.Mark("Player ready: room={0}, player={1}, car={2}, automatic={3}, ready={4}/{5}."),
                room.Id,
                player.Id,
                selectedCar,
                ready.AutomaticTransmission,
                room.PendingLoadouts.Count,
                room.PlayerIds.Count));
            SendProtocolMessageToRoom(
                room,
                LocalizationService.Format(
                    LocalizationService.Mark("{0} is ready."),
                    DescribePlayer(player)));
            TryStartRaceAfterLoadout(room);
        }

        private void HandlePlayerWithdraw(PlayerConnection player)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, LocalizationService.Mark("You are not in a game room."));
                return;
            }

            if (!room.PreparingRace)
            {
                SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Race setup has not started yet."));
                return;
            }

            if (!room.PlayerIds.Contains(player.Id))
            {
                SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, LocalizationService.Mark("You are not in this game room."));
                return;
            }

            room.PendingLoadouts.Remove(player.Id);
            room.PrepareSkips.Add(player.Id);
            player.State = PlayerState.NotReady;
            TouchRoomVersion(room);
            EmitRoomParticipantEvent(
                room,
                RoomEventKind.ParticipantStateChanged,
                player.Id,
                player.PlayerNumber,
                player.State,
                string.IsNullOrWhiteSpace(player.Name)
                    ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1)
                    : player.Name);
            SendProtocolMessageToRoom(
                room,
                LocalizationService.Format(
                    LocalizationService.Mark("{0} left race preparation."),
                    DescribePlayer(player)));
            TryStartRaceAfterLoadout(room);
        }

    }
}
