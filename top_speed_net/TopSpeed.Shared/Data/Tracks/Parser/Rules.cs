using System;
using System.Collections.Generic;
using System.IO;

namespace TopSpeed.Data
{
    public static partial class TrackTsmParser
    {
        private static void ValidateSegmentField(
            string key,
            string value,
            int lineNumber,
            float minPart,
            string sectionId,
            Dictionary<string, string> segmentRooms,
            Dictionary<string, IReadOnlyList<string>> segmentSounds,
            List<TrackTsmIssue> issues)
        {
            if (key == "type")
            {
                if (!TryParseTrackType(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid segment type '{value}'."));
                return;
            }

            if (key == "surface")
            {
                if (!TryParseTrackSurface(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid segment surface '{value}'."));
                return;
            }

            if (key == "noise")
            {
                if (!TryParseTrackNoise(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid segment noise '{value}'."));
                return;
            }

            if (key == "length")
            {
                if (!TryParseFloat(value, out var length))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid segment length '{value}'."));
                else if (length < minPart)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Warning, lineNumber, $"Segment length '{length}' is below minimum {minPart} and will be clamped."));
                return;
            }

            if (key == "width")
            {
                if (!TryParseFloat(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid segment width '{value}'."));
                return;
            }

            if (key == "height")
            {
                if (!TryParseFloat(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid segment height '{value}'."));
                return;
            }

            if (key == "room" || key == "room_profile" || key == "room_preset")
            {
                var roomId = NormalizeNullable(value);
                if (roomId == null)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, "Segment room id cannot be empty."));
                else
                    segmentRooms[sectionId] = roomId;
                return;
            }

            if (TryValidateRoomOverride(key, value, lineNumber, issues))
                return;

            if (key == "sound_sources" || key == "sound_source_ids")
            {
                var list = ParseCsvList(value);
                if (list.Count == 0)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, "Segment sound source list cannot be empty."));
                else
                    segmentSounds[sectionId] = list;
                return;
            }

            if (key == "name")
                return;

            if (!IsMetadataKey(key))
                issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Unknown segment key '{key}'."));
        }

        private static void ValidateRoomField(string key, string value, int lineNumber, List<TrackTsmIssue> issues)
        {
            if (key == "name")
                return;

            if (key == "room_preset")
            {
                if (!TrackRoomLibrary.IsPreset(value.Trim()))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Unknown room preset '{value}'."));
                return;
            }

            if (IsRoomNumericKey(key))
            {
                if (!TryParseFloat(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid room value '{value}' for key '{key}'."));
                return;
            }

            if (!IsMetadataKey(key))
                issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Unknown room key '{key}'."));
        }

        private static void ValidateSoundField(
            string key,
            string value,
            int lineNumber,
            string sectionId,
            Dictionary<string, string> soundStartAreas,
            Dictionary<string, string> soundEndAreas,
            List<TrackTsmIssue> issues)
        {
            if (key == "type")
            {
                if (!TryParseSoundType(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid sound type '{value}'."));
                return;
            }

            if (key == "path" || key == "file")
            {
                if (!IsValidTrackRelativeSoundPath(value))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Sound key '{key}' must be a track-relative path and cannot escape the track folder."));
                return;
            }

            if (key == "variant_paths")
            {
                var list = ParseCsvList(value);
                if (list.Count == 0)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Sound list for '{key}' cannot be empty."));
                else
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        if (!IsValidTrackRelativeSoundPath(list[i]))
                            issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Sound key '{key}' contains invalid path '{list[i]}'. Paths must be track-relative."));
                    }
                }
                return;
            }

            if (key == "variant_source_ids")
            {
                var list = ParseCsvList(value);
                if (list.Count == 0)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Sound list for '{key}' cannot be empty."));
                return;
            }

            if (key == "random_mode")
            {
                if (!TryParseSoundRandomMode(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid sound random mode '{value}'."));
                return;
            }

            if (IsSoundBooleanKey(key))
            {
                if (!TryParseBool(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid boolean '{value}' for key '{key}'."));
                return;
            }

            if (key == "start_area")
            {
                var area = NormalizeNullable(value);
                if (area == null)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, "start_area cannot be empty."));
                else
                    soundStartAreas[sectionId] = area;
                return;
            }

            if (key == "end_area")
            {
                var area = NormalizeNullable(value);
                if (area == null)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, "end_area cannot be empty."));
                else
                    soundEndAreas[sectionId] = area;
                return;
            }

            if (IsSoundVectorKey(key))
            {
                if (!TryParseVector(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid vector '{value}' for key '{key}'."));
                return;
            }

            if (IsSoundNumericKey(key))
            {
                if (!TryParseFloat(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid numeric value '{value}' for key '{key}'."));
                return;
            }

            if (!IsMetadataKey(key))
                issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Unknown sound key '{key}'."));
        }

        private static bool TryValidateRoomOverride(string key, string value, int lineNumber, List<TrackTsmIssue> issues)
        {
            if (!IsRoomOverrideKey(key))
                return false;

            if (!TryParseFloat(value, out _))
            {
                issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid room override value '{value}' for key '{key}'."));
            }

            return true;
        }

        private static bool IsRoomNumericKey(string key)
        {
            return key == "reverb_time" ||
                   key == "reverb_gain" ||
                   key == "hf_decay_ratio" ||
                   key == "late_reverb_gain" ||
                   key == "diffusion" ||
                   key == "air_absorption" ||
                   key == "occlusion_scale" ||
                   key == "transmission_scale" ||
                   IsRoomOverrideKey(key);
        }

        private static bool IsRoomOverrideKey(string key)
        {
            return key == "occlusion_override" ||
                   key == "transmission_override" ||
                   key == "transmission_override_low" ||
                   key == "transmission_override_mid" ||
                   key == "transmission_override_high" ||
                   key == "air_absorption_override" ||
                   key == "air_absorption_override_low" ||
                   key == "air_absorption_override_mid" ||
                   key == "air_absorption_override_high";
        }

        private static bool IsSoundNumericKey(string key)
        {
            return key == "volume" ||
                   key == "fade_in" ||
                   key == "fade_out" ||
                   key == "crossfade_seconds" ||
                   key == "pitch" ||
                   key == "pan" ||
                   key == "min_distance" ||
                   key == "max_distance" ||
                   key == "rolloff" ||
                   key == "start_radius" ||
                   key == "end_radius" ||
                   key == "speed" ||
                   key == "speed_meters_per_second";
        }

        private static bool IsSoundBooleanKey(string key)
        {
            return key == "loop" ||
                   key == "spatial" ||
                   key == "allow_hrtf" ||
                   key == "global";
        }

        private static bool IsSoundVectorKey(string key)
        {
            return key == "start_position" ||
                   key == "end_position" ||
                   key == "position";
        }

        private static bool IsValidTrackRelativeSoundPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var trimmed = value.Trim();
            if (Path.IsPathRooted(trimmed) || trimmed.IndexOf(':') >= 0)
                return false;

            var normalized = trimmed.Replace('\\', '/');
            var parts = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return false;

            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i].Trim();
                if (part.Length == 0 || part == "." || part == "..")
                    return false;
            }

            return true;
        }

        private static bool IsMetadataKey(string key)
        {
            return key.StartsWith("meta", StringComparison.OrdinalIgnoreCase) ||
                   key.StartsWith("metadata", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidWeather(string raw)
        {
            if (TryParseInt(raw, out var weatherInt))
                return weatherInt >= 0 && weatherInt <= 3;
            var normalized = NormalizeLookupToken(raw);
            return normalized == "sunny" ||
                   normalized == "rain" ||
                   normalized == "rainy" ||
                   normalized == "wind" ||
                   normalized == "windy" ||
                   normalized == "storm" ||
                   normalized == "stormy";
        }

        private static bool IsValidAmbience(string raw)
        {
            if (TryParseInt(raw, out var ambienceInt))
                return ambienceInt >= 0 && ambienceInt <= 2;
            var normalized = NormalizeLookupToken(raw);
            return normalized == "noambience" ||
                   normalized == "none" ||
                   normalized == "desert" ||
                   normalized == "airport";
        }
    }
}
