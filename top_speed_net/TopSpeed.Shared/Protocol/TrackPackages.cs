using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using TopSpeed.Data;

namespace TopSpeed.Protocol
{
    public enum RoomTrackSelectionKind : byte
    {
        None = 0,
        BuiltIn = 1,
        CustomPackage = 2
    }

    public sealed class TrackPackageRef
    {
        public RoomTrackSelectionKind Kind = RoomTrackSelectionKind.None;
        public string BuiltInTrackKey = string.Empty;
        public string TrackId = string.Empty;
        public string Version = string.Empty;
        public string Hash = string.Empty;

        public bool IsBuiltIn => Kind == RoomTrackSelectionKind.BuiltIn;
        public bool IsCustomPackage => Kind == RoomTrackSelectionKind.CustomPackage;

        public static TrackPackageRef BuiltIn(string trackKey)
        {
            return new TrackPackageRef
            {
                Kind = RoomTrackSelectionKind.BuiltIn,
                BuiltInTrackKey = (trackKey ?? string.Empty).Trim()
            };
        }

        public static TrackPackageRef Custom(string trackId, string version, string hash)
        {
            return new TrackPackageRef
            {
                Kind = RoomTrackSelectionKind.CustomPackage,
                TrackId = (trackId ?? string.Empty).Trim(),
                Version = (version ?? string.Empty).Trim(),
                Hash = NormalizeHash(hash)
            };
        }

        public static string NormalizeHash(string hash)
        {
            return (hash ?? string.Empty).Trim().ToLowerInvariant();
        }
    }

    public sealed class TrackPackageManifest
    {
        public string TrackId = string.Empty;
        public string Version = string.Empty;
        public string Hash = string.Empty;
        public string DefaultWeatherProfileId = TrackWeatherProfile.DefaultProfileId;
        public TrackAmbience Ambience;
        public byte Laps;
    }

    public sealed class TrackPackagePayload
    {
        public TrackPackageManifest Manifest = new TrackPackageManifest();
        public TrackDefinition[] Definitions = Array.Empty<TrackDefinition>();
        public IReadOnlyDictionary<string, string> Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, TrackRoomDefinition> RoomProfiles = new Dictionary<string, TrackRoomDefinition>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, TrackWeatherProfile> WeatherProfiles = new Dictionary<string, TrackWeatherProfile>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, TrackSoundSourceDefinition> SoundDefinitions = new Dictionary<string, TrackSoundSourceDefinition>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, byte[]> AssetBlobs = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
    }

    public static class TrackPackageCodec
    {
        private const byte FormatVersion = 2;

        public static byte[] Serialize(TrackPackagePayload payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                WritePayload(writer, payload, includeHash: true);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public static bool TryDeserialize(byte[] bytes, out TrackPackagePayload payload, out string error)
        {
            payload = new TrackPackagePayload();
            error = string.Empty;

            if (bytes == null || bytes.Length == 0)
            {
                error = "Track package payload is empty.";
                return false;
            }

            try
            {
                using (var ms = new MemoryStream(bytes, writable: false))
                using (var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true))
                {
                    payload = ReadPayload(reader);
                    if (ms.Position != ms.Length)
                    {
                        error = "Track package payload contains trailing bytes.";
                        return false;
                    }
                }
            }
            catch (Exception ex) when (ex is EndOfStreamException || ex is IOException || ex is InvalidDataException || ex is ArgumentException)
            {
                error = ex.Message;
                payload = new TrackPackagePayload();
                return false;
            }

            return TryValidate(payload, out error);
        }

        public static string ComputeHash(TrackPackagePayload payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                WritePayload(writer, payload, includeHash: false);
                writer.Flush();
                return ComputeHash(ms.ToArray());
            }
        }

        public static string ComputeHash(byte[] canonicalBytes)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(canonicalBytes ?? Array.Empty<byte>());
                var builder = new StringBuilder(hash.Length * 2);
                for (var i = 0; i < hash.Length; i++)
                    builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        public static TrackData ToTrackData(TrackPackagePayload payload, bool userDefined, string sourcePath)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            var manifest = payload.Manifest ?? new TrackPackageManifest();
            var name = string.IsNullOrWhiteSpace(manifest.TrackId) ? "custom" : manifest.TrackId;
            return new TrackData(
                userDefined,
                string.IsNullOrWhiteSpace(manifest.DefaultWeatherProfileId) ? TrackWeatherProfile.DefaultProfileId : manifest.DefaultWeatherProfileId,
                payload.WeatherProfiles,
                manifest.Ambience,
                payload.Definitions,
                manifest.Laps,
                name,
                manifest.Version,
                metadata: payload.Metadata,
                roomProfiles: payload.RoomProfiles,
                soundSources: payload.SoundDefinitions,
                sourcePath: sourcePath);
        }

        public static bool TryValidate(TrackPackagePayload payload, out string error)
        {
            error = string.Empty;
            if (payload == null || payload.Manifest == null)
            {
                error = "Track package payload is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(payload.Manifest.TrackId))
            {
                error = "Track package track id is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(payload.Manifest.Version))
            {
                error = "Track package version is required.";
                return false;
            }

            if (payload.Definitions == null || payload.Definitions.Length == 0)
            {
                error = "Track package definitions are required.";
                return false;
            }

            if (payload.Definitions.Length > ProtocolConstants.MaxMultiTrackLength)
            {
                error = "Track package exceeds maximum definition count.";
                return false;
            }

            var weatherProfiles = payload.WeatherProfiles ?? new Dictionary<string, TrackWeatherProfile>(StringComparer.OrdinalIgnoreCase);
            var metadata = payload.Metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var roomProfiles = payload.RoomProfiles ?? new Dictionary<string, TrackRoomDefinition>(StringComparer.OrdinalIgnoreCase);
            var defaultWeather = string.IsNullOrWhiteSpace(payload.Manifest.DefaultWeatherProfileId)
                ? TrackWeatherProfile.DefaultProfileId
                : payload.Manifest.DefaultWeatherProfileId.Trim();
            if (!weatherProfiles.ContainsKey(defaultWeather))
            {
                error = "Track package default weather profile is missing.";
                return false;
            }

            foreach (var pair in metadata)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    error = "Track package contains an invalid metadata key.";
                    return false;
                }
            }

            foreach (var pair in roomProfiles)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    error = "Track package contains an invalid room profile id.";
                    return false;
                }

                var room = pair.Value;
                if (room == null)
                {
                    error = string.Format(CultureInfo.InvariantCulture, "Track package room profile '{0}' is invalid.", pair.Key);
                    return false;
                }
            }

            var assets = payload.AssetBlobs ?? new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            var sounds = payload.SoundDefinitions ?? new Dictionary<string, TrackSoundSourceDefinition>(StringComparer.OrdinalIgnoreCase);
            var definitions = payload.Definitions ?? Array.Empty<TrackDefinition>();
            var normalizedAssetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            long totalAssets = 0;
            foreach (var pair in assets)
            {
                var key = NormalizeAssetKey(pair.Key ?? string.Empty);
                if (string.IsNullOrWhiteSpace(key))
                {
                    error = "Track package contains an invalid asset blob key.";
                    return false;
                }
                normalizedAssetKeys.Add(key);

                var bytes = pair.Value ?? Array.Empty<byte>();
                if (bytes.Length > ProtocolConstants.MaxTrackPackageAssetBytes)
                {
                    error = string.Format(CultureInfo.InvariantCulture, "Asset '{0}' exceeds max size.", key);
                    return false;
                }

                totalAssets += bytes.Length;
                if (totalAssets > ProtocolConstants.MaxTrackPackageBytes)
                {
                    error = "Track package exceeds max size.";
                    return false;
                }
            }

            foreach (var pair in sounds)
            {
                var sound = pair.Value;
                if (sound == null)
                {
                    error = "Track package contains an invalid sound definition.";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(sound.Path) && !normalizedAssetKeys.Contains(NormalizeAssetKey(sound.Path ?? string.Empty)))
                {
                    error = string.Format(CultureInfo.InvariantCulture, "Sound '{0}' references missing asset path '{1}'.", pair.Key, sound.Path);
                    return false;
                }

                for (var i = 0; i < sound.VariantPaths.Count; i++)
                {
                    if (!normalizedAssetKeys.Contains(NormalizeAssetKey(sound.VariantPaths[i] ?? string.Empty)))
                    {
                        error = string.Format(CultureInfo.InvariantCulture, "Sound '{0}' references missing variant asset path '{1}'.", pair.Key, sound.VariantPaths[i]);
                        return false;
                    }
                }
            }

            for (var i = 0; i < definitions.Length; i++)
            {
                var definition = definitions[i];
                if (!string.IsNullOrWhiteSpace(definition.WeatherProfileId)
                    && !weatherProfiles.ContainsKey(definition.WeatherProfileId!))
                {
                    error = string.Format(CultureInfo.InvariantCulture, "Track definition {0} references missing weather profile '{1}'.", i, definition.WeatherProfileId);
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(definition.RoomId)
                    && !roomProfiles.ContainsKey(definition.RoomId!)
                    && !TrackRoomLibrary.TryGetPreset(definition.RoomId!, out _))
                {
                    error = string.Format(CultureInfo.InvariantCulture, "Track definition {0} references missing room profile '{1}'.", i, definition.RoomId);
                    return false;
                }

                for (var sourceIndex = 0; sourceIndex < definition.SoundSourceIds.Count; sourceIndex++)
                {
                    var soundSourceId = definition.SoundSourceIds[sourceIndex];
                    if (string.IsNullOrWhiteSpace(soundSourceId))
                        continue;
                    if (!sounds.ContainsKey(soundSourceId))
                    {
                        error = string.Format(CultureInfo.InvariantCulture, "Track definition {0} references missing sound source '{1}'.", i, soundSourceId);
                        return false;
                    }
                }
            }

            return true;
        }

        public static string NormalizeAssetKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            var normalized = key.Trim().Replace('\\', '/').TrimStart('/');
            if (normalized.Length == 0 || normalized.IndexOf(':') >= 0)
                return string.Empty;

            var segments = normalized.Split('/');
            for (var i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "." || segments[i] == ".." || segments[i].Length == 0)
                    return string.Empty;
            }

            return normalized;
        }

        private static void WritePayload(BinaryWriter writer, TrackPackagePayload payload, bool includeHash)
        {
            var manifest = payload.Manifest ?? new TrackPackageManifest();
            writer.Write(FormatVersion);
            writer.Write(manifest.TrackId ?? string.Empty);
            writer.Write(manifest.Version ?? string.Empty);
            writer.Write(includeHash ? TrackPackageRef.NormalizeHash(manifest.Hash) : string.Empty);
            writer.Write(manifest.DefaultWeatherProfileId ?? string.Empty);
            writer.Write((byte)manifest.Ambience);
            writer.Write(manifest.Laps);

            WriteMetadata(writer, payload.Metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            WriteRoomProfiles(writer, payload.RoomProfiles ?? new Dictionary<string, TrackRoomDefinition>(StringComparer.OrdinalIgnoreCase));

            var definitions = payload.Definitions ?? Array.Empty<TrackDefinition>();
            writer.Write(definitions.Length);
            for (var i = 0; i < definitions.Length; i++)
                WriteDefinition(writer, definitions[i]);

            WriteWeatherProfiles(writer, payload.WeatherProfiles ?? new Dictionary<string, TrackWeatherProfile>(StringComparer.OrdinalIgnoreCase));
            WriteSoundDefinitions(writer, payload.SoundDefinitions ?? new Dictionary<string, TrackSoundSourceDefinition>(StringComparer.OrdinalIgnoreCase));
            WriteAssets(writer, payload.AssetBlobs ?? new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase));
        }

        private static TrackPackagePayload ReadPayload(BinaryReader reader)
        {
            var format = reader.ReadByte();
            if (format != FormatVersion)
                throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "Unsupported track package payload format '{0}'.", format));

            var payload = new TrackPackagePayload();
            payload.Manifest = new TrackPackageManifest
            {
                TrackId = reader.ReadString(),
                Version = reader.ReadString(),
                Hash = TrackPackageRef.NormalizeHash(reader.ReadString()),
                DefaultWeatherProfileId = reader.ReadString(),
                Ambience = (TrackAmbience)reader.ReadByte(),
                Laps = reader.ReadByte()
            };

            payload.Metadata = ReadMetadata(reader);
            payload.RoomProfiles = ReadRoomProfiles(reader);

            var definitionCount = reader.ReadInt32();
            if (definitionCount < 0)
                throw new InvalidDataException("Invalid track definition count.");

            var definitions = new TrackDefinition[definitionCount];
            for (var i = 0; i < definitionCount; i++)
                definitions[i] = ReadDefinition(reader);

            payload.Definitions = definitions;
            payload.WeatherProfiles = ReadWeatherProfiles(reader);
            payload.SoundDefinitions = ReadSoundDefinitions(reader);
            payload.AssetBlobs = ReadAssets(reader);
            return payload;
        }

        private static void WriteDefinition(BinaryWriter writer, TrackDefinition definition)
        {
            writer.Write((byte)definition.Type);
            writer.Write((byte)definition.Surface);
            writer.Write((byte)definition.Noise);
            writer.Write(definition.Length);
            writer.Write(definition.SegmentId ?? string.Empty);
            writer.Write(definition.Width);
            writer.Write(definition.Height);
            writer.Write(definition.WeatherProfileId ?? string.Empty);
            writer.Write(definition.WeatherTransitionSeconds);
            writer.Write(definition.RoomId ?? string.Empty);
            WriteRoomOverrides(writer, definition.RoomOverrides);
            WriteStringList(writer, definition.SoundSourceIds ?? Array.Empty<string>());
            WriteMetadata(writer, definition.Metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        private static TrackDefinition ReadDefinition(BinaryReader reader)
        {
            var type = (TrackType)reader.ReadByte();
            var surface = (TrackSurface)reader.ReadByte();
            var noise = (TrackNoise)reader.ReadByte();
            var length = reader.ReadSingle();
            var segmentId = NormalizeNull(reader.ReadString());
            var width = reader.ReadSingle();
            var height = reader.ReadSingle();
            var weatherProfileId = NormalizeNull(reader.ReadString());
            var weatherTransitionSeconds = reader.ReadSingle();
            var roomId = NormalizeNull(reader.ReadString());
            var roomOverrides = ReadRoomOverrides(reader);
            var soundSourceIds = ReadStringList(reader);
            var metadata = ReadMetadata(reader);
            return new TrackDefinition(
                type,
                surface,
                noise,
                length,
                segmentId,
                width,
                height,
                weatherProfileId,
                weatherTransitionSeconds,
                roomId,
                roomOverrides,
                soundSourceIds,
                metadata);
        }

        private static void WriteRoomProfiles(BinaryWriter writer, IReadOnlyDictionary<string, TrackRoomDefinition> rooms)
        {
            var ordered = rooms.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
            writer.Write(ordered.Length);
            for (var i = 0; i < ordered.Length; i++)
            {
                var room = ordered[i].Value;
                writer.Write(ordered[i].Key ?? string.Empty);
                writer.Write(room.Name ?? string.Empty);
                writer.Write(room.ReverbTimeSeconds);
                writer.Write(room.ReverbGain);
                writer.Write(room.HfDecayRatio);
                writer.Write(room.LateReverbGain);
                writer.Write(room.Diffusion);
                writer.Write(room.AirAbsorption);
                writer.Write(room.OcclusionScale);
                writer.Write(room.TransmissionScale);
                WriteNullableSingle(writer, room.OcclusionOverride);
                WriteNullableSingle(writer, room.TransmissionOverrideLow);
                WriteNullableSingle(writer, room.TransmissionOverrideMid);
                WriteNullableSingle(writer, room.TransmissionOverrideHigh);
                WriteNullableSingle(writer, room.AirAbsorptionOverrideLow);
                WriteNullableSingle(writer, room.AirAbsorptionOverrideMid);
                WriteNullableSingle(writer, room.AirAbsorptionOverrideHigh);
            }
        }

        private static IReadOnlyDictionary<string, TrackRoomDefinition> ReadRoomProfiles(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            if (count < 0)
                throw new InvalidDataException("Invalid room profile count.");

            var map = new Dictionary<string, TrackRoomDefinition>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < count; i++)
            {
                var id = reader.ReadString();
                if (string.IsNullOrWhiteSpace(id))
                    throw new InvalidDataException("Invalid room profile id.");
                map[id] = new TrackRoomDefinition(
                    id,
                    NormalizeNull(reader.ReadString()),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    ReadNullableSingle(reader),
                    ReadNullableSingle(reader),
                    ReadNullableSingle(reader),
                    ReadNullableSingle(reader),
                    ReadNullableSingle(reader),
                    ReadNullableSingle(reader),
                    ReadNullableSingle(reader));
            }

            return map;
        }

        private static void WriteRoomOverrides(BinaryWriter writer, TrackRoomOverrides? overrides)
        {
            writer.Write(overrides != null);
            if (overrides == null)
                return;

            WriteNullableSingle(writer, overrides.ReverbTimeSeconds);
            WriteNullableSingle(writer, overrides.ReverbGain);
            WriteNullableSingle(writer, overrides.HfDecayRatio);
            WriteNullableSingle(writer, overrides.LateReverbGain);
            WriteNullableSingle(writer, overrides.Diffusion);
            WriteNullableSingle(writer, overrides.AirAbsorption);
            WriteNullableSingle(writer, overrides.OcclusionScale);
            WriteNullableSingle(writer, overrides.TransmissionScale);
            WriteNullableSingle(writer, overrides.OcclusionOverride);
            WriteNullableSingle(writer, overrides.TransmissionOverrideLow);
            WriteNullableSingle(writer, overrides.TransmissionOverrideMid);
            WriteNullableSingle(writer, overrides.TransmissionOverrideHigh);
            WriteNullableSingle(writer, overrides.AirAbsorptionOverrideLow);
            WriteNullableSingle(writer, overrides.AirAbsorptionOverrideMid);
            WriteNullableSingle(writer, overrides.AirAbsorptionOverrideHigh);
        }

        private static TrackRoomOverrides? ReadRoomOverrides(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return null;

            var overrides = new TrackRoomOverrides
            {
                ReverbTimeSeconds = ReadNullableSingle(reader),
                ReverbGain = ReadNullableSingle(reader),
                HfDecayRatio = ReadNullableSingle(reader),
                LateReverbGain = ReadNullableSingle(reader),
                Diffusion = ReadNullableSingle(reader),
                AirAbsorption = ReadNullableSingle(reader),
                OcclusionScale = ReadNullableSingle(reader),
                TransmissionScale = ReadNullableSingle(reader),
                OcclusionOverride = ReadNullableSingle(reader),
                TransmissionOverrideLow = ReadNullableSingle(reader),
                TransmissionOverrideMid = ReadNullableSingle(reader),
                TransmissionOverrideHigh = ReadNullableSingle(reader),
                AirAbsorptionOverrideLow = ReadNullableSingle(reader),
                AirAbsorptionOverrideMid = ReadNullableSingle(reader),
                AirAbsorptionOverrideHigh = ReadNullableSingle(reader)
            };
            return overrides.HasAny ? overrides : null;
        }

        private static void WriteStringList(BinaryWriter writer, IReadOnlyList<string> values)
        {
            writer.Write(values.Count);
            for (var i = 0; i < values.Count; i++)
                writer.Write(values[i] ?? string.Empty);
        }

        private static string[] ReadStringList(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            if (count < 0)
                throw new InvalidDataException("Invalid string list count.");

            var values = new string[count];
            for (var i = 0; i < count; i++)
                values[i] = reader.ReadString();
            return values;
        }

        private static void WriteMetadata(BinaryWriter writer, IReadOnlyDictionary<string, string> metadata)
        {
            var ordered = metadata.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
            writer.Write(ordered.Length);
            for (var i = 0; i < ordered.Length; i++)
            {
                writer.Write(ordered[i].Key ?? string.Empty);
                writer.Write(ordered[i].Value ?? string.Empty);
            }
        }

        private static IReadOnlyDictionary<string, string> ReadMetadata(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            if (count < 0)
                throw new InvalidDataException("Invalid metadata count.");

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var value = reader.ReadString();
                if (!string.IsNullOrWhiteSpace(key))
                    map[key] = value ?? string.Empty;
            }

            return map;
        }

        private static void WriteWeatherProfiles(BinaryWriter writer, IReadOnlyDictionary<string, TrackWeatherProfile> profiles)
        {
            var ordered = profiles.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
            writer.Write(ordered.Length);
            for (var i = 0; i < ordered.Length; i++)
            {
                var profile = ordered[i].Value;
                writer.Write(profile.Id ?? string.Empty);
                writer.Write((byte)profile.Kind);
                writer.Write(profile.LongitudinalWindMps);
                writer.Write(profile.LateralWindMps);
                writer.Write(profile.AirDensityKgPerM3);
                writer.Write(profile.DraftingFactor);
                writer.Write(profile.TemperatureC);
                writer.Write(profile.Humidity);
                writer.Write(profile.PressureKpa);
                writer.Write(profile.VisibilityM);
                writer.Write(profile.RainGain);
                writer.Write(profile.WindGain);
                writer.Write(profile.StormGain);
            }
        }

        private static IReadOnlyDictionary<string, TrackWeatherProfile> ReadWeatherProfiles(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            if (count < 0)
                throw new InvalidDataException("Invalid weather profile count.");

            var map = new Dictionary<string, TrackWeatherProfile>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < count; i++)
            {
                var id = reader.ReadString();
                map[id] = new TrackWeatherProfile(
                    id,
                    (TrackWeather)reader.ReadByte(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle());
            }

            return map;
        }

        private static void WriteSoundDefinitions(BinaryWriter writer, IReadOnlyDictionary<string, TrackSoundSourceDefinition> sounds)
        {
            var ordered = sounds.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
            writer.Write(ordered.Length);
            for (var i = 0; i < ordered.Length; i++)
            {
                var sound = ordered[i].Value;
                writer.Write(sound.Id ?? string.Empty);
                writer.Write((byte)sound.Type);
                writer.Write(sound.Path ?? string.Empty);
                writer.Write(sound.VariantPaths.Count);
                for (var variantIndex = 0; variantIndex < sound.VariantPaths.Count; variantIndex++)
                    writer.Write(sound.VariantPaths[variantIndex] ?? string.Empty);
                writer.Write(sound.VariantSourceIds.Count);
                for (var sourceIndex = 0; sourceIndex < sound.VariantSourceIds.Count; sourceIndex++)
                    writer.Write(sound.VariantSourceIds[sourceIndex] ?? string.Empty);
                writer.Write((byte)sound.RandomMode);
                writer.Write(sound.Loop);
                writer.Write(sound.Volume);
                writer.Write(sound.Spatial);
                writer.Write(sound.AllowHrtf);
                writer.Write(sound.FadeInSeconds);
                writer.Write(sound.FadeOutSeconds);
                writer.Write(sound.CrossfadeSeconds.HasValue);
                if (sound.CrossfadeSeconds.HasValue)
                    writer.Write(sound.CrossfadeSeconds.Value);
                writer.Write(sound.Pitch);
                writer.Write(sound.Pan);
                WriteNullableSingle(writer, sound.MinDistance);
                WriteNullableSingle(writer, sound.MaxDistance);
                WriteNullableSingle(writer, sound.Rolloff);
                writer.Write(sound.Global);
                writer.Write(sound.StartAreaId ?? string.Empty);
                writer.Write(sound.EndAreaId ?? string.Empty);
                WriteNullableVector3(writer, sound.StartPosition);
                WriteNullableSingle(writer, sound.StartRadiusMeters);
                WriteNullableVector3(writer, sound.EndPosition);
                WriteNullableSingle(writer, sound.EndRadiusMeters);
                WriteNullableVector3(writer, sound.Position);
                WriteNullableSingle(writer, sound.SpeedMetersPerSecond);
            }
        }

        private static IReadOnlyDictionary<string, TrackSoundSourceDefinition> ReadSoundDefinitions(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            if (count < 0)
                throw new InvalidDataException("Invalid sound definition count.");

            var map = new Dictionary<string, TrackSoundSourceDefinition>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < count; i++)
            {
                var id = reader.ReadString();
                var type = (TrackSoundSourceType)reader.ReadByte();
                var path = reader.ReadString();

                var variantPathCount = reader.ReadInt32();
                if (variantPathCount < 0)
                    throw new InvalidDataException("Invalid variant path count.");
                var variantPaths = new string[variantPathCount];
                for (var variantIndex = 0; variantIndex < variantPathCount; variantIndex++)
                    variantPaths[variantIndex] = reader.ReadString();

                var variantSourceCount = reader.ReadInt32();
                if (variantSourceCount < 0)
                    throw new InvalidDataException("Invalid variant source count.");
                var variantSourceIds = new string[variantSourceCount];
                for (var sourceIndex = 0; sourceIndex < variantSourceCount; sourceIndex++)
                    variantSourceIds[sourceIndex] = reader.ReadString();

                var randomMode = (TrackSoundRandomMode)reader.ReadByte();
                var loop = reader.ReadBoolean();
                var volume = reader.ReadSingle();
                var spatial = reader.ReadBoolean();
                var allowHrtf = reader.ReadBoolean();
                var fadeInSeconds = reader.ReadSingle();
                var fadeOutSeconds = reader.ReadSingle();
                var hasCrossfade = reader.ReadBoolean();
                float? crossfadeSeconds = null;
                if (hasCrossfade)
                    crossfadeSeconds = reader.ReadSingle();
                var pitch = reader.ReadSingle();
                var pan = reader.ReadSingle();
                var minDistance = ReadNullableSingle(reader);
                var maxDistance = ReadNullableSingle(reader);
                var rolloff = ReadNullableSingle(reader);
                var global = reader.ReadBoolean();
                var startAreaId = NormalizeNull(reader.ReadString());
                var endAreaId = NormalizeNull(reader.ReadString());
                var startPosition = ReadNullableVector3(reader);
                var startRadius = ReadNullableSingle(reader);
                var endPosition = ReadNullableVector3(reader);
                var endRadius = ReadNullableSingle(reader);
                var position = ReadNullableVector3(reader);
                var speedMetersPerSecond = ReadNullableSingle(reader);

                map[id] = new TrackSoundSourceDefinition(
                    id,
                    type,
                    string.IsNullOrWhiteSpace(path) ? null : path,
                    variantPaths,
                    variantSourceIds,
                    randomMode,
                    loop,
                    volume,
                    spatial,
                    allowHrtf,
                    fadeInSeconds,
                    fadeOutSeconds,
                    crossfadeSeconds,
                    pitch,
                    pan,
                    minDistance,
                    maxDistance,
                    rolloff,
                    global,
                    startAreaId,
                    endAreaId,
                    startPosition,
                    startRadius,
                    endPosition,
                    endRadius,
                    position,
                    speedMetersPerSecond);
            }

            return map;
        }

        private static void WriteAssets(BinaryWriter writer, IReadOnlyDictionary<string, byte[]> assets)
        {
            var ordered = assets.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
            writer.Write(ordered.Length);
            for (var i = 0; i < ordered.Length; i++)
            {
                writer.Write(NormalizeAssetKey(ordered[i].Key));
                var data = ordered[i].Value ?? Array.Empty<byte>();
                writer.Write(data.Length);
                writer.Write(data);
            }
        }

        private static IReadOnlyDictionary<string, byte[]> ReadAssets(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            if (count < 0)
                throw new InvalidDataException("Invalid asset count.");

            var map = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < count; i++)
            {
                var key = NormalizeAssetKey(reader.ReadString());
                if (string.IsNullOrWhiteSpace(key))
                    throw new InvalidDataException("Invalid asset key.");
                var length = reader.ReadInt32();
                if (length < 0)
                    throw new InvalidDataException("Invalid asset blob length.");
                var data = reader.ReadBytes(length);
                if (data.Length != length)
                    throw new EndOfStreamException("Unexpected end of asset blob.");
                map[key] = data;
            }

            return map;
        }

        private static void WriteNullableSingle(BinaryWriter writer, float? value)
        {
            writer.Write(value.HasValue);
            if (value.HasValue)
                writer.Write(value.Value);
        }

        private static float? ReadNullableSingle(BinaryReader reader)
        {
            return reader.ReadBoolean() ? reader.ReadSingle() : (float?)null;
        }

        private static void WriteNullableVector3(BinaryWriter writer, Vector3? value)
        {
            writer.Write(value.HasValue);
            if (!value.HasValue)
                return;
            writer.Write(value.Value.X);
            writer.Write(value.Value.Y);
            writer.Write(value.Value.Z);
        }

        private static Vector3? ReadNullableVector3(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return null;
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        private static string? NormalizeNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
