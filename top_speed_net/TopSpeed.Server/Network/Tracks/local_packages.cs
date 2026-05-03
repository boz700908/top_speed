using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void RefreshServerTrackPackages()
        {
            var tracksRoot = GetServerTracksDirectory();
            var discovered = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(tracksRoot))
            {
                RemoveStaleServerTrackPackages(discovered);
                return;
            }

            var files = EnumerateServerTrackFiles(tracksRoot);
            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file))
                    continue;

                DateTime lastWriteUtc;
                try
                {
                    lastWriteUtc = File.GetLastWriteTimeUtc(file);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                discovered[file] = lastWriteUtc;
                if (HasServerTrackPackageForSource(file, lastWriteUtc))
                    continue;

                if (!TryBuildServerTrackPackage(file, out var payload, out var bytes, out var error))
                {
                    _logger.Warning(LocalizationService.Format(
                        LocalizationService.Mark("Skipping server track package '{0}': {1}"),
                        file,
                        error));
                    continue;
                }

                var hash = TrackPackageRef.NormalizeHash(payload.Manifest.Hash);
                if (_trackPackageCache.TryGetValue(hash, out var existing) && existing != null)
                {
                    existing.FromServerTracksFolder = true;
                    existing.SourcePath = file;
                    existing.SourceLastWriteUtc = lastWriteUtc;
                    continue;
                }

                StoreTrackPackage(
                    payload,
                    bytes,
                    fromServerTracksFolder: true,
                    sourcePath: file,
                    sourceLastWriteUtc: lastWriteUtc);
            }

            RemoveStaleServerTrackPackages(discovered);
        }

        private void RemoveStaleServerTrackPackages(IReadOnlyDictionary<string, DateTime> discovered)
        {
            var keys = _trackPackageCache
                .Where(pair => pair.Value != null && pair.Value.FromServerTracksFolder)
                .Where(pair =>
                {
                    var sourcePath = pair.Value.SourcePath ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(sourcePath))
                        return true;

                    if (!discovered.TryGetValue(sourcePath, out var sourceLastWriteUtc))
                        return true;

                    return pair.Value.SourceLastWriteUtc != sourceLastWriteUtc;
                })
                .Select(pair => pair.Key)
                .ToArray();

            for (var i = 0; i < keys.Length; i++)
            {
                if (IsTrackPackageInUse(keys[i]))
                    continue;
                _trackPackageCache.Remove(keys[i]);
            }
        }

        private bool IsTrackPackageInUse(string hash)
        {
            var normalizedHash = TrackPackageRef.NormalizeHash(hash);
            foreach (var room in _rooms.Values)
            {
                if (room.TrackSelection == null || !room.TrackSelection.IsCustomPackage)
                    continue;
                if (string.Equals(
                        TrackPackageRef.NormalizeHash(room.TrackSelection.Hash),
                        normalizedHash,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasServerTrackPackageForSource(string sourcePath, DateTime sourceLastWriteUtc)
        {
            foreach (var package in _trackPackageCache.Values)
            {
                if (package == null || !package.FromServerTracksFolder)
                    continue;
                if (!string.Equals(package.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (package.SourceLastWriteUtc != sourceLastWriteUtc)
                    continue;
                return true;
            }

            return false;
        }

        private static string GetServerTracksDirectory()
        {
            return Path.Combine(AppContext.BaseDirectory, "Tracks");
        }

        private static IReadOnlyList<string> EnumerateServerTrackFiles(string tracksRoot)
        {
            if (!Directory.Exists(tracksRoot))
                return Array.Empty<string>();

            try
            {
                return Directory.EnumerateFiles(tracksRoot, "*.tsm", SearchOption.AllDirectories)
                    .Select(Path.GetFullPath)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
        }

        private static bool TryBuildServerTrackPackage(
            string trackFile,
            out TrackPackagePayload payload,
            out byte[] bytes,
            out string error)
        {
            payload = new TrackPackagePayload();
            bytes = Array.Empty<byte>();
            error = string.Empty;

            if (!TrackTsmParser.TryLoadFromFile(trackFile, out var trackData, out var issues))
            {
                error = BuildTrackLoadError(issues);
                return false;
            }

            if (!TryBuildServerTrackPackagePayload(trackData, trackFile, out payload, out error))
                return false;

            var hash = TrackPackageCodec.ComputeHash(payload);
            payload.Manifest.Hash = hash;
            if (!TrackPackageCodec.TryValidate(payload, out error))
                return false;

            bytes = TrackPackageCodec.Serialize(payload);
            return true;
        }

        private static bool TryBuildServerTrackPackagePayload(
            TrackData trackData,
            string trackFile,
            out TrackPackagePayload payload,
            out string error)
        {
            payload = new TrackPackagePayload();
            error = string.Empty;

            if (trackData == null)
            {
                error = LocalizationService.Mark("Track data is missing.");
                return false;
            }

            if (!TryBuildServerTrackAssetBlobs(trackData, out var assets, out error))
                return false;

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (trackData.Metadata != null)
            {
                foreach (var pair in trackData.Metadata)
                    metadata[pair.Key] = pair.Value ?? string.Empty;
            }

            var displayName = ResolveTrackDisplayName(string.Empty, trackData, trackFile);
            if (!metadata.ContainsKey("name"))
                metadata["name"] = displayName;

            var laps = trackData.Laps > 0 ? trackData.Laps : (byte)3;
            payload.Manifest = new TrackPackageManifest
            {
                TrackId = ResolveTrackId(trackData, trackFile),
                Version = ResolveTrackVersion(trackData),
                Hash = string.Empty,
                DefaultWeatherProfileId = trackData.DefaultWeatherProfileId,
                Ambience = trackData.Ambience,
                Laps = laps
            };
            payload.Definitions = trackData.Definitions ?? Array.Empty<TrackDefinition>();
            payload.Metadata = metadata;
            payload.RoomProfiles = trackData.RoomProfiles ?? new Dictionary<string, TrackRoomDefinition>(StringComparer.OrdinalIgnoreCase);
            payload.WeatherProfiles = trackData.WeatherProfiles ?? new Dictionary<string, TrackWeatherProfile>(StringComparer.OrdinalIgnoreCase);
            payload.SoundDefinitions = trackData.SoundSources ?? new Dictionary<string, TrackSoundSourceDefinition>(StringComparer.OrdinalIgnoreCase);
            payload.AssetBlobs = assets;
            return true;
        }

        private static bool TryBuildServerTrackAssetBlobs(
            TrackData trackData,
            out IReadOnlyDictionary<string, byte[]> assets,
            out string error)
        {
            assets = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            error = string.Empty;

            var sounds = trackData.SoundSources ?? new Dictionary<string, TrackSoundSourceDefinition>(StringComparer.OrdinalIgnoreCase);
            if (sounds.Count == 0)
                return true;

            var sourcePath = trackData.SourcePath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                error = LocalizationService.Mark("Track source path is missing, so sound assets cannot be resolved.");
                return false;
            }

            var trackRoot = Path.GetDirectoryName(Path.GetFullPath(sourcePath));
            if (string.IsNullOrWhiteSpace(trackRoot))
            {
                error = LocalizationService.Mark("Unable to resolve custom track folder path.");
                return false;
            }

            var map = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in sounds)
            {
                var sound = pair.Value;
                if (sound == null)
                    continue;

                if (!TryAddServerTrackAsset(trackRoot, sound.Path, map, out error))
                    return false;

                var variants = sound.VariantPaths ?? Array.Empty<string>();
                for (var i = 0; i < variants.Count; i++)
                {
                    if (!TryAddServerTrackAsset(trackRoot, variants[i], map, out error))
                        return false;
                }
            }

            assets = map;
            return true;
        }

        private static bool TryAddServerTrackAsset(
            string trackRoot,
            string? relativeAssetPath,
            Dictionary<string, byte[]> map,
            out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(relativeAssetPath))
                return true;

            var key = TrackPackageCodec.NormalizeAssetKey(relativeAssetPath);
            if (string.IsNullOrWhiteSpace(key))
            {
                error = LocalizationService.Format(LocalizationService.Mark("Invalid sound asset path: {0}"), relativeAssetPath);
                return false;
            }

            if (map.ContainsKey(key))
                return true;

            var relativePath = key.Replace('/', Path.DirectorySeparatorChar);
            var absolutePath = Path.GetFullPath(Path.Combine(trackRoot, relativePath));
            if (!IsPathInsideRoot(absolutePath, trackRoot))
            {
                error = LocalizationService.Format(LocalizationService.Mark("Sound asset path escapes the track folder: {0}"), relativeAssetPath);
                return false;
            }

            if (!File.Exists(absolutePath))
            {
                error = LocalizationService.Format(LocalizationService.Mark("Missing sound asset file: {0}"), relativeAssetPath);
                return false;
            }

            try
            {
                map[key] = File.ReadAllBytes(absolutePath);
                return true;
            }
            catch (IOException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool IsPathInsideRoot(string candidatePath, string rootPath)
        {
            if (string.Equals(candidatePath, rootPath, StringComparison.OrdinalIgnoreCase))
                return true;

            var rootPrefix = rootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return candidatePath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveTrackDisplayName(string selectedDisplayName, TrackData trackData, string trackFile)
        {
            var selected = (selectedDisplayName ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(selected))
                return selected;

            var name = (trackData?.Name ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(name))
                return name;

            var directory = Path.GetDirectoryName(trackFile);
            var folder = string.IsNullOrWhiteSpace(directory) ? string.Empty : Path.GetFileName(directory);
            if (!string.IsNullOrWhiteSpace(folder))
                return folder;

            var fileName = Path.GetFileNameWithoutExtension(trackFile);
            return string.IsNullOrWhiteSpace(fileName) ? LocalizationService.Mark("Custom track") : fileName;
        }

        private static string ResolveTrackId(TrackData trackData, string trackFile)
        {
            var candidate = string.Empty;
            if (trackData?.Metadata != null
                && trackData.Metadata.TryGetValue("id", out var metadataId))
            {
                candidate = metadataId ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(candidate))
                candidate = trackData?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(candidate))
                candidate = Path.GetFileNameWithoutExtension(trackFile);
            if (string.IsNullOrWhiteSpace(candidate))
                candidate = "custom-track";

            var normalized = NormalizeTrackIdentifier(candidate);
            if (string.IsNullOrWhiteSpace(normalized))
                normalized = "custom-track";
            if (normalized.Length > ProtocolConstants.MaxTrackIdLength)
                normalized = normalized.Substring(0, ProtocolConstants.MaxTrackIdLength);
            return normalized;
        }

        private static string ResolveTrackVersion(TrackData trackData)
        {
            var version = (trackData?.Version ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(version))
                version = "1.0";
            if (version.Length > ProtocolConstants.MaxTrackVersionLength)
                version = version.Substring(0, ProtocolConstants.MaxTrackVersionLength);
            return version;
        }

        private static string NormalizeTrackIdentifier(string value)
        {
            var input = (value ?? string.Empty).Trim();
            if (input.Length == 0)
                return string.Empty;

            var buffer = new char[input.Length];
            var length = 0;
            var previousDash = false;
            for (var i = 0; i < input.Length; i++)
            {
                var ch = char.ToLowerInvariant(input[i]);
                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_' || ch == '.')
                {
                    buffer[length++] = ch;
                    previousDash = false;
                    continue;
                }

                if (previousDash)
                    continue;

                buffer[length++] = '-';
                previousDash = true;
            }

            var normalized = new string(buffer, 0, length).Trim('-');
            return normalized;
        }

        private static string BuildTrackLoadError(IReadOnlyList<TrackTsmIssue> issues)
        {
            if (issues == null || issues.Count == 0)
                return LocalizationService.Mark("The selected custom track is invalid.");

            var first = issues[0].ToString();
            if (!string.IsNullOrWhiteSpace(first))
                return first;

            return LocalizationService.Mark("The selected custom track is invalid.");
        }
    }
}
