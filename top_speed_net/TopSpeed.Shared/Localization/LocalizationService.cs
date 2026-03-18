using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace TopSpeed.Localization
{
    public static class LocalizationService
    {
        private static ITextLocalizer _localizer = PassthroughLocalizer.Instance;

        public static string Mark(string? messageId)
        {
            return messageId ?? string.Empty;
        }

        internal static void SetLocalizer(ITextLocalizer? localizer)
        {
            Volatile.Write(ref _localizer, localizer ?? PassthroughLocalizer.Instance);
        }

        public static string Translate(string? messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return string.Empty;

            var localizer = Volatile.Read(ref _localizer);
            var sourceText = messageId!;
            var localizedText = localizer.Translate(sourceText);
            return ResolveTranslatedText(sourceText, localizedText);
        }

        public static string Translate(string? context, string? messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(context))
                return Translate(messageId);

            var localizer = Volatile.Read(ref _localizer);
            var sourceText = messageId!;
            var localizedText = localizer.Translate(context!, sourceText);
            return ResolveTranslatedText(sourceText, localizedText);
        }

        public static string Format(string? template, params object[]? arguments)
        {
            var sourceTemplate = template ?? string.Empty;
            if (string.IsNullOrWhiteSpace(sourceTemplate))
                return string.Empty;

            var localizedTemplate = Translate(sourceTemplate);
            var resolvedTemplate = ResolveFormatTemplate(sourceTemplate, localizedTemplate, arguments);
            if (TryApplyFormat(resolvedTemplate, arguments, out var formatted))
                return formatted;

            if (!string.Equals(resolvedTemplate, sourceTemplate, StringComparison.Ordinal) &&
                TryApplyFormat(sourceTemplate, arguments, out formatted))
                return formatted;

            return resolvedTemplate;
        }

        public static string Format(string? context, string? template, params object[]? arguments)
        {
            var sourceTemplate = template ?? string.Empty;
            if (string.IsNullOrWhiteSpace(sourceTemplate))
                return string.Empty;

            var localizedTemplate = Translate(context, sourceTemplate);
            var resolvedTemplate = ResolveFormatTemplate(sourceTemplate, localizedTemplate, arguments);
            if (TryApplyFormat(resolvedTemplate, arguments, out var formatted))
                return formatted;

            if (!string.Equals(resolvedTemplate, sourceTemplate, StringComparison.Ordinal) &&
                TryApplyFormat(sourceTemplate, arguments, out formatted))
                return formatted;

            return resolvedTemplate;
        }

        private static bool TryApplyFormat(string template, object[]? arguments, out string formatted)
        {
            formatted = string.Empty;
            if (string.IsNullOrEmpty(template))
                return true;
            if (arguments == null || arguments.Length == 0)
            {
                formatted = template;
                return true;
            }

            try
            {
                formatted = string.Format(CultureInfo.CurrentCulture, template, arguments);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string ResolveFormatTemplate(string sourceTemplate, string localizedTemplate, object[]? arguments)
        {
            if (ShouldFallbackToSourceTemplate(sourceTemplate, localizedTemplate, arguments))
                return sourceTemplate;

            return string.IsNullOrWhiteSpace(localizedTemplate)
                ? sourceTemplate
                : localizedTemplate;
        }

        private static string ResolveTranslatedText(string sourceText, string localizedText)
        {
            if (string.IsNullOrWhiteSpace(sourceText))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(localizedText))
                return sourceText;

            if (string.Equals(sourceText, localizedText, StringComparison.Ordinal))
                return sourceText;

            if (ContainsLetters(sourceText) && !ContainsLetters(localizedText))
                return sourceText;

            var sourceHasBraces = sourceText.IndexOf('{') >= 0 || sourceText.IndexOf('}') >= 0;
            var localizedHasBraces = localizedText.IndexOf('{') >= 0 || localizedText.IndexOf('}') >= 0;
            if (!sourceHasBraces && localizedHasBraces)
                return sourceText;

            if (sourceHasBraces && !TryExtractPlaceholderIndexes(localizedText, out _))
                return sourceText;

            return localizedText;
        }

        private static bool ShouldFallbackToSourceTemplate(string sourceTemplate, string localizedTemplate, object[]? arguments)
        {
            if (string.IsNullOrWhiteSpace(sourceTemplate))
                return false;
            if (string.IsNullOrWhiteSpace(localizedTemplate))
                return true;
            if (string.Equals(sourceTemplate, localizedTemplate, StringComparison.Ordinal))
                return false;

            if (ContainsLetters(sourceTemplate) && !ContainsLetters(localizedTemplate))
                return true;

            if (arguments != null && arguments.Length > 0 && !CanFormatTemplate(localizedTemplate, arguments.Length))
                return true;

            if (arguments == null || arguments.Length == 0)
                return false;

            if (!TryExtractPlaceholderIndexes(sourceTemplate, out var sourceIndexes) || sourceIndexes.Count == 0)
                return false;

            if (!TryExtractPlaceholderIndexes(localizedTemplate, out var localizedIndexes))
                return true;

            foreach (var index in sourceIndexes)
            {
                if (!localizedIndexes.Contains(index))
                    return true;
            }

            foreach (var index in localizedIndexes)
            {
                if (!sourceIndexes.Contains(index))
                    return true;
            }

            return false;
        }

        private static bool ContainsLetters(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            for (var i = 0; i < text.Length; i++)
            {
                if (char.IsLetter(text, i))
                    return true;
            }

            return false;
        }

        private static bool TryExtractPlaceholderIndexes(string template, out HashSet<int> indexes)
        {
            indexes = new HashSet<int>();
            if (string.IsNullOrEmpty(template))
                return true;

            for (var i = 0; i < template.Length; i++)
            {
                var ch = template[i];
                if (ch == '{')
                {
                    if (i + 1 < template.Length && template[i + 1] == '{')
                    {
                        i++;
                        continue;
                    }

                    i++;
                    if (i >= template.Length || !char.IsDigit(template[i]))
                        return false;

                    var index = 0;
                    while (i < template.Length && char.IsDigit(template[i]))
                    {
                        index = (index * 10) + (template[i] - '0');
                        i++;
                    }

                    indexes.Add(index);

                    var closed = false;
                    while (i < template.Length)
                    {
                        if (template[i] == '}')
                        {
                            closed = true;
                            break;
                        }

                        if (template[i] == '{')
                            return false;

                        i++;
                    }

                    if (!closed)
                        return false;
                }
                else if (ch == '}')
                {
                    if (i + 1 < template.Length && template[i + 1] == '}')
                    {
                        i++;
                        continue;
                    }

                    return false;
                }
            }

            return true;
        }

        private static bool CanFormatTemplate(string template, int argumentCount)
        {
            if (!TryExtractPlaceholderIndexes(template, out var indexes))
                return false;

            foreach (var index in indexes)
            {
                if (index < 0 || index >= argumentCount)
                    return false;
            }

            return true;
        }
    }
}
