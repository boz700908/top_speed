using System;
using System.Collections.Generic;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;
namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void OpenRoomOptionsMenu()
        {
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak(LocalizationService.Mark("You are not currently inside a game room."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.IsHost)
            {
                _speech.Speak(LocalizationService.Mark("Only the host can change game options."));
                return;
            }

            BeginRoomOptionsDraft();
            RebuildRoomOptionsMenu();
            _menu.Push(MultiplayerMenuKeys.RoomOptions);
        }

        private void BeginRoomOptionsDraft()
        {
            _state.RoomDrafts.RoomOptionsDraftActive = true;
            _state.RoomDrafts.RoomOptionsTrackRandom = false;
            var currentTrack = _state.Rooms.CurrentRoom.Track;
            if (currentTrack == null || !PacketValidation.IsValidTrackPackageRef(currentTrack))
                currentTrack = TrackPackageRef.BuiltIn(_state.Rooms.CurrentRoom.TrackName);
            if (currentTrack == null || !PacketValidation.IsValidTrackPackageRef(currentTrack))
                currentTrack = TrackPackageRef.BuiltIn(TrackList.RaceTracks[0].Key);

            _state.RoomDrafts.RoomOptionsTrack = CloneTrackRef(currentTrack);
            _state.RoomDrafts.RoomOptionsTrackName = _state.RoomDrafts.RoomOptionsTrack.IsBuiltIn
                ? _state.RoomDrafts.RoomOptionsTrack.BuiltInTrackKey
                : _state.RoomDrafts.RoomOptionsTrack.TrackId;
            _state.RoomDrafts.RoomOptionsTrackDisplayName = FormatTrackRefDisplay(_state.RoomDrafts.RoomOptionsTrack);
            _state.RoomDrafts.RoomOptionsLaps = _state.Rooms.CurrentRoom.Laps > 0 ? _state.Rooms.CurrentRoom.Laps : (byte)1;
            _state.RoomDrafts.RoomOptionsPlayersToStart = _state.Rooms.CurrentRoom.PlayersToStart >= 2 ? _state.Rooms.CurrentRoom.PlayersToStart : (byte)2;
            _state.RoomDrafts.RoomOptionsGameRulesFlags = _state.Rooms.CurrentRoom.GameRulesFlags;
            if (_state.Rooms.CurrentRoom.RoomType == GameRoomType.OneOnOne)
                _state.RoomDrafts.RoomOptionsPlayersToStart = 2;
        }

        private void CancelRoomOptionsChanges()
        {
            _state.RoomDrafts.RoomOptionsDraftActive = false;
            _state.RoomDrafts.RoomOptionsTrackRandom = false;
            _state.RoomDrafts.RoomOptionsTrack = TrackPackageRef.BuiltIn(string.Empty);
            _state.RoomDrafts.RoomOptionsTrackDisplayName = string.Empty;
            _state.RoomDrafts.RoomOptionsGameRulesFlags = 0;
            _state.RoomDrafts.RoomTrackCatalogOpenPending = false;
            _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
        }

        private void ConfirmRoomOptionsChanges()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost || !_state.RoomDrafts.RoomOptionsDraftActive)
            {
                _speech.Speak(LocalizationService.Mark("Only the host can change game options."));
                return;
            }

            var appliedAny = false;
            var currentTrack = _state.Rooms.CurrentRoom.Track;
            if (currentTrack == null || !PacketValidation.IsValidTrackPackageRef(currentTrack))
                currentTrack = TrackPackageRef.BuiltIn(_state.Rooms.CurrentRoom.TrackName);
            if (currentTrack == null || !PacketValidation.IsValidTrackPackageRef(currentTrack))
                currentTrack = TrackPackageRef.BuiltIn(TrackList.RaceTracks[0].Key);

            if (!TrackRefsEqual(currentTrack, _state.RoomDrafts.RoomOptionsTrack))
            {
                if (!TrySend(session.SendRoomSetTrack(_state.RoomDrafts.RoomOptionsTrack), LocalizationService.Mark("track change request")))
                    return;
                appliedAny = true;
            }

            if (_state.Rooms.CurrentRoom.Laps != _state.RoomDrafts.RoomOptionsLaps)
            {
                if (!TrySend(session.SendRoomSetLaps(_state.RoomDrafts.RoomOptionsLaps), LocalizationService.Mark("lap count change request")))
                    return;
                appliedAny = true;
            }

            if (_state.Rooms.CurrentRoom.RoomType != GameRoomType.OneOnOne)
            {
                var playersToStart = _state.RoomDrafts.RoomOptionsPlayersToStart < 2 ? (byte)2 : _state.RoomDrafts.RoomOptionsPlayersToStart;
                if (_state.Rooms.CurrentRoom.PlayersToStart != playersToStart)
                {
                    if (!TrySend(session.SendRoomSetPlayersToStart(playersToStart), LocalizationService.Mark("player count change request")))
                        return;
                    appliedAny = true;
                }
            }

            var gameRules = _state.RoomDrafts.RoomOptionsGameRulesFlags
                & ((uint)RoomGameRules.GhostMode | (uint)RoomGameRules.CustomTracks);
            if (_state.Rooms.CurrentRoom.GameRulesFlags != gameRules)
            {
                if (!TrySend(session.SendRoomSetGameRules(gameRules), LocalizationService.Mark("game rules change request")))
                    return;
                appliedAny = true;
            }

            CancelRoomOptionsChanges();
            _menu.ShowRoot(MultiplayerMenuKeys.RoomControls);
            _speech.Speak(appliedAny
                ? LocalizationService.Mark("Room options updated.")
                : LocalizationService.Mark("No option changes to apply."));
        }

        private string GetRoomOptionsTrackText()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            if (_state.RoomDrafts.RoomOptionsTrackRandom)
            {
                return LocalizationService.Mark("Track, currently random chosen.");
            }

            var trackName = string.IsNullOrWhiteSpace(_state.RoomDrafts.RoomOptionsTrackDisplayName)
                ? FormatTrackRefDisplay(_state.RoomDrafts.RoomOptionsTrack)
                : _state.RoomDrafts.RoomOptionsTrackDisplayName;
            return LocalizationService.Format(
                LocalizationService.Mark("Track, currently {0}."),
                trackName);
        }

        private int GetRoomOptionsLapsIndex()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            var laps = _state.RoomDrafts.RoomOptionsLaps < 1 ? (byte)1 : _state.RoomDrafts.RoomOptionsLaps;
            return Math.Max(0, Math.Min(LapCountOptions.Length - 1, laps - 1));
        }

        private void SetRoomOptionsLaps(byte laps)
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            if (laps < 1 || laps > 16)
                return;
            _state.RoomDrafts.RoomOptionsLaps = laps;
        }

        private int GetRoomOptionsPlayersToStartIndex()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            var playersToStart = _state.RoomDrafts.RoomOptionsPlayersToStart < 2 ? (byte)2 : _state.RoomDrafts.RoomOptionsPlayersToStart;
            return Math.Max(0, Math.Min(RoomCapacityOptions.Length - 1, playersToStart - 2));
        }

        private void SetRoomOptionsPlayersToStart(byte playersToStart)
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            if (_state.Rooms.CurrentRoom.RoomType == GameRoomType.OneOnOne)
            {
                _state.RoomDrafts.RoomOptionsPlayersToStart = 2;
                return;
            }

            if (playersToStart < 2 || playersToStart > ProtocolConstants.MaxRoomPlayersToStart)
                return;

            _state.RoomDrafts.RoomOptionsPlayersToStart = playersToStart;
        }

        private void OpenRoomTrackTypeMenu()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            RebuildRoomTrackTypeMenu();
            RebuildRoomTrackMenu(MultiplayerMenuKeys.RoomTrackRace, TrackCategory.RaceTrack);
            RebuildRoomTrackMenu(MultiplayerMenuKeys.RoomTrackAdventure, TrackCategory.StreetAdventure);
            RebuildRoomTrackCustomMenu();
            RebuildRoomTrackLocalCustomMenu();
            _menu.Push(MultiplayerMenuKeys.RoomTrackType);
        }

        private void OpenRoomGameRulesMenu()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            RebuildRoomGameRulesMenu();
            _menu.Push(MultiplayerMenuKeys.RoomGameRules);
        }

        private bool GetRoomOptionsGhostModeEnabled()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            return (_state.RoomDrafts.RoomOptionsGameRulesFlags & (uint)RoomGameRules.GhostMode) != 0u;
        }

        private void SetRoomOptionsGhostModeEnabled(bool enabled)
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            var flags = _state.RoomDrafts.RoomOptionsGameRulesFlags;
            if (enabled)
                flags |= (uint)RoomGameRules.GhostMode;
            else
                flags &= ~(uint)RoomGameRules.GhostMode;

            _state.RoomDrafts.RoomOptionsGameRulesFlags = flags;
        }

        private bool GetRoomOptionsCustomTracksEnabled()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            return (_state.RoomDrafts.RoomOptionsGameRulesFlags & (uint)RoomGameRules.CustomTracks) != 0u;
        }

        private bool IsCurrentRoomCustomTracksEnabled()
        {
            return (_state.Rooms.CurrentRoom.GameRulesFlags & (uint)RoomGameRules.CustomTracks) != 0u;
        }

        private void SetRoomOptionsCustomTracksEnabled(bool enabled)
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            var flags = _state.RoomDrafts.RoomOptionsGameRulesFlags;
            if (enabled)
                flags |= (uint)RoomGameRules.CustomTracks;
            else
                flags &= ~(uint)RoomGameRules.CustomTracks;

            _state.RoomDrafts.RoomOptionsGameRulesFlags = flags;
        }

        private void AnnounceCurrentRoomGameRules()
        {
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak(LocalizationService.Mark("You are not currently inside a game room."));
                return;
            }

            var room = _state.Rooms.CurrentRoom;
            _speech.Speak(FormatGameRulesSummary(
                room.GameRulesFlags,
                room.Track,
                room.TrackName,
                room.Laps,
                room.PlayersToStart));
        }

        private static string FormatGameRulesSummary(
            uint gameRulesFlags,
            TrackPackageRef track,
            string trackName,
            byte laps,
            byte playersToStart)
        {
            var ghostEnabled = (gameRulesFlags & (uint)RoomGameRules.GhostMode) != 0u;
            var customTracksEnabled = (gameRulesFlags & (uint)RoomGameRules.CustomTracks) != 0u;
            var trackDisplay = ResolveTrackAnnouncement(track, trackName);
            var normalizedLaps = laps > 0 ? laps : (byte)1;
            var normalizedPlayers = playersToStart >= 2 ? playersToStart : (byte)2;
            var lapsText = LocalizationService.Format(
                normalizedLaps == 1
                    ? LocalizationService.Mark("{0} lap")
                    : LocalizationService.Mark("{0} laps"),
                normalizedLaps);
            var playersText = LocalizationService.Format(
                normalizedPlayers == 1
                    ? LocalizationService.Mark("{0} player")
                    : LocalizationService.Mark("{0} players"),
                normalizedPlayers);
            return LocalizationService.Format(
                LocalizationService.Mark("Ghost mode is {0}. Custom tracks are {1}. The chosen track is {2}. The game will run for {3}. This room is limited to {4}."),
                ghostEnabled
                    ? LocalizationService.Translate(LocalizationService.Mark("enabled"))
                    : LocalizationService.Translate(LocalizationService.Mark("disabled")),
                customTracksEnabled
                    ? LocalizationService.Translate(LocalizationService.Mark("enabled"))
                    : LocalizationService.Translate(LocalizationService.Mark("disabled")),
                trackDisplay,
                lapsText,
                playersText);
        }

        private static string ResolveTrackAnnouncement(TrackPackageRef track, string trackName)
        {
            var display = FormatTrackRefDisplay(track);
            if (!string.IsNullOrWhiteSpace(display))
                return display;

            if (TryGetTrackDisplay(trackName, out var builtInDisplay))
                return LocalizationService.Translate(builtInDisplay);

            var fallback = (trackName ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(fallback)
                ? LocalizationService.Mark("Unknown")
                : fallback;
        }

        private void RebuildRoomTrackTypeMenu()
        {
            var items = new List<MenuItem>();
            items.Add(new MenuItem(LocalizationService.Mark("Race track"), MenuAction.None, nextMenuId: MultiplayerMenuKeys.RoomTrackRace));
            items.Add(new MenuItem(LocalizationService.Mark("Street adventure"), MenuAction.None, nextMenuId: MultiplayerMenuKeys.RoomTrackAdventure));
            if (IsCurrentRoomCustomTracksEnabled())
            {
                items.Add(new MenuItem(
                    LocalizationService.Mark("Custom track"),
                    MenuAction.None,
                    onActivate: OpenRoomTrackCustomMenu));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, onActivate: SelectRandomRoomTrackAny));

            _menu.UpdateItems(MultiplayerMenuKeys.RoomTrackType, items);
        }

        private void RebuildRoomTrackMenu(string menuId, TrackCategory category)
        {
            var items = new List<MenuItem>();
            var tracks = TrackList.GetTracks(category);
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                items.Add(new MenuItem(track.Display, MenuAction.None, onActivate: () => SelectRoomTrack(TrackPackageRef.BuiltIn(track.Key), track.Display, false)));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, onActivate: () => SelectRandomRoomTrackCategory(category)));
            _menu.UpdateItems(menuId, items);
        }

        private void SelectRandomRoomTrackAny()
        {
            if (RoomTrackOptions.Length == 0)
            {
                SelectRoomTrack(TrackList.RaceTracks[0].Key, true);
                return;
            }

            var index = Algorithm.RandomInt(RoomTrackOptions.Length);
            SelectRoomTrack(RoomTrackOptions[index].Key, true);
        }

        private void SelectRandomRoomTrackCategory(TrackCategory category)
        {
            var tracks = TrackList.GetTracks(category);
            if (tracks.Count == 0)
            {
                SelectRandomRoomTrackAny();
                return;
            }

            var index = Algorithm.RandomInt(tracks.Count);
            SelectRoomTrack(tracks[index].Key, true);
        }

        private void SelectRoomTrack(string trackKey, bool randomChosen)
        {
            SelectRoomTrack(TrackPackageRef.BuiltIn(trackKey), string.Empty, randomChosen);
        }

        private void SelectRoomTrack(TrackPackageRef track, string displayName, bool randomChosen)
        {
            var normalized = CloneTrackRef(track);
            if (!PacketValidation.IsValidTrackPackageRef(normalized))
                normalized = TrackPackageRef.BuiltIn(TrackList.RaceTracks[0].Key);

            _state.RoomDrafts.RoomOptionsTrack = normalized;
            _state.RoomDrafts.RoomOptionsTrackName = normalized.IsBuiltIn
                ? normalized.BuiltInTrackKey
                : normalized.TrackId;
            _state.RoomDrafts.RoomOptionsTrackDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? FormatTrackRefDisplay(normalized)
                : displayName;
            _state.RoomDrafts.RoomOptionsTrackRandom = randomChosen;
            ReturnToRoomOptionsMenu();
            _speech.Speak(GetRoomOptionsTrackText());
        }

        private void ReturnToRoomOptionsMenu()
        {
            if (string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomOptions, StringComparison.Ordinal))
                return;

            while (_menu.CanPop && !string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomOptions, StringComparison.Ordinal))
                _menu.PopToPrevious(announceTitle: false);

            if (!string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomOptions, StringComparison.Ordinal))
                _menu.Push(MultiplayerMenuKeys.RoomOptions);
        }

        private static bool TryGetTrackDisplay(string trackKey, out string display)
        {
            display = string.Empty;
            if (string.IsNullOrWhiteSpace(trackKey))
                return false;

            for (var i = 0; i < RoomTrackOptions.Length; i++)
            {
                if (!string.Equals(RoomTrackOptions[i].Key, trackKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                display = RoomTrackOptions[i].Display;
                return true;
            }

            return false;
        }

        private static TrackPackageRef CloneTrackRef(TrackPackageRef track)
        {
            if (track == null)
                return TrackPackageRef.BuiltIn(string.Empty);

            return track.IsCustomPackage
                ? TrackPackageRef.Custom(track.TrackId ?? string.Empty, track.Version ?? string.Empty, track.Hash ?? string.Empty)
                : TrackPackageRef.BuiltIn(track.BuiltInTrackKey ?? string.Empty);
        }

        private static bool TrackRefsEqual(TrackPackageRef left, TrackPackageRef right)
        {
            var a = CloneTrackRef(left);
            var b = CloneTrackRef(right);
            if (a.Kind != b.Kind)
                return false;
            if (a.IsCustomPackage)
            {
                return string.Equals(a.TrackId, b.TrackId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(a.Version, b.Version, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(TrackPackageRef.NormalizeHash(a.Hash), TrackPackageRef.NormalizeHash(b.Hash), StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(a.BuiltInTrackKey, b.BuiltInTrackKey, StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatTrackRefDisplay(TrackPackageRef track)
        {
            if (track != null && track.IsCustomPackage)
            {
                var id = string.IsNullOrWhiteSpace(track.TrackId) ? LocalizationService.Mark("Custom track") : track.TrackId;
                if (string.IsNullOrWhiteSpace(track.Version))
                    return id;
                return id + " (" + track.Version + ")";
            }

            var builtIn = track?.BuiltInTrackKey ?? string.Empty;
            if (TryGetTrackDisplay(builtIn, out var display))
                return LocalizationService.Translate(display);
            return builtIn;
        }
    }
}



