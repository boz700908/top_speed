using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private readonly TrackSource _roomTrackUploadSource = new TrackSource();
        private PendingTrackUpload? _pendingTrackUpload;
        private uint _nextTrackUploadId = 1;
        private bool _trackUploadProgressOpen;

        private sealed class PendingTrackUpload
        {
            public uint UploadId;
            public TrackPackageRef Track = TrackPackageRef.Custom(string.Empty, string.Empty, string.Empty);
            public string DisplayName = string.Empty;
            public byte[] Bytes = Array.Empty<byte>();
            public int Offset;
            public ushort NextChunkIndex;
            public bool BeginSent;
            public bool EndSent;
            public bool WaitingForResult;
            public bool CancelRequested;
        }

        private void ResetTrackUploadState()
        {
            _pendingTrackUpload = null;
            _trackUploadProgressOpen = false;
            _nextTrackUploadId = 1;
            _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
        }

        public void HandleTrackPackageUploadResult(PacketTrackPackageUploadResult result)
        {
            _roomsFlow.HandleTrackPackageUploadResult(result);
        }

        internal void HandleTrackPackageUploadResultCore(PacketTrackPackageUploadResult result)
        {
            if (result == null)
                return;

            var upload = _pendingTrackUpload;
            if (upload == null || result.UploadId != upload.UploadId)
            {
                if (!string.IsNullOrWhiteSpace(result.Message))
                    _speech.Speak(result.Message);
                return;
            }

            var wasCanceled = upload.CancelRequested;
            var shouldReopenCatalog = _state.RoomDrafts.RoomTrackUploadReturnToCatalog;
            _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
            _trackUploadProgressOpen = false;
            _pendingTrackUpload = null;
            _dialogs.CloseActive();

            if (wasCanceled)
            {
                _speech.Speak(LocalizationService.Mark("Track upload canceled."));
                return;
            }

            if (result.Status == TrackPackageUploadStatus.Accepted || result.Status == TrackPackageUploadStatus.Reused)
            {
                _speech.Speak(string.IsNullOrWhiteSpace(result.Message)
                    ? LocalizationService.Mark("Track package uploaded successfully.")
                    : result.Message);
                RequestRoomTrackCatalog(openOnResponse: shouldReopenCatalog);
                return;
            }

            _speech.Speak(string.IsNullOrWhiteSpace(result.Message)
                ? LocalizationService.Mark("Track package upload failed.")
                : result.Message);
        }

        public void HandleTrackPackageCatalog(PacketTrackPackageCatalog catalog)
        {
            _roomsFlow.HandleTrackPackageCatalog(catalog);
        }

        internal void HandleTrackPackageCatalogCore(PacketTrackPackageCatalog catalog)
        {
            var source = catalog?.Tracks ?? Array.Empty<PacketTrackPackageCatalogEntry>();
            var items = new List<PacketTrackPackageCatalogEntry>(source.Length);
            for (var i = 0; i < source.Length && items.Count < ProtocolConstants.MaxTrackPackageCatalogEntries; i++)
            {
                var item = source[i];
                if (!PacketValidation.IsValidTrackPackageCatalogEntry(item))
                    continue;

                items.Add(new PacketTrackPackageCatalogEntry
                {
                    Track = TrackPackageRef.Custom(item.Track.TrackId, item.Track.Version, item.Track.Hash),
                    DisplayName = item.DisplayName
                });
            }

            _state.RoomDrafts.RoomTrackCatalog = items.ToArray();
            RebuildRoomTrackCustomMenu();

            if (_state.RoomDrafts.RoomTrackCatalogOpenPending)
            {
                _state.RoomDrafts.RoomTrackCatalogOpenPending = false;
                _menu.Push(MultiplayerMenuKeys.RoomTrackCustom);
            }
        }

        private void OpenRoomTrackCustomMenu()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            if (!IsCurrentRoomCustomTracksEnabled())
            {
                _speech.Speak(LocalizationService.Mark("Custom tracks are disabled for this room."));
                return;
            }

            RequestRoomTrackCatalog(openOnResponse: true);
        }

        private void RequestRoomTrackCatalog(bool openOnResponse)
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            _state.RoomDrafts.RoomTrackCatalogOpenPending = openOnResponse;
            if (!TrySend(session.SendTrackPackageCatalogRequest(), LocalizationService.Mark("custom track list request")))
            {
                _state.RoomDrafts.RoomTrackCatalogOpenPending = false;
                return;
            }

            if (openOnResponse)
                _speech.Speak(LocalizationService.Mark("Loading custom tracks from server."));
        }

        private void RebuildRoomTrackCustomMenu()
        {
            var items = new List<MenuItem>();
            var tracks = _state.RoomDrafts.RoomTrackCatalog ?? Array.Empty<PacketTrackPackageCatalogEntry>();
            for (var i = 0; i < tracks.Length; i++)
            {
                var track = tracks[i];
                if (!PacketValidation.IsValidTrackPackageCatalogEntry(track))
                    continue;

                var display = string.IsNullOrWhiteSpace(track.DisplayName)
                    ? FormatTrackRefDisplay(track.Track)
                    : track.DisplayName;
                items.Add(new MenuItem(display, MenuAction.None, onActivate: () => SelectRoomTrack(track.Track, display, false)));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Upload a local track"), MenuAction.None, onActivate: OpenRoomTrackLocalCustomMenu));
            items.Add(new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, onActivate: SelectRandomRoomTrackAny));

            var preserveSelection = string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomTrackCustom, StringComparison.Ordinal);
            _menu.UpdateItems(MultiplayerMenuKeys.RoomTrackCustom, items, preserveSelection);
        }

        private void OpenRoomTrackLocalCustomMenu()
        {
            RebuildRoomTrackLocalCustomMenu();
            _menu.Push(MultiplayerMenuKeys.RoomTrackLocalCustom);
        }

        private void RebuildRoomTrackLocalCustomMenu()
        {
            var items = new List<MenuItem>();
            var tracks = _roomTrackUploadSource.GetInfo();
            if (tracks.Count == 0)
            {
                items.Add(new MenuItem(LocalizationService.Mark("No custom tracks found."), MenuAction.None));
            }
            else
            {
                for (var i = 0; i < tracks.Count; i++)
                {
                    var track = tracks[i];
                    items.Add(new MenuItem(track.Display, MenuAction.None, onActivate: () => StartLocalTrackUpload(track.Key, track.Display)));
                }
            }

            var preserveSelection = string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomTrackLocalCustom, StringComparison.Ordinal);
            _menu.UpdateItems(MultiplayerMenuKeys.RoomTrackLocalCustom, items, preserveSelection);
        }

        private void StartLocalTrackUpload(string trackFile, string displayName)
        {
            if (_pendingTrackUpload != null)
            {
                _speech.Speak(LocalizationService.Mark("A track upload is already in progress."));
                return;
            }

            if (!TryBuildTrackUpload(trackFile, displayName, out var pending, out var error))
            {
                _speech.Speak(string.IsNullOrWhiteSpace(error)
                    ? LocalizationService.Mark("Unable to prepare custom track upload.")
                    : error);
                return;
            }

            _pendingTrackUpload = pending;
            _trackUploadProgressOpen = true;
            _state.RoomDrafts.RoomTrackUploadReturnToCatalog = true;
            ShowTrackUploadProgressDialog();
        }

        private bool UpdateTrackPackageUploadOperation()
        {
            var upload = _pendingTrackUpload;
            if (upload == null)
                return false;

            if (_trackUploadProgressOpen)
                ShowTrackUploadProgressDialog();

            var session = SessionOrNull();
            if (session == null)
            {
                _trackUploadProgressOpen = false;
                _pendingTrackUpload = null;
                _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
                _speech.Speak(LocalizationService.Mark("Track upload canceled because the connection was lost."));
                return false;
            }

            if (!upload.BeginSent)
            {
                if (!TrySend(session.SendTrackPackageUploadBegin(new PacketTrackPackageUploadBegin
                    {
                        UploadId = upload.UploadId,
                        TrackId = upload.Track.TrackId,
                        Version = upload.Track.Version,
                        Hash = upload.Track.Hash,
                        TotalBytes = (uint)upload.Bytes.Length
                    }),
                    LocalizationService.Mark("track upload start")))
                {
                    _trackUploadProgressOpen = false;
                    _pendingTrackUpload = null;
                    _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
                    return false;
                }

                upload.BeginSent = true;
                return true;
            }

            if (upload.WaitingForResult)
                return true;

            if (upload.CancelRequested)
            {
                if (!upload.EndSent)
                {
                    if (!TrySend(session.SendTrackPackageUploadEnd(new PacketTrackPackageUploadEnd { UploadId = upload.UploadId }), LocalizationService.Mark("track upload cancel")))
                    {
                        _trackUploadProgressOpen = false;
                        _pendingTrackUpload = null;
                        _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
                        return false;
                    }

                    upload.EndSent = true;
                    upload.WaitingForResult = true;
                }

                return true;
            }

            var chunksSent = 0;
            while (upload.Offset < upload.Bytes.Length && chunksSent < 8)
            {
                var remaining = upload.Bytes.Length - upload.Offset;
                var length = Math.Min(ProtocolConstants.MaxTrackPackageChunkBytes, remaining);
                var chunkBytes = new byte[length];
                Buffer.BlockCopy(upload.Bytes, upload.Offset, chunkBytes, 0, length);

                if (!TrySend(session.SendTrackPackageUploadChunk(new PacketTrackPackageUploadChunk
                    {
                        UploadId = upload.UploadId,
                        ChunkIndex = upload.NextChunkIndex,
                        Data = chunkBytes
                    }),
                    LocalizationService.Mark("track upload chunk")))
                {
                    _trackUploadProgressOpen = false;
                    _pendingTrackUpload = null;
                    _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
                    return false;
                }

                upload.Offset += length;
                upload.NextChunkIndex++;
                chunksSent++;
            }

            if (upload.Offset >= upload.Bytes.Length && !upload.EndSent)
            {
                if (!TrySend(session.SendTrackPackageUploadEnd(new PacketTrackPackageUploadEnd { UploadId = upload.UploadId }), LocalizationService.Mark("track upload completion")))
                {
                    _trackUploadProgressOpen = false;
                    _pendingTrackUpload = null;
                    _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
                    return false;
                }

                upload.EndSent = true;
                upload.WaitingForResult = true;
            }

            return true;
        }

        private void ShowTrackUploadProgressDialog()
        {
            var upload = _pendingTrackUpload;
            if (upload == null)
                return;

            var total = upload.Bytes.Length;
            var uploaded = Math.Max(0, Math.Min(upload.Offset, total));
            var percent = total == 0 ? 0 : (int)Math.Round((double)uploaded * 100d / total, MidpointRounding.AwayFromZero);
            if (percent < 0)
                percent = 0;
            if (percent > 100)
                percent = 100;

            var items = new List<DialogItem>
            {
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("Track: {0}"), upload.DisplayName)),
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("File size: {0}"), FormatTransferBytes(total))),
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("Uploaded size: {0}"), FormatTransferBytes(uploaded))),
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("Percentage: {0}%"), percent))
            };

            var dialog = new Dialog(
                LocalizationService.Mark("Uploading track..."),
                null,
                QuestionId.Close,
                items,
                onResult: _ => RequestTrackUploadCancel(),
                new DialogButton(QuestionId.Close, LocalizationService.Mark("Cancel")));
            _dialogs.Show(dialog);
        }

        private void RequestTrackUploadCancel()
        {
            var upload = _pendingTrackUpload;
            if (upload == null)
                return;
            upload.CancelRequested = true;
        }

        private bool TryBuildTrackUpload(string trackFile, string displayName, out PendingTrackUpload pending, out string error)
        {
            pending = null!;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(trackFile))
            {
                error = LocalizationService.Mark("Custom track file path is empty.");
                return false;
            }

            if (!TrackTsmParser.TryLoadFromFile(trackFile, out var trackData, out var issues))
            {
                error = BuildTrackLoadError(issues);
                return false;
            }

            if (!TryBuildTrackPackagePayload(trackData, trackFile, out var payload, out error))
                return false;

            var hash = TrackPackageCodec.ComputeHash(payload);
            payload.Manifest.Hash = hash;
            if (!TrackPackageCodec.TryValidate(payload, out error))
                return false;

            var bytes = TrackPackageCodec.Serialize(payload);
            pending = new PendingTrackUpload
            {
                UploadId = NextTrackUploadId(),
                Track = TrackPackageRef.Custom(payload.Manifest.TrackId, payload.Manifest.Version, hash),
                DisplayName = ResolveTrackDisplayName(displayName, trackData, trackFile),
                Bytes = bytes,
                Offset = 0,
                NextChunkIndex = 0
            };
            return true;
        }

        private bool TryBuildTrackPackagePayload(TrackData trackData, string trackFile, out TrackPackagePayload payload, out string error)
        {
            payload = new TrackPackagePayload();
            error = string.Empty;

            if (trackData == null)
            {
                error = LocalizationService.Mark("Track data is missing.");
                return false;
            }

            if (!TryBuildTrackAssetBlobs(trackData, out var assets, out error))
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

            var laps = trackData.Laps > 0 ? trackData.Laps : _state.RoomDrafts.RoomOptionsLaps;
            if (laps == 0)
                laps = 3;

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

        private static bool TryBuildTrackAssetBlobs(TrackData trackData, out IReadOnlyDictionary<string, byte[]> assets, out string error)
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

                if (!TryAddTrackAsset(trackRoot!, sound.Path, map, out error))
                    return false;

                var variants = sound.VariantPaths ?? Array.Empty<string>();
                for (var i = 0; i < variants.Count; i++)
                {
                    if (!TryAddTrackAsset(trackRoot!, variants[i], map, out error))
                        return false;
                }
            }

            assets = map;
            return true;
        }

        private static bool TryAddTrackAsset(string trackRoot, string? relativeAssetPath, Dictionary<string, byte[]> map, out string error)
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

        private uint NextTrackUploadId()
        {
            var id = _nextTrackUploadId++;
            if (id == 0)
            {
                id = _nextTrackUploadId++;
            }

            if (_nextTrackUploadId == 0)
                _nextTrackUploadId = 1;
            return id;
        }

        private static string FormatTransferBytes(long bytes)
        {
            if (bytes < 0)
                bytes = 0;

            var units = new[]
            {
                LocalizationService.Mark("B"),
                LocalizationService.Mark("KB"),
                LocalizationService.Mark("MB"),
                LocalizationService.Mark("GB")
            };
            var index = 0;
            var value = (double)bytes;
            while (value >= 1024d && index < units.Length - 1)
            {
                value /= 1024d;
                index++;
            }

            return value.ToString("0.##", CultureInfo.InvariantCulture) + " " + LocalizationService.Translate(units[index]);
        }
    }
}
