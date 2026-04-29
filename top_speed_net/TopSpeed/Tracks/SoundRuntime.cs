using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Data;
using TS.Audio;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        private sealed class RuntimeTrackSound
        {
            private const float DefaultRandomCrossfadeSeconds = 0.75f;
            private readonly AudioManager _audio;
            private readonly string _sourceRootFullPath;
            private readonly Random _random;
            private readonly IReadOnlyDictionary<string, TrackSoundSourceDefinition> _soundDefinitions;
            private readonly List<PreparedVariant> _preparedVariants;
            private Source? _handle;
            private PreparedVariant? _activeVariant;

            public RuntimeTrackSound(
                AudioManager audio,
                string sourceDirectory,
                Random random,
                IReadOnlyDictionary<string, TrackSoundSourceDefinition> soundDefinitions,
                string id,
                TrackSoundSourceDefinition definition)
            {
                _audio = audio;
                _sourceRootFullPath = Path.GetFullPath(sourceDirectory);
                _random = random;
                _soundDefinitions = soundDefinitions;
                _preparedVariants = new List<PreparedVariant>();
                Id = id;
                Definition = definition;
                ActiveDefinition = definition;
                LastAreaIndex = -1;
                BuildPreparedVariants();
            }

            public string Id { get; }
            public TrackSoundSourceDefinition Definition { get; private set; }
            public TrackSoundSourceDefinition ActiveDefinition { get; private set; }
            public Source? Handle => _handle;
            public int LastAreaIndex { get; set; }
            public bool TriggerActive { get; set; }
            public bool TriggerInitialized { get; set; }

            public void UpdateDefinition(TrackSoundSourceDefinition definition)
            {
                Definition = definition;
                ActiveDefinition = definition;
            }

            public bool EnsureCreated(bool refreshRandomVariant, float categoryScale)
            {
                var selection = SelectVariant(refreshRandomVariant);
                if (!selection.HasValue)
                    return false;

                var activeDefinition = selection.Value.Definition;
                var nextVariant = selection.Value.Variant;
                if (_handle != null && !refreshRandomVariant && ReferenceEquals(nextVariant, _activeVariant))
                {
                    ActiveDefinition = activeDefinition;
                    ApplySourceSettings(_handle, activeDefinition, categoryScale);
                    return true;
                }

                var previousHandle = _handle;
                _activeVariant = nextVariant;
                ActiveDefinition = activeDefinition;
                _handle = nextVariant.Handle;
                ApplySourceSettings(_handle, activeDefinition, categoryScale);

                if (previousHandle != null && !ReferenceEquals(previousHandle, _handle))
                    StopPreviousHandle(previousHandle, refreshRandomVariant);

                if (_handle != null)
                    _handle.SeekToStart();

                return true;
            }

            public void ApplyCategoryVolume(float categoryScale)
            {
                if (_handle == null)
                    return;

                var scale = Clamp01(categoryScale);
                var value = Clamp01(ActiveDefinition.Volume * scale);
                _handle.SetVolume(value);
            }

            public void Play()
            {
                if (_handle == null)
                    return;

                if (_handle.IsPlaying)
                    return;

                if (ActiveDefinition.FadeInSeconds > 0f)
                    _handle.Play(ActiveDefinition.Loop, ActiveDefinition.FadeInSeconds);
                else
                    _handle.Play(ActiveDefinition.Loop);
            }

            public void Stop()
            {
                if (_handle == null)
                    return;

                if (ActiveDefinition.FadeOutSeconds > 0f)
                    _handle.Stop(ActiveDefinition.FadeOutSeconds);
                else
                    _handle.Stop();
            }

            public void Dispose()
            {
                for (var i = 0; i < _preparedVariants.Count; i++)
                    DisposeHandle(_preparedVariants[i].Handle);
                _preparedVariants.Clear();
                _activeVariant = null;
                _handle = null;
            }

            private (TrackSoundSourceDefinition Definition, PreparedVariant Variant)? SelectVariant(bool refreshRandomVariant)
            {
                if (!refreshRandomVariant && _activeVariant != null)
                    return (ActiveDefinition, _activeVariant);

                if (_preparedVariants.Count == 0)
                    return null;

                if (Definition.Type == TrackSoundSourceType.Random && _preparedVariants.Count > 1)
                {
                    var index = _random.Next(_preparedVariants.Count);
                    return (_preparedVariants[index].Definition, _preparedVariants[index]);
                }

                return (_preparedVariants[0].Definition, _preparedVariants[0]);
            }

            private void BuildPreparedVariants()
            {
                var resolved = new List<(TrackSoundSourceDefinition Definition, string Path)>();
                if (!string.IsNullOrWhiteSpace(Definition.Path))
                {
                    var path = ResolveSoundPath(Definition.Path!);
                    if (path != null)
                        resolved.Add((Definition, path));
                }

                for (var i = 0; i < Definition.VariantPaths.Count; i++)
                {
                    var path = ResolveSoundPath(Definition.VariantPaths[i]);
                    if (path != null)
                        resolved.Add((Definition, path));
                }

                for (var i = 0; i < Definition.VariantSourceIds.Count; i++)
                {
                    var sourceId = Definition.VariantSourceIds[i];
                    if (string.IsNullOrWhiteSpace(sourceId))
                        continue;

                    if (!_soundDefinitions.TryGetValue(sourceId, out var sourceDefinition))
                        continue;

                    if (sourceDefinition.Type == TrackSoundSourceType.Random || string.IsNullOrWhiteSpace(sourceDefinition.Path))
                        continue;

                    var path = ResolveSoundPath(sourceDefinition.Path!);
                    if (path != null)
                        resolved.Add((sourceDefinition, path));
                }

                for (var i = 0; i < resolved.Count; i++)
                {
                    var item = resolved[i];
                    var asset = _audio.LoadAsset(item.Path, streamFromDisk: true);
                    var handle = item.Definition.Spatial
                        ? _audio.CreateSpatialSource(asset, AudioEngineOptions.TrackBusName, item.Definition.AllowHrtf)
                        : _audio.CreateSource(asset, AudioEngineOptions.TrackBusName, useHrtf: false);
                    ApplySourceSettings(handle, item.Definition, categoryScale: 1f);
                    _preparedVariants.Add(new PreparedVariant(item.Definition, item.Path, handle));
                }
            }

            private static void ApplySourceSettings(Source handle, TrackSoundSourceDefinition definition, float categoryScale)
            {
                var scale = Clamp01(categoryScale);
                handle.SetVolume(Clamp01(definition.Volume * scale));
                handle.SetPitch(definition.Pitch);
                handle.SetPan(definition.Pan);

                if (definition.MinDistance.HasValue ||
                    definition.MaxDistance.HasValue ||
                    definition.Rolloff.HasValue ||
                    definition.StartRadiusMeters.HasValue ||
                    definition.EndRadiusMeters.HasValue)
                {
                    var minDistance = definition.MinDistance ?? definition.StartRadiusMeters ?? 1.0f;
                    var maxDistance = definition.MaxDistance ?? definition.EndRadiusMeters ?? 10000f;
                    var rolloff = definition.Rolloff ?? 1.0f;
                    handle.SetDistanceModel(DistanceModel.Inverse, minDistance, maxDistance, rolloff);
                }
            }

            private static float Clamp01(float value)
            {
                if (value <= 0f)
                    return 0f;
                if (value >= 1f)
                    return 1f;
                return value;
            }

            private string? ResolveSoundPath(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    return null;

                var trimmed = path.Trim();
                if (Path.IsPathRooted(trimmed))
                    return null;

                var normalized = trimmed
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .TrimStart(Path.DirectorySeparatorChar);
                if (normalized.IndexOf(':') >= 0 || ContainsTraversal(normalized))
                    return null;

                var candidate = Path.GetFullPath(Path.Combine(_sourceRootFullPath, normalized));
                if (!IsInsideTrackRoot(candidate))
                    return null;

                return File.Exists(candidate) ? candidate : null;
            }

            private bool IsInsideTrackRoot(string candidate)
            {
                if (string.Equals(candidate, _sourceRootFullPath, StringComparison.OrdinalIgnoreCase))
                    return true;

                var rootWithSeparator = _sourceRootFullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
            }

            private static bool ContainsTraversal(string path)
            {
                var parts = path.Split(Path.DirectorySeparatorChar);
                for (var i = 0; i < parts.Length; i++)
                {
                    var segment = parts[i].Trim();
                    if (segment == "." || segment == "..")
                        return true;
                }

                return false;
            }

            private static void DisposeHandle(Source handle)
            {
                handle.Stop();
                handle.Dispose();
            }

            private void StopPreviousHandle(Source previousHandle, bool refreshRandomVariant)
            {
                var fadeOutSeconds = 0f;
                if (refreshRandomVariant && Definition.Type == TrackSoundSourceType.Random)
                {
                    fadeOutSeconds = Definition.CrossfadeSeconds.HasValue
                        ? Math.Max(0f, Definition.CrossfadeSeconds.Value)
                        : DefaultRandomCrossfadeSeconds;
                }

                if (fadeOutSeconds > 0f && previousHandle.IsPlaying)
                {
                    previousHandle.Stop(fadeOutSeconds);
                    return;
                }

                previousHandle.Stop();
            }
        }

        private sealed class PreparedVariant
        {
            public PreparedVariant(TrackSoundSourceDefinition definition, string path, Source handle)
            {
                Definition = definition;
                Path = path;
                Handle = handle;
            }

            public TrackSoundSourceDefinition Definition { get; }
            public string Path { get; }
            public Source Handle { get; }
        }
    }
}

