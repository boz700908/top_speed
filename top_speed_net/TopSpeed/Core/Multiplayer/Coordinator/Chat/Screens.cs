using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Menu;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void UpdateHistoryScreens()
        {
            var items = BuildHistoryItems(_state.Chat.History.GetCurrentEntries());
            TryUpdateChatScreen(MultiplayerMenuKeys.Lobby, items);
            TryUpdateChatScreen(MultiplayerMenuKeys.RoomControls, items);
        }

        private void TryUpdateChatScreen(string menuId, IEnumerable<MenuItem> items)
        {
            try
            {
                _menu.UpdateItems(menuId, MultiplayerScreenKeys.SharedLobbyChat, items, preserveSelection: true);
            }
            catch (InvalidOperationException)
            {
                // Menus may not be registered yet during startup.
            }
        }

        private static string? NormalizeChatMessage(string message)
        {
            var text = (message ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private static List<MenuItem> BuildHistoryItems(IReadOnlyList<string> entries)
        {
            var items = new List<MenuItem>();
            if (entries == null || entries.Count == 0)
            {
                items.Add(new MenuItem(LocalizationService.Mark("No messages yet."), MenuAction.None));
                return items;
            }

            for (var i = 0; i < entries.Count; i++)
                items.Add(new MenuItem(entries[i], MenuAction.None));

            return items;
        }
    }
}



