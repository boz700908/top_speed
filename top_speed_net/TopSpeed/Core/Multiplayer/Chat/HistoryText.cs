using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer.Chat
{
    internal static class HistoryText
    {
        public static string JoinedRoom(string roomName)
        {
            return LocalizationService.Translate(LocalizationService.Mark("You joined "))
                   + NormalizeRoomName(roomName)
                   + ".";
        }

        public static string LeftRoom()
        {
            return LocalizationService.Mark("You left the game room.");
        }

        public static string BecameHost()
        {
            return LocalizationService.Mark("You are now host of this game.");
        }

        public static string ParticipantJoined(RoomEventInfo roomEvent)
        {
            return ResolvePlayerName(roomEvent)
                   + LocalizationService.Translate(LocalizationService.Mark(" joined the current room."));
        }

        public static string ParticipantLeft(RoomEventInfo roomEvent)
        {
            return ResolvePlayerName(roomEvent)
                   + LocalizationService.Translate(LocalizationService.Mark(" left the current room."));
        }

        public static string FromRoomEvent(RoomEventInfo roomEvent)
        {
            var roomName = NormalizeRoomName(roomEvent.RoomName);
            switch (roomEvent.Kind)
            {
                case RoomEventKind.RaceStarted:
                    return LocalizationService.Translate(LocalizationService.Mark("Race started in "))
                           + roomName
                           + ".";
                case RoomEventKind.RaceStopped:
                    return LocalizationService.Translate(LocalizationService.Mark("Race stopped in "))
                           + roomName
                           + ".";
                default:
                    return string.Empty;
            }
        }

        private static string ResolvePlayerName(RoomEventInfo roomEvent)
        {
            if (!string.IsNullOrWhiteSpace(roomEvent.SubjectPlayerName))
                return roomEvent.SubjectPlayerName.Trim();
            return LocalizationService.Translate(LocalizationService.Mark("Player "))
                   + (roomEvent.SubjectPlayerNumber + 1);
        }

        private static string NormalizeRoomName(string roomName)
        {
            if (!string.IsNullOrWhiteSpace(roomName))
                return roomName.Trim();
            return LocalizationService.Translate(LocalizationService.Mark("game room"));
        }
    }
}
