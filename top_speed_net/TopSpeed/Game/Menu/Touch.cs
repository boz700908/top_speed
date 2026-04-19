using TS.Sdl.Input;
using TopSpeed.Input;
using TopSpeed.Menu;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private bool _multiplayerMenuTouchZonesApplied;

        private void UpdateMultiplayerMenuTouchControls()
        {
            if (!ShouldApplyMultiplayerMenuTouchLayout())
            {
                if (_multiplayerMenuTouchZonesApplied)
                {
                    // Avoid clearing race touch zones when we have already switched to race state.
                    if (!_driveTouchZonesApplied)
                        _input.ClearTouchZones();
                    _multiplayerMenuTouchZonesApplied = false;
                }

                return;
            }

            EnsureMultiplayerMenuTouchZones();
            HandleMultiplayerTopZoneGestures();
        }

        private bool ShouldApplyMultiplayerMenuTouchLayout()
        {
            if (!_isAndroidPlatform || _state != AppState.Menu)
                return false;

            return MenuTouchProfile.UsesMultiplayerZones(_menu.CurrentId);
        }

        private void EnsureMultiplayerMenuTouchZones()
        {
            if (_multiplayerMenuTouchZonesApplied)
                return;

            _input.SetTouchZones(new[]
            {
                new TouchZone(
                    MenuTouchProfile.MultiplayerTopZoneId,
                    new TouchZoneRect(0f, 0f, 1f, MenuTouchProfile.MultiplayerSplitY),
                    priority: 20,
                    behavior: TouchZoneBehavior.Lock),
                new TouchZone(
                    MenuTouchProfile.MultiplayerBottomZoneId,
                    new TouchZoneRect(0f, MenuTouchProfile.MultiplayerSplitY, 1f, 1f - MenuTouchProfile.MultiplayerSplitY),
                    priority: 20,
                    behavior: TouchZoneBehavior.Lock)
            });
            _multiplayerMenuTouchZonesApplied = true;
        }

        private void HandleMultiplayerTopZoneGestures()
        {
            if (HasBlockingMultiplayerOverlay())
                return;

            HandleTopZoneCategoryGestures();
            HandleTopZoneHistoryItemGestures();
            HandleTopZonePingGesture();
            HandleTopZoneChatInputGestures();
        }

        private bool HasBlockingMultiplayerOverlay()
        {
            return _textInputPromptActive
                || _dialogs.HasActiveOverlayDialog
                || _choices.HasActiveChoiceDialog
                || _multiplayerMenuTouch.HasActiveOverlayQuestion;
        }

        private void HandleTopZoneCategoryGestures()
        {
            if (_input.WasZoneGesturePressed(GestureIntent.SwipeUp, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.NextChatCategory();
            else if (_input.WasZoneGesturePressed(GestureIntent.SwipeDown, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.PreviousChatCategory();
        }

        private void HandleTopZoneHistoryItemGestures()
        {
            if (_input.WasZoneGesturePressed(GestureIntent.SwipeRight, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.NextChatItem();
            else if (_input.WasZoneGesturePressed(GestureIntent.SwipeLeft, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.PreviousChatItem();
        }

        private void HandleTopZonePingGesture()
        {
            if (_input.WasZoneGesturePressed(GestureIntent.DoubleTap, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.CheckPing();
        }

        private void HandleTopZoneChatInputGestures()
        {
            if (_input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeRight, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.OpenGlobalChatHotkey();
            else if (_input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeLeft, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.OpenRoomChatHotkey();
        }
    }
}
