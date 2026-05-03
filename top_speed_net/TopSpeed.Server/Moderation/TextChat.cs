using System;
using TopSpeed.Localization;
using TopSpeed.Server.Config;

namespace TopSpeed.Server.Moderation
{
    internal static class TextChatModeration
    {
        public static bool TryAllowTextChat(ServerFeaturesSettings settings, out string message)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (settings.TextChat)
            {
                message = string.Empty;
                return true;
            }

            message = LocalizationService.Mark("Text chat is disabled on this server.");
            return false;
        }
    }
}
