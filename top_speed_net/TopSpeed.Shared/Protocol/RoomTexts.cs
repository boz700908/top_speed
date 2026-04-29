using TopSpeed.Localization;

namespace TopSpeed.Protocol
{
    public static class RoomTexts
    {
        public static string AllPlayersReadyStartingGame => LocalizationService.Mark("All players are ready. Starting game.");
        public static string RoomUnavailableFull => LocalizationService.Mark("This game room is unavailable because it is full.");
        public static string NoBotsToRemove => LocalizationService.Mark("There are no bots to remove.");
    }
}
