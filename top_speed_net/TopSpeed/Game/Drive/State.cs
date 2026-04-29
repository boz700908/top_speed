namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private static bool IsRaceState(AppState state)
        {
            return state == AppState.TimeTrial
                || state == AppState.SingleRace
                || state == AppState.MultiplayerRace
                || state == AppState.Paused;
        }

        private static bool IsMenuState(AppState state)
        {
            return state == AppState.Logo
                || state == AppState.Menu
                || state == AppState.Calibration;
        }
    }
}

