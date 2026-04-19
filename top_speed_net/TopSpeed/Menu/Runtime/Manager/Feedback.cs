using System;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuManager
    {
        public bool TryPlayMenuCue(string menuId, MenuFeedbackCue cue)
        {
            if (string.IsNullOrWhiteSpace(menuId) || _stack.Count == 0)
                return false;

            if (!_screens.TryGetValue(menuId, out var screen))
                return false;

            if (!ReferenceEquals(_stack.Peek(), screen))
                return false;

            screen.PlayMenuCue(cue);
            return true;
        }
    }
}
