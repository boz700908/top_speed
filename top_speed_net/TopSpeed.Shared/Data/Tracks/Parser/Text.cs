using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace TopSpeed.Data
{
    public static partial class TrackTsmParser
    {
        private static bool TryParseSectionHeader(
            string line,
            out string kind,
            out string id,
            out TrackSoundSourceType? impliedSoundType)
        {
            kind = string.Empty;
            id = string.Empty;
            impliedSoundType = null;
            if (!line.StartsWith("[", StringComparison.Ordinal) ||
                !line.EndsWith("]", StringComparison.Ordinal) ||
                line.Length < 3)
            {
                return false;
            }

            var raw = line.Substring(1, line.Length - 2).Trim();
            if (raw.Length == 0)
                return false;

            var sep = raw.IndexOf(':');
            if (sep < 0)
                sep = raw.IndexOf(' ');

            if (sep >= 0)
            {
                kind = NormalizeIdentifier(raw.Substring(0, sep));
                id = raw.Substring(sep + 1).Trim();
            }
            else
            {
                kind = NormalizeIdentifier(raw);
            }

            if (kind == "ambient")
            {
                kind = "sound";
                impliedSoundType = TrackSoundSourceType.Ambient;
                return true;
            }

            if (kind == "static" || kind == "static_source")
            {
                kind = "sound";
                impliedSoundType = TrackSoundSourceType.Static;
                return true;
            }

            if (kind == "moving" || kind == "moving_source")
            {
                kind = "sound";
                impliedSoundType = TrackSoundSourceType.Moving;
                return true;
            }

            if (kind == "random" || kind == "random_source")
            {
                kind = "sound";
                impliedSoundType = TrackSoundSourceType.Random;
                return true;
            }

            return kind.Length > 0;
        }

        private static bool TryParseKeyValue(string line, out string key, out string value)
        {
            key = string.Empty;
            value = string.Empty;
            var sep = line.IndexOf('=');
            if (sep < 0)
                sep = line.IndexOf(':');
            if (sep <= 0)
                return false;

            key = line.Substring(0, sep).Trim();
            value = line.Substring(sep + 1).Trim();
            return key.Length > 0;
        }

        private static string NormalizeIdentifier(string raw)
        {
            return raw
                .Trim()
                .ToLowerInvariant();
        }

        private static string NormalizeLookupToken(string raw)
        {
            return NormalizeIdentifier(raw).Replace("_", string.Empty);
        }

        private static string? NormalizeNullable(string raw)
        {
            var trimmed = raw.Trim().Trim('"');
            return trimmed.Length == 0 ? null : trimmed;
        }

        private static string StripInlineComment(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            var hash = line.IndexOf('#');
            var semi = line.IndexOf(';');
            var cut = -1;
            if (hash >= 0 && semi >= 0)
                cut = Math.Min(hash, semi);
            else if (hash >= 0)
                cut = hash;
            else if (semi >= 0)
                cut = semi;

            return cut >= 0 ? line.Substring(0, cut) : line;
        }

        private static bool TryParseBool(string raw, out bool value)
        {
            var n = NormalizeLookupToken(raw);
            switch (n)
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                    value = true;
                    return true;
                case "0":
                case "false":
                case "no":
                case "off":
                    value = false;
                    return true;
                default:
                    value = false;
                    return false;
            }
        }

        private static bool TryParseInt(string raw, out int value)
        {
            return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseFloat(string raw, out float value)
        {
            return float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseVector(string raw, out Vector3 value)
        {
            value = default;
            var tokens = raw.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2)
                return false;

            if (!TryParseFloat(tokens[0], out var x))
                return false;

            if (tokens.Length >= 3)
            {
                if (!TryParseFloat(tokens[1], out var y))
                    return false;
                if (!TryParseFloat(tokens[2], out var z))
                    return false;
                value = new Vector3(x, y, z);
                return true;
            }

            if (!TryParseFloat(tokens[1], out var z2))
                return false;
            value = new Vector3(x, 0f, z2);
            return true;
        }

        private static IReadOnlyList<string> ParseCsvList(string raw)
        {
            var list = new List<string>();
            var tokens = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var value = token.Trim();
                if (value.Length > 0)
                    list.Add(value);
            }
            return list;
        }
    }
}
