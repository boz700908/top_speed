using System;
using System.Collections.Generic;
using System.Globalization;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
    {
        private static string RequireString(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return string.Empty;
            }

            var value = entry.Value.Trim();
            if (value.Length == 0)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' in section [{1}] must not be empty.", key, section.Name)));
            return value;
        }

        private static string? OptionalString(Section section, string key)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
                return null;
            var value = entry.Value.Trim();
            return value.Length == 0 ? null : value;
        }

        private static IReadOnlyList<string> RequireCsvStrings(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return Array.Empty<string>();
            }

            var values = ParseCsvStrings(entry.Value);
            if (values.Count == 0)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must contain at least one path.", key)));
            return values;
        }

        private static IReadOnlyList<string> OptionalCsvStrings(Section section, string key)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
                return Array.Empty<string>();
            return ParseCsvStrings(entry.Value);
        }

        private static int RequireIntRange(Section section, string key, int min, int max, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return min;
            }

            if (!int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must be an integer.", key)));
                return min;
            }

            if (value < min || value > max)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' value {1} is outside allowed range {2} to {3}.", key, value, min, max)));
            return value;
        }

        private static int? OptionalInt(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
                return null;
            if (!int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must be an integer.", key)));
                return null;
            }
            return value;
        }

        private static float RequireFloatRange(Section section, string key, float min, float max, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return min;
            }

            if (!TryParseFloat(entry.Value, out var value))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must be a float.", key)));
                return min;
            }

            if (value < min || value > max)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    entry.Line,
                    Localized(
                        "Key '{0}' value {1} is outside allowed range {2} to {3}.",
                        key,
                        value.ToString(CultureInfo.InvariantCulture),
                        min.ToString(CultureInfo.InvariantCulture),
                        max.ToString(CultureInfo.InvariantCulture))));
            }

            return value;
        }

        private static float? OptionalFloat(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
                return null;
            if (!TryParseFloat(entry.Value, out var value))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must be a float.", key)));
                return null;
            }
            return value;
        }

        private static bool RequireBoolInt(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return false;
            }

            if (TryParseBool(entry.Value, out var boolValue))
                return boolValue;
            if (int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                return intValue != 0;

            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must be a boolean or 0/1 integer.", key)));
            return false;
        }

        private static bool? OptionalBoolInt(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
                return null;

            if (TryParseBool(entry.Value, out var boolValue))
                return boolValue;
            if (int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                return intValue != 0;

            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must be a boolean or 0/1 integer.", key)));
            return null;
        }

        private static List<float>? RequireFloatCsv(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return null;
            }

            var values = new List<float>();
            var tokens = entry.Value.Split(',');
            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i].Trim();
                if (token.Length == 0)
                    continue;
                if (!TryParseFloat(token, out var parsed))
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' contains a non-float value '{1}'.", key, token)));
                    return null;
                }
                values.Add(parsed);
            }

            if (values.Count == 0)
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must contain at least one float value.", key)));
                return null;
            }

            return values;
        }
    }
}
