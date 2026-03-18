using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GetText;

namespace TopSpeed.Localization
{
    internal sealed class CatalogLocalizer : ITextLocalizer
    {
        private readonly Catalog _primaryCatalog;

        private CatalogLocalizer(Catalog primaryCatalog)
        {
            _primaryCatalog = primaryCatalog;
        }

        public static ITextLocalizer Create(string? languageCode, string languagesRoot)
        {
            if (string.IsNullOrWhiteSpace(languagesRoot))
                return PassthroughLocalizer.Instance;

            if (IsSourceLanguage(languageCode))
                return PassthroughLocalizer.Instance;

            var primaryCatalog = TryLoadCatalog(languageCode, languagesRoot);
            if (primaryCatalog == null)
                return PassthroughLocalizer.Instance;

            return new CatalogLocalizer(primaryCatalog);
        }

        public string Translate(string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return string.Empty;

            var resolved = _primaryCatalog.GetStringDefault(messageId, messageId);
            if (!string.Equals(resolved, messageId, StringComparison.Ordinal))
                return resolved;

            return messageId;
        }

        public string Translate(string context, string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return string.Empty;

            if (TryGetContextTranslation(_primaryCatalog, context, messageId, out var translated))
                return translated;

            return Translate(messageId);
        }

        private static bool TryGetContextTranslation(Catalog catalog, string context, string messageId, out string translation)
        {
            translation = string.Empty;
            if (catalog == null || string.IsNullOrWhiteSpace(context) || string.IsNullOrWhiteSpace(messageId))
                return false;

            var key = context + Catalog.CONTEXTGLUE + messageId;
            if (!catalog.Translations.TryGetValue(key, out var forms) || forms == null || forms.Length == 0)
                return false;

            var form = forms[0];
            if (string.IsNullOrWhiteSpace(form))
                return false;

            translation = form;
            return true;
        }

        private static Catalog? TryLoadCatalog(string? languageCode, string languagesRoot)
        {
            foreach (var candidate in BuildLanguageCandidates(languageCode))
            {
                var moFile = Path.Combine(languagesRoot, candidate, "messages.mo");
                if (!File.Exists(moFile))
                    continue;

                try
                {
                    using var stream = File.OpenRead(moFile);
                    return new Catalog(stream, ResolveCulture(candidate));
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }

        private static IEnumerable<string> BuildLanguageCandidates(string? languageCode)
        {
            var normalized = LanguageCode.Normalize(languageCode);
            if (normalized.Length > 0)
                yield return normalized;
        }

        private static bool IsSourceLanguage(string? languageCode)
        {
            var normalized = LanguageCode.Normalize(languageCode);
            if (string.IsNullOrWhiteSpace(normalized))
                return true;

            if (string.Equals(normalized, "en", StringComparison.OrdinalIgnoreCase))
                return true;

            return normalized.StartsWith("en-", StringComparison.OrdinalIgnoreCase);
        }

        private static CultureInfo ResolveCulture(string languageCode)
        {
            return LanguageCode.TryResolveCulture(languageCode) ?? CultureInfo.InvariantCulture;
        }

    }
}
