using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void OpenRoomBrowser()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (_state.Rooms.IsRoomBrowserOpenPending)
                return;

            _state.Rooms.IsRoomBrowserOpenPending = true;
            if (!TrySend(session.SendRoomListRequest(), LocalizationService.Mark("room list request")))
                _state.Rooms.IsRoomBrowserOpenPending = false;
        }

        private void JoinRoom(uint roomId)
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            TrySend(session.SendRoomJoin(roomId), LocalizationService.Mark("room join request"));
        }

        private void UpdateRoomBrowserMenu()
        {
            var items = new List<MenuItem>();
            var rooms = _state.Rooms.RoomList.Rooms ?? Array.Empty<RoomSummaryInfo>();
            if (rooms.Length == 0)
            {
                items.Add(new MenuItem(LocalizationService.Mark("No game rooms found"), MenuAction.None));
            }
            else
            {
                foreach (var room in rooms)
                {
                    var roomCopy = room;
                    var label = BuildRoomBrowserLabel(roomCopy);
                    items.Add(new MenuItem(label, MenuAction.None, onActivate: () => JoinRoom(roomCopy.RoomId)));
                }
            }

            items.Add(new MenuItem(LocalizationService.Mark("Return to multiplayer lobby"), MenuAction.Back));
            _menu.UpdateItems(MultiplayerMenuKeys.RoomBrowser, items);
        }

        private static string BuildRoomBrowserLabel(RoomSummaryInfo room)
        {
            var typeText = room.RoomType switch
            {
                GameRoomType.OneOnOne => LocalizationService.Translate(LocalizationService.Mark("one-on-one")),
                GameRoomType.PlayersRace => LocalizationService.Translate(LocalizationService.Mark("race without bots")),
                _ => LocalizationService.Translate(LocalizationService.Mark("race with bots"))
            };

            var label = typeText;
            if (!string.IsNullOrWhiteSpace(room.RoomName))
                label += ", " + room.RoomName;
            label += LocalizationService.Translate(LocalizationService.Mark(" game with "))
                     + room.PlayerCount
                     + LocalizationService.Translate(LocalizationService.Mark(" people"));
            label += LocalizationService.Translate(LocalizationService.Mark(", maximum "))
                     + room.PlayersToStart
                     + LocalizationService.Translate(LocalizationService.Mark(" players"));
            if (room.RaceStarted)
                label += LocalizationService.Mark(", in progress");
            else if (room.PlayerCount >= room.PlayersToStart)
                label += LocalizationService.Mark(", room is full");
            return label;
        }
    }
}
