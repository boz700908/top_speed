namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        void IMultiplayerRuntime.NextChatCategory()
        {
            NextChatCategory();
        }

        void IMultiplayerRuntime.PreviousChatCategory()
        {
            PreviousChatCategory();
        }

        void IMultiplayerRuntime.NextChatItem()
        {
            NextChatItem();
        }

        void IMultiplayerRuntime.PreviousChatItem()
        {
            PreviousChatItem();
        }

        void IMultiplayerRuntime.CheckPing()
        {
            CheckCurrentPing();
        }

        void IMultiplayerRuntime.OpenGlobalChatHotkey()
        {
            OpenGlobalChatHotkey();
        }

        void IMultiplayerRuntime.OpenRoomChatHotkey()
        {
            OpenRoomChatHotkey();
        }

        bool IMultiplayerMenuTouch.HasActiveOverlayQuestion => _questions.HasActiveOverlayQuestion;

        void IMultiplayerMenuTouch.NextChatCategory()
        {
            NextChatCategory();
        }

        void IMultiplayerMenuTouch.PreviousChatCategory()
        {
            PreviousChatCategory();
        }

        void IMultiplayerMenuTouch.NextChatItem()
        {
            NextChatItem();
        }

        void IMultiplayerMenuTouch.PreviousChatItem()
        {
            PreviousChatItem();
        }

        void IMultiplayerMenuTouch.CheckPing()
        {
            CheckCurrentPing();
        }

        void IMultiplayerMenuTouch.OpenGlobalChatHotkey()
        {
            OpenGlobalChatHotkey();
        }

        void IMultiplayerMenuTouch.OpenRoomChatHotkey()
        {
            OpenRoomChatHotkey();
        }

        string IMultiplayerRuntime.ResolvePlayerName(byte playerNumber)
        {
            return ResolvePlayerName(playerNumber);
        }
    }
}

