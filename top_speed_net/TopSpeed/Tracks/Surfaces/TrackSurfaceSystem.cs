using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using TopSpeed.Tracks.Areas;
using TopSpeed.Tracks.Map;
using TopSpeed.Tracks.Geometry;

namespace TopSpeed.Tracks.Surfaces
{
    internal sealed class TrackSurfaceSystem
    {
        private readonly List<TrackSurfaceMesh> _surfaces;
        private readonly SurfaceCellIndex _surfaceIndex;

        public TrackSurfaceSystem(TrackMap map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            _surfaces = new List<TrackSurfaceMesh>();
            _surfaceIndex = new SurfaceCellIndex(Math.Max(0.5f, map.SurfaceResolutionMeters));

            var geometriesById = BuildGeometryLookup(map.Geometries);
            var profilesById = BuildProfileLookup(map.Profiles);
            var banksById = BuildBankLookup(map.Banks);
            var surfaceDefaults = BuildSurfaceDefaults(map.Areas, map.DefaultWidthMeters);
            var surfacesToBuild = BuildSurfaceWorklist(map.Surfaces, map.Areas, geometriesById);

            var defaultResolution = Math.Max(0.25f, map.SurfaceResolutionMeters);
            foreach (var surface in surfacesToBuild)
            {
                if (surface == null)
                    continue;
                if (string.IsNullOrWhiteSpace(surface.GeometryId))
                    continue;
                if (!geometriesById.TryGetValue(surface.GeometryId!, out var geometry))
                    continue;

                profilesById.TryGetValue(surface.ProfileId ?? string.Empty, out var profile);
                banksById.TryGetValue(surface.BankId ?? string.Empty, out var bank);

                var baseHeight = map.BaseHeightMeters;
                SurfaceDefaults defaults;
                if (SurfaceParameterParser.TryGetFloat(surface.Metadata, out var baseValue, "base_height", "elevation", "height", "y"))
                    baseHeight = baseValue;
                else if (surfaceDefaults.TryGetValue(surface.Id, out defaults) && defaults.BaseHeight.HasValue)
                    baseHeight = defaults.BaseHeight.Value;

                var defaultWidth = map.DefaultWidthMeters;
                if (surfaceDefaults.TryGetValue(surface.Id, out defaults) && defaults.Width.HasValue)
                    defaultWidth = defaults.Width.Value;

                var surfaceResolution = surface.ResolutionMeters ??
                                        (surfaceDefaults.TryGetValue(surface.Id, out defaults) && defaults.Resolution.HasValue
                                            ? defaults.Resolution.Value
                                            : defaultResolution);

                var profileEvaluator = SurfaceProfileEvaluator.Create(profile, surfaceResolution);
                var bankEvaluator = SurfaceBankEvaluator.Create(bank);

                var mesh = SurfaceMeshBuilder.BuildSurface(
                    surface,
                    geometry,
                    profileEvaluator,
                    bankEvaluator,
                    baseHeight,
                    defaultWidth,
                    surfaceResolution);

                if (mesh == null)
                    continue;

                _surfaces.Add(mesh);
                _surfaceIndex.AddSurface(_surfaces.Count - 1, mesh.Bounds);
            }
        }

        public IReadOnlyList<TrackSurfaceMesh> Surfaces => _surfaces;

        public bool TrySample(Vector3 position, out TrackSurfaceSample sample, TrackSurfaceQueryOptions? options = null)
        {
            sample = default;
            if (_surfaces.Count == 0)
                return false;

            if (!_surfaceIndex.TryGetSurfaces(position.X, position.Z, out var surfaceIndices))
                return false;

            var found = false;
            TrackSurfaceSample best = default;
            var minLayer = options?.MinLayer;
            var maxLayer = options?.MaxLayer;
            var preferLayer = options?.PreferHighestLayer ?? true;
            var preferHeight = options?.PreferHighestHeight ?? true;
            var preferClosestHeight = options?.PreferClosestHeightToReference ?? false;
            var referenceHeight = options?.ReferenceHeightMeters ?? position.Y;
            var maxHeightDelta = options?.MaxHeightDeltaMeters;

            foreach (var index in surfaceIndices)
            {
                if ((uint)index >= (uint)_surfaces.Count)
                    continue;
                var surface = _surfaces[index];
                if (minLayer.HasValue && surface.Layer < minLayer.Value)
                    continue;
                if (maxLayer.HasValue && surface.Layer > maxLayer.Value)
                    continue;
                if (!surface.TrySample(position.X, position.Z, out var hit))
                    continue;
                if (maxHeightDelta.HasValue &&
                    Math.Abs(hit.Position.Y - referenceHeight) > maxHeightDelta.Value)
                {
                    continue;
                }

                if (!found || IsBetter(hit, best, preferLayer, preferHeight, preferClosestHeight, referenceHeight))
                {
                    found = true;
                    best = hit;
                }
            }

            if (!found)
                return false;

            sample = best;
            return true;
        }

        private static bool IsBetter(
            TrackSurfaceSample candidate,
            TrackSurfaceSample current,
            bool preferLayer,
            bool preferHeight,
            bool preferClosestHeight,
            float referenceHeight)
        {
            if (preferLayer && candidate.Layer != current.Layer)
                return candidate.Layer > current.Layer;

            if (preferClosestHeight)
            {
                var candidateDelta = Math.Abs(candidate.Position.Y - referenceHeight);
                var currentDelta = Math.Abs(current.Position.Y - referenceHeight);
                if (Math.Abs(candidateDelta - currentDelta) > 0.0001f)
                    return candidateDelta < currentDelta;
            }

            if (preferHeight)
                return candidate.Position.Y > current.Position.Y;
            return false;
        }

        private static Dictionary<string, GeometryDefinition> BuildGeometryLookup(IEnumerable<GeometryDefinition> geometries)
        {
            var lookup = new Dictionary<string, GeometryDefinition>(StringComparer.OrdinalIgnoreCase);
            if (geometries == null)
                return lookup;
            foreach (var geometry in geometries)
            {
                if (geometry == null)
                    continue;
                lookup[geometry.Id] = geometry;
            }
            return lookup;
        }

        private static IReadOnlyList<TrackSurfaceDefinition> BuildSurfaceWorklist(
            IEnumerable<TrackSurfaceDefinition> explicitSurfaces,
            IEnumerable<TrackAreaDefinition> areas,
            IReadOnlyDictionary<string, GeometryDefinition> geometriesById)
        {
            var list = new List<TrackSurfaceDefinition>();
            var explicitIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (explicitSurfaces != null)
            {
                foreach (var surface in explicitSurfaces)
                {
                    if (surface == null)
                        continue;
                    list.Add(surface);
                    if (!string.IsNullOrWhiteSpace(surface.Id))
                        explicitIds.Add(surface.Id);
                }
            }

            foreach (var generated in BuildImplicitMeshSurfaces(areas, geometriesById, explicitIds))
                list.Add(generated);

            return list;
        }

        private static IReadOnlyList<TrackSurfaceDefinition> BuildImplicitMeshSurfaces(
            IEnumerable<TrackAreaDefinition> areas,
            IReadOnlyDictionary<string, GeometryDefinition> geometriesById,
            HashSet<string> explicitSurfaceIds)
        {
            var generated = new List<TrackSurfaceDefinition>();
            var generatedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (areas == null || geometriesById == null)
                return generated;

            foreach (var area in areas)
            {
                if (area == null || string.IsNullOrWhiteSpace(area.GeometryId))
                    continue;
                if (area.Type == TrackAreaType.Boundary || area.Type == TrackAreaType.OffTrack)
                    continue;
                if (!geometriesById.TryGetValue(area.GeometryId, out var geometry))
                    continue;
                if (geometry.Type != GeometryType.Mesh)
                    continue;

                var candidateId = !string.IsNullOrWhiteSpace(area.SurfaceId)
                    ? area.SurfaceId!.Trim()
                    : $"auto_mesh_{area.Id}";
                if (explicitSurfaceIds.Contains(candidateId))
                    continue;

                var surfaceId = candidateId;
                if (!generatedIds.Add(surfaceId))
                {
                    surfaceId = $"{candidateId}_{area.Id}";
                    if (!generatedIds.Add(surfaceId))
                        continue;
                }

                var metadata = BuildImplicitMeshSurfaceMetadata(area);
                var profileId = TryGetAreaSurfaceValue(area.Metadata, "surface_profile", "mesh_profile");
                var bankId = TryGetAreaSurfaceValue(area.Metadata, "surface_bank", "mesh_bank");
                var layer = TryGetAreaSurfaceLayer(area.Metadata) ?? 0;
                var resolution = TryGetAreaSurfaceResolution(area.Metadata);
                var name = string.IsNullOrWhiteSpace(area.Name)
                    ? $"Auto mesh surface {area.Id}"
                    : $"Auto mesh surface {area.Name}";

                generated.Add(new TrackSurfaceDefinition(
                    surfaceId,
                    TrackSurfaceType.Mesh,
                    area.GeometryId,
                    profileId,
                    bankId,
                    layer,
                    resolution,
                    area.MaterialId,
                    name,
                    metadata.Count > 0 ? metadata : null));
            }

            return generated;
        }

        private static Dictionary<string, TrackProfileDefinition> BuildProfileLookup(IEnumerable<TrackProfileDefinition> profiles)
        {
            var lookup = new Dictionary<string, TrackProfileDefinition>(StringComparer.OrdinalIgnoreCase);
            if (profiles == null)
                return lookup;
            foreach (var profile in profiles)
            {
                if (profile == null)
                    continue;
                lookup[profile.Id] = profile;
            }
            return lookup;
        }

        private static Dictionary<string, TrackBankDefinition> BuildBankLookup(IEnumerable<TrackBankDefinition> banks)
        {
            var lookup = new Dictionary<string, TrackBankDefinition>(StringComparer.OrdinalIgnoreCase);
            if (banks == null)
                return lookup;
            foreach (var bank in banks)
            {
                if (bank == null)
                    continue;
                lookup[bank.Id] = bank;
            }
            return lookup;
        }

        private static Dictionary<string, SurfaceDefaults> BuildSurfaceDefaults(
            IEnumerable<TrackAreaDefinition> areas,
            float defaultWidth)
        {
            var defaults = new Dictionary<string, SurfaceDefaults>(StringComparer.OrdinalIgnoreCase);
            if (areas == null)
                return defaults;

            foreach (var area in areas)
            {
                if (area == null || string.IsNullOrWhiteSpace(area.SurfaceId))
                    continue;

                var surfaceId = area.SurfaceId!.Trim();
                var width = area.WidthMeters ?? defaultWidth;
                var height = area.ElevationMeters;
                var resolution = TryGetAreaSurfaceResolution(area.Metadata);

                if (!defaults.TryGetValue(surfaceId, out var entry))
                {
                    entry = new SurfaceDefaults(width, height, resolution, true, true, true);
                    defaults[surfaceId] = entry;
                    continue;
                }

                var widthOk = entry.Width.HasValue && Math.Abs(entry.Width.Value - width) <= 0.001f;
                var heightOk = entry.BaseHeight.HasValue && Math.Abs(entry.BaseHeight.Value - height) <= 0.001f;
                var resolutionValue = entry.Resolution;
                if (resolution.HasValue)
                {
                    if (!resolutionValue.HasValue)
                        resolutionValue = resolution.Value;
                    else if (Math.Abs(resolutionValue.Value - resolution.Value) > 0.001f)
                        resolutionValue = null;
                }

                defaults[surfaceId] = new SurfaceDefaults(
                    widthOk ? entry.Width : null,
                    heightOk ? entry.BaseHeight : null,
                    resolutionValue,
                    widthOk,
                    heightOk,
                    true);
            }

            return defaults;
        }

        private static float? TryGetAreaSurfaceResolution(IReadOnlyDictionary<string, string> metadata)
        {
            if (metadata == null || metadata.Count == 0)
                return null;

            if (SurfaceParameterParser.TryGetFloat(metadata, out var resolution, "surface_resolution", "surface_cell_size", "surface_grid"))
                return Math.Max(0.1f, resolution);

            return null;
        }

        private static int? TryGetAreaSurfaceLayer(IReadOnlyDictionary<string, string> metadata)
        {
            if (metadata == null || metadata.Count == 0)
                return null;

            if (!SurfaceParameterParser.TryGetValue(metadata, out var raw, "surface_layer", "mesh_layer"))
                return null;

            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
            return null;
        }

        private static string? TryGetAreaSurfaceValue(IReadOnlyDictionary<string, string> metadata, params string[] keys)
        {
            if (metadata == null || metadata.Count == 0)
                return null;

            if (!SurfaceParameterParser.TryGetValue(metadata, out var raw, keys))
                return null;

            var trimmed = raw.Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        private static IReadOnlyDictionary<string, string> BuildImplicitMeshSurfaceMetadata(TrackAreaDefinition area)
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["base_height"] = area.ElevationMeters.ToString(CultureInfo.InvariantCulture)
            };

            CopyAreaMetadata(area.Metadata, metadata, "mesh_height_mode", "mesh_y_mode", "height_mode", "mesh_height");
            CopyAreaMetadata(area.Metadata, metadata, "mesh_y_offset", "mesh_offset", "y_offset", "height_offset");
            CopyAreaMetadata(area.Metadata, metadata, "mesh_apply_profile", "apply_profile", "profile_on_mesh");

            return metadata;
        }

        private static void CopyAreaMetadata(
            IReadOnlyDictionary<string, string> source,
            Dictionary<string, string> destination,
            params string[] keys)
        {
            if (source == null || source.Count == 0)
                return;

            foreach (var key in keys)
            {
                if (!source.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                    continue;
                destination[key] = value.Trim();
                break;
            }
        }

        private readonly struct SurfaceDefaults
        {
            public SurfaceDefaults(float? width, float? baseHeight, float? resolution, bool widthStable, bool heightStable, bool resolutionStable)
            {
                Width = widthStable ? width : null;
                BaseHeight = heightStable ? baseHeight : null;
                Resolution = resolutionStable ? resolution : null;
            }

            public float? Width { get; }
            public float? BaseHeight { get; }
            public float? Resolution { get; }
        }
    }
}
