using System;
using System.Collections.Generic;

namespace TopSpeed.Tracks.Walls
{
    public sealed class TrackWallDefinition
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public TrackWallDefinition(
            string id,
            string shapeId,
            float widthMeters,
            float elevationMeters,
            TrackWallMaterial collisionMaterial,
            TrackWallCollisionMode collisionMode,
            string? name = null,
            IReadOnlyDictionary<string, string>? metadata = null,
            float heightMeters = 2f,
            string? materialId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Wall id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(shapeId))
                throw new ArgumentException("Wall shape id is required.", nameof(shapeId));

            Id = id.Trim();
            ShapeId = shapeId.Trim();
            WidthMeters = widthMeters;
            ElevationMeters = elevationMeters;
            CollisionMaterial = collisionMaterial;
            CollisionMode = collisionMode;
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            Metadata = NormalizeMetadata(metadata);
            HeightMeters = heightMeters < 0f ? 0f : heightMeters;
            var trimmedMaterial = materialId?.Trim();
            MaterialId = string.IsNullOrWhiteSpace(trimmedMaterial) ? null : trimmedMaterial;
        }

        public string Id { get; }
        public string ShapeId { get; }
        public float WidthMeters { get; }
        public float ElevationMeters { get; }
        public float HeightMeters { get; }
        public TrackWallMaterial CollisionMaterial { get; }
        public TrackWallCollisionMode CollisionMode { get; }
        public string? Name { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }
        public string? MaterialId { get; }

        private static IReadOnlyDictionary<string, string> NormalizeMetadata(IReadOnlyDictionary<string, string>? metadata)
        {
            if (metadata == null || metadata.Count == 0)
                return EmptyMetadata;
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in metadata)
                copy[pair.Key] = pair.Value;
            return copy;
        }
    }
}
