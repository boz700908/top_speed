using System;
using System.Globalization;

namespace TopSpeed.Localization
{
    public static class LanguageCode
    {
        public static string Normalize(string? languageCode)
        {
            var raw = languageCode ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var normalized = raw.Trim()
                .Replace('_', '-')
                .Replace('\\', '-')
                .Replace('/', '-');
            return normalized.ToLowerInvariant();
        }

        public static string ParentOf(string languageCode)
        {
            var normalized = Normalize(languageCode);
            var split = normalized.IndexOf('-');
            if (split <= 0)
                return string.Empty;
            return normalized.Substring(0, split);
        }

        public static CultureInfo? TryResolveCulture(string languageCode)
        {
            var normalized = Normalize(languageCode);
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            try
            {
                return CultureInfo.GetCultureInfo(normalized);
            }
            catch (CultureNotFoundException)
            {
                var parent = ParentOf(normalized);
                if (string.IsNullOrWhiteSpace(parent))
                    return null;

                try
                {
                    return CultureInfo.GetCultureInfo(parent);
                }
                catch (CultureNotFoundException)
                {
                    return null;
                }
            }
        }
    }
}
