using System;
using TopSpeed.Menu;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public bool IsInRoom => _roomState.InRoom;

        private const string MultiplayerPingShortcutActionId = "multiplayer_ping";

        private static readonly string[] MultiplayerPingShortcutMenus =
        {
            MultiplayerLobbyMenuId,
            MultiplayerRoomBrowserMenuId,
            MultiplayerCreateRoomMenuId,
            MultiplayerRoomControlsMenuId,
            MultiplayerRoomPlayersMenuId,
            MultiplayerRoomOptionsMenuId,
            MultiplayerLoadoutVehicleMenuId,
            MultiplayerLoadoutTransmissionMenuId
        };

        public void ConfigureMenuCloseHandlers()
        {
            _menu.RegisterSharedShortcutAction(
                MultiplayerPingShortcutActionId,
                new MenuShortcut(SharpDX.DirectInput.Key.F1, CheckCurrentPing));

            for (var i = 0; i < MultiplayerPingShortcutMenus.Length; i++)
            {
                _menu.SetSharedShortcutActions(
                    MultiplayerPingShortcutMenus[i],
                    new[] { MultiplayerPingShortcutActionId });
            }

            _menu.SetCloseHandler(MultiplayerLobbyMenuId, _ =>
            {
                OpenDisconnectConfirmation();
                return true;
            });

            _menu.SetCloseHandler(MultiplayerRoomControlsMenuId, _ =>
            {
                OpenLeaveRoomConfirmation();
                return true;
            });

            _menu.SetCloseHandler(MultiplayerSavedServerFormMenuId, _ =>
            {
                CloseSavedServerForm();
                return true;
            });

            _menu.SetCloseHandler(MultiplayerLoadoutTransmissionMenuId, _ =>
            {
                _menu.ShowRoot(MultiplayerLoadoutVehicleMenuId);
                return true;
            });

            _menu.SetCloseHandler(MultiplayerLoadoutVehicleMenuId, _ =>
            {
                _speech.Speak("Choose your vehicle and transmission mode to get ready for the race.");
                _menu.ShowRoot(MultiplayerLoadoutVehicleMenuId);
                return true;
            });
        }

        public void ShowMultiplayerMenuAfterRace()
        {
            if (_roomState.InRoom)
                _menu.ShowRoot(MultiplayerRoomControlsMenuId);
            else
                _menu.ShowRoot(MultiplayerLobbyMenuId);
        }

        public void BeginRaceLoadoutSelection()
        {
            if (!_roomState.InRoom)
                return;

            _pendingLoadoutVehicleIndex = 0;
            RebuildLoadoutVehicleMenu();
            RebuildLoadoutTransmissionMenu();
            _menu.ShowRoot(MultiplayerLoadoutVehicleMenuId);
            _enterMenuState();
        }
    }
}
