using System.Collections.Generic;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(
                    "Game settings",
                    MenuAction.None,
                    nextMenuId: "options_game",
                    hint: "Configure general gameplay and interface behavior, including units, usage hints, menu behavior, and update checking."),
                new MenuItem(
                    "Audio",
                    MenuAction.None,
                    nextMenuId: "options_audio",
                    hint: "Configure spatial audio behavior, including HRTF processing, stereo widening, and automatic device format detection."),
                new MenuItem("Volume settings", MenuAction.None, nextMenuId: "options_volume",
                    onActivate: () =>
                    {
                        _settings.SyncAudioCategoriesFromMusicVolume();
                        _audio.ApplyAudioSettings();
                    },
                    hint: "Adjust category-based volume balance for engine sounds, effects, ambience, radio, music, and online events."),
                new MenuItem(
                    "Controls",
                    MenuAction.None,
                    nextMenuId: "options_controls",
                    hint: "Configure input devices, force feedback, progressive keyboard behavior, key mappings, and menu shortcut mappings."),
                new MenuItem(
                    "Race settings",
                    MenuAction.None,
                    nextMenuId: "options_race",
                    hint: "Set race behavior defaults such as copilot callouts, curve announcements, automatic info, laps, computer opponents, and difficulty."),
                new MenuItem(
                    "Server settings",
                    MenuAction.None,
                    nextMenuId: "options_server",
                    hint: "Configure default multiplayer hosting settings, including the server port used by the game."),
                new MenuItem(
                    "Restore default settings",
                    MenuAction.None,
                    nextMenuId: "options_restore",
                    hint: "Reset all configurable settings back to their default values."),
                BackItem()
            };
            return _menu.CreateMenu("options_main", items);
        }
    }
}
