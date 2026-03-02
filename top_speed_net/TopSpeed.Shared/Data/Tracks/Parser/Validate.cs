using System;
using System.Collections.Generic;
using System.IO;

namespace TopSpeed.Data
{
    public static partial class TrackTsmParser
    {
        private static bool ValidateFile(string filename, float minPart, List<TrackTsmIssue> issues)
        {
            var sectionKind = string.Empty;
            var segmentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var soundIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var segmentRooms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var segmentSounds = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            var soundStartAreas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var soundEndAreas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var currentSectionId = string.Empty;

            int lineNumber = 0;
            foreach (var raw in File.ReadLines(filename))
            {
                lineNumber++;
                var line = StripInlineComment(raw).Trim();
                if (line.Length == 0)
                    continue;

                if (TryParseSectionHeader(line, out var nextKind, out var nextId, out _))
                {
                    sectionKind = nextKind;
                    currentSectionId = nextId.Trim();

                    if (sectionKind != "meta" &&
                        sectionKind != "segment" &&
                        sectionKind != "room" &&
                        sectionKind != "sound")
                    {
                        issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Unknown section '{nextKind}'."));
                        sectionKind = string.Empty;
                        continue;
                    }

                    if ((sectionKind == "segment" || sectionKind == "room" || sectionKind == "sound") &&
                        currentSectionId.Length == 0)
                    {
                        issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Section '{sectionKind}' requires an id."));
                        sectionKind = string.Empty;
                        continue;
                    }

                    if (sectionKind == "segment")
                    {
                        if (!segmentIds.Add(currentSectionId))
                            issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Duplicate segment id '{currentSectionId}'."));
                        if (!segmentSounds.ContainsKey(currentSectionId))
                            segmentSounds[currentSectionId] = Array.Empty<string>();
                    }
                    else if (sectionKind == "room")
                    {
                        if (!roomIds.Add(currentSectionId))
                            issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Duplicate room id '{currentSectionId}'."));
                    }
                    else if (sectionKind == "sound")
                    {
                        if (!soundIds.Add(currentSectionId))
                            issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Duplicate sound id '{currentSectionId}'."));
                    }

                    continue;
                }

                if (!TryParseKeyValue(line, out var rawKey, out var rawValue))
                {
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, "Malformed line. Expected '[section]' or 'key = value'."));
                    continue;
                }

                if (sectionKind.Length == 0)
                {
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Property '{rawKey.Trim()}' is outside any section."));
                    continue;
                }

                var key = NormalizeIdentifier(rawKey);
                var value = rawValue.Trim();
                if (value.Length == 0)
                {
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Key '{rawKey.Trim()}' is missing a value."));
                    continue;
                }

                switch (sectionKind)
                {
                    case "meta":
                        if (key == "weather" && !IsValidWeather(value))
                            issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid weather value '{value}'."));
                        else if (key == "ambience" && !IsValidAmbience(value))
                            issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, $"Invalid ambience value '{value}'."));
                        break;
                    case "segment":
                        ValidateSegmentField(
                            key,
                            value,
                            lineNumber,
                            minPart,
                            currentSectionId,
                            segmentRooms,
                            segmentSounds,
                            issues);
                        break;
                    case "room":
                        ValidateRoomField(key, value, lineNumber, issues);
                        break;
                    case "sound":
                        ValidateSoundField(
                            key,
                            value,
                            lineNumber,
                            currentSectionId,
                            soundStartAreas,
                            soundEndAreas,
                            issues);
                        break;
                }
            }

            if (segmentIds.Count == 0)
                issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, 0, "Track must include at least one [segment:<id>] section."));

            foreach (var pair in segmentRooms)
            {
                var roomId = pair.Value;
                if (string.IsNullOrWhiteSpace(roomId))
                    continue;
                if (!roomIds.Contains(roomId) && !TrackRoomLibrary.IsPreset(roomId))
                {
                    issues.Add(new TrackTsmIssue(
                        TrackTsmIssueSeverity.Error,
                        0,
                        $"Segment '{pair.Key}' references room '{roomId}', but no matching [room:{roomId}] section or preset exists."));
                }
            }

            foreach (var pair in segmentSounds)
            {
                foreach (var soundId in pair.Value)
                {
                    if (!soundIds.Contains(soundId))
                    {
                        issues.Add(new TrackTsmIssue(
                            TrackTsmIssueSeverity.Error,
                            0,
                            $"Segment '{pair.Key}' references sound source '{soundId}', but no matching [sound:{soundId}] section exists."));
                    }
                }
            }

            foreach (var pair in soundStartAreas)
            {
                if (!segmentIds.Contains(pair.Value))
                {
                    issues.Add(new TrackTsmIssue(
                        TrackTsmIssueSeverity.Error,
                        0,
                        $"Sound '{pair.Key}' references start_area '{pair.Value}', but no matching segment id exists."));
                }
            }

            foreach (var pair in soundEndAreas)
            {
                if (!segmentIds.Contains(pair.Value))
                {
                    issues.Add(new TrackTsmIssue(
                        TrackTsmIssueSeverity.Error,
                        0,
                        $"Sound '{pair.Key}' references end_area '{pair.Value}', but no matching segment id exists."));
                }
            }

            for (var i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == TrackTsmIssueSeverity.Error)
                    return false;
            }

            return true;
        }
    }
}
