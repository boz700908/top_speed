using System;
using System.IO;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Data;
using TS.Audio;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        private void InitializeSounds()
        {
            var root = Path.Combine(AssetPaths.SoundsRoot, "Legacy");
            _soundCrowd = CreateLegacySound(root, "crowd.wav");
            _soundOcean = CreateLegacySound(root, "ocean.wav");
            _soundRain = CreateLegacySound(root, "rain.wav");
            _soundWind = CreateLegacySound(root, "wind.wav");
            _soundStorm = CreateLegacySound(root, "storm.wav");
            _soundDesert = CreateLegacySound(root, "desert.wav");
            _soundAirport = CreateLegacySound(root, "airport.wav");
            _soundAirplane = CreateLegacySound(root, "airplane.wav");
            _soundClock = CreateLegacySound(root, "clock.wav");
            _soundJet = CreateLegacySound(root, "jet.wav");
            _soundThunder = CreateLegacySound(root, "thunder.wav");
            _soundPile = CreateLegacySound(root, "pile.wav");
            _soundConstruction = CreateLegacySound(root, "const.wav");
            _soundRiver = CreateLegacySound(root, "river.wav");
            _soundHelicopter = CreateLegacySound(root, "helicopter.wav");
            _soundOwl = CreateLegacySound(root, "owl.wav");
        }

        private AudioSourceHandle? CreateLegacySound(string root, string file)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path))
                return null;
            return _audio.CreateLoopingSource(path);
        }

        private void InitializeTrackSoundSources()
        {
            if (_soundDefinitions.Count == 0)
                return;

            foreach (var pair in _soundDefinitions)
            {
                var runtime = new RuntimeTrackSound(
                    _audio,
                    _sourceDirectory,
                    _random,
                    _soundDefinitions,
                    EnqueuePendingHandleStop,
                    pair.Key,
                    pair.Value);
                _segmentTrackSounds[pair.Key] = runtime;
                _allTrackSounds.Add(runtime);

                if (pair.Value.Global && runtime.EnsureCreated(refreshRandomVariant: false))
                    runtime.Play();
            }
        }

        private void EnqueuePendingHandleStop(AudioSourceHandle handle, float fadeOutSeconds)
        {
            if (fadeOutSeconds <= 0f)
            {
                handle.Dispose();
                return;
            }

            var disposeAt = DateTime.UtcNow.AddSeconds(fadeOutSeconds);
            _pendingHandleStops.Add(new PendingHandleStop(handle, disposeAt));
        }

        private void UpdatePendingHandleStops()
        {
            if (_pendingHandleStops.Count == 0)
                return;

            var now = DateTime.UtcNow;
            for (var i = _pendingHandleStops.Count - 1; i >= 0; i--)
            {
                if (now < _pendingHandleStops[i].DisposeAtUtc)
                    continue;

                _pendingHandleStops[i].Handle.Dispose();
                _pendingHandleStops.RemoveAt(i);
            }
        }

        private void DisposePendingHandleStops()
        {
            for (var i = 0; i < _pendingHandleStops.Count; i++)
                _pendingHandleStops[i].Handle.Dispose();
            _pendingHandleStops.Clear();
        }

        private void ActivateTrackSoundsForPosition(float position, int segmentIndex)
        {
            if (_segmentTrackSounds.Count == 0)
                return;

            foreach (var runtime in _allTrackSounds)
            {
                var shouldPlay = ShouldPlayRuntimeSound(runtime, position, segmentIndex);
                if (!shouldPlay)
                {
                    runtime.Stop();
                    continue;
                }

                var refreshRandom =
                    runtime.Definition.Type == TrackSoundSourceType.Random &&
                    runtime.Definition.RandomMode == TrackSoundRandomMode.PerArea &&
                    runtime.LastAreaIndex != segmentIndex;

                if (!runtime.EnsureCreated(refreshRandom))
                    continue;

                if (runtime.Handle != null)
                {
                    UpdateTrackSoundPlacement(runtime, position, segmentIndex);
                    runtime.Play();
                }

                runtime.LastAreaIndex = segmentIndex;
            }
        }

        private bool ShouldPlayRuntimeSound(RuntimeTrackSound runtime, float position, int segmentIndex)
        {
            var definition = runtime.Definition;
            if (definition.Global)
                return true;

            var hasStartOrEndConditions = HasStartOrEndConditions(definition);
            if (hasStartOrEndConditions)
                return UpdateTriggerState(runtime, position, segmentIndex);

            if (IsSoundAssignedToSegment(segmentIndex, runtime.Id))
                return true;

            return IsSegmentInSoundArea(segmentIndex, definition);
        }

        private bool IsSoundAssignedToSegment(int segmentIndex, string soundId)
        {
            if (segmentIndex < 0 || segmentIndex >= _definition.Length)
                return false;

            var segment = _definition[segmentIndex];
            for (var i = 0; i < segment.SoundSourceIds.Count; i++)
            {
                if (string.Equals(segment.SoundSourceIds[i], soundId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool HasStartOrEndConditions(TrackSoundSourceDefinition definition)
        {
            return !string.IsNullOrWhiteSpace(definition.StartAreaId) ||
                   !string.IsNullOrWhiteSpace(definition.EndAreaId) ||
                   definition.StartPosition.HasValue ||
                   definition.EndPosition.HasValue;
        }

        private bool UpdateTriggerState(RuntimeTrackSound runtime, float position, int segmentIndex)
        {
            if (!runtime.TriggerInitialized)
            {
                runtime.TriggerInitialized = true;
                runtime.TriggerActive = false;
            }

            if (!runtime.TriggerActive)
            {
                runtime.TriggerActive = IsStartConditionMet(runtime.Definition, position, segmentIndex);
            }
            else if (IsEndConditionMet(runtime.Definition, position, segmentIndex))
            {
                runtime.TriggerActive = false;
            }

            return runtime.TriggerActive;
        }

        private bool IsStartConditionMet(TrackSoundSourceDefinition definition, float position, int segmentIndex)
        {
            var hasStartCondition = !string.IsNullOrWhiteSpace(definition.StartAreaId) || definition.StartPosition.HasValue;
            if (!hasStartCondition)
                return true;

            if (IsAreaConditionMet(segmentIndex, definition.StartAreaId))
                return true;

            if (definition.StartPosition.HasValue &&
                IsPositionConditionMet(position, definition.StartPosition.Value, definition.StartRadiusMeters))
            {
                return true;
            }

            return false;
        }

        private bool IsEndConditionMet(TrackSoundSourceDefinition definition, float position, int segmentIndex)
        {
            if (IsAreaConditionMet(segmentIndex, definition.EndAreaId))
                return true;

            if (definition.EndPosition.HasValue &&
                IsPositionConditionMet(position, definition.EndPosition.Value, definition.EndRadiusMeters))
            {
                return true;
            }

            return false;
        }

        private bool IsAreaConditionMet(int segmentIndex, string? areaId)
        {
            if (string.IsNullOrWhiteSpace(areaId))
                return false;
            if (!_segmentIndexById.TryGetValue(areaId!, out var areaSegment))
                return false;
            return areaSegment == segmentIndex;
        }

        private bool IsPositionConditionMet(float playerPosition, Vector3 targetPosition, float? radiusMeters)
        {
            var radius = radiusMeters ?? 1f;
            if (radius <= 0f)
                radius = 1f;

            var listenerZ = _lapDistance > 0f ? WrapPosition(playerPosition) : playerPosition;
            var dx = targetPosition.X;
            var dy = targetPosition.Y;
            var dz = _lapDistance > 0f
                ? AudioWorld.WrapDelta(targetPosition.Z - listenerZ, _lapDistance)
                : targetPosition.Z - listenerZ;

            var distanceSquared = (dx * dx) + (dy * dy) + (dz * dz);
            return distanceSquared <= (radius * radius);
        }

        private void UpdateTrackSoundPlacement(RuntimeTrackSound runtime, float position, int segmentIndex)
        {
            var handle = runtime.Handle;
            if (handle == null)
                return;

            var definition = runtime.ActiveDefinition;
            if (!definition.Spatial)
            {
                handle.SetPan(definition.Pan);
                return;
            }

            var sourcePos = ComputeTrackSoundPosition(runtime, position, segmentIndex);
            handle.SetPosition(sourcePos);
            handle.SetVelocity(Vector3.Zero);
        }

        private Vector3 ComputeTrackSoundPosition(RuntimeTrackSound runtime, float playerPosition, int segmentIndex)
        {
            var definition = runtime.ActiveDefinition;
            var lapPos = _lapDistance > 0f ? WrapPosition(playerPosition) : playerPosition;
            var segmentStart = GetSegmentStartDistance(segmentIndex);
            var segmentLength = segmentIndex >= 0 && segmentIndex < _definition.Length
                ? _definition[segmentIndex].Length
                : MinPartLengthMeters;
            var segmentCenter = segmentStart + (segmentLength * 0.5f);

            if (definition.Type == TrackSoundSourceType.Moving &&
                TryComputeMovingSoundPosition(definition, playerPosition, segmentIndex, out var movingPosition))
            {
                var wrappedZ = WrapWorldZ(movingPosition.Z, lapPos, playerPosition);
                return new Vector3(
                    AudioWorld.ToMeters(movingPosition.X),
                    AudioWorld.ToMeters(movingPosition.Y),
                    AudioWorld.ToMeters(wrappedZ));
            }

            if (definition.StartPosition.HasValue && definition.EndPosition.HasValue)
            {
                var t = ComputeAreaProgress(segmentIndex, definition);
                if (t <= 0f &&
                    definition.SpeedMetersPerSecond.HasValue &&
                    Math.Abs(definition.SpeedMetersPerSecond.Value) > 0.0001f &&
                    _lapDistance > 0f &&
                    definition.StartAreaId == null &&
                    definition.EndAreaId == null)
                {
                    var phase = (WrapPosition(playerPosition) * definition.SpeedMetersPerSecond.Value) / _lapDistance;
                    t = phase - (float)Math.Floor(phase);
                }

                var start = definition.StartPosition.Value;
                var end = definition.EndPosition.Value;
                var x = Lerp(start.X, end.X, t);
                var y = Lerp(start.Y, end.Y, t);
                var z = Lerp(start.Z, end.Z, t);
                var wrappedZ = WrapWorldZ(z, lapPos, playerPosition);
                return new Vector3(AudioWorld.ToMeters(x), AudioWorld.ToMeters(y), AudioWorld.ToMeters(wrappedZ));
            }

            if (definition.Position.HasValue)
            {
                var pos = definition.Position.Value;
                var wrappedZ = WrapWorldZ(pos.Z, lapPos, playerPosition);
                return new Vector3(AudioWorld.ToMeters(pos.X), AudioWorld.ToMeters(pos.Y), AudioWorld.ToMeters(wrappedZ));
            }

            var xDefault = 0f;
            var zDefault = WrapWorldZ(segmentCenter, lapPos, playerPosition);
            return new Vector3(AudioWorld.ToMeters(xDefault), 0f, AudioWorld.ToMeters(zDefault));
        }

        private bool TryComputeMovingSoundPosition(
            TrackSoundSourceDefinition definition,
            float playerPosition,
            int segmentIndex,
            out Vector3 position)
        {
            position = default;
            var speed = definition.SpeedMetersPerSecond ?? 0f;
            if (Math.Abs(speed) <= 0.0001f)
                return false;

            var pathLength = _lapDistance > 0f ? _lapDistance : 0f;
            var hasAreaSpan = TryResolveAreaSpan(definition, out var areaStartZ, out _, out var areaLength);
            if (hasAreaSpan)
                pathLength = areaLength;
            if (pathLength <= 0f)
                return false;

            var phase = (WrapPosition(playerPosition) * speed) / pathLength;
            phase -= (float)Math.Floor(phase);
            if (phase < 0f)
                phase += 1f;

            if (definition.StartPosition.HasValue && definition.EndPosition.HasValue)
            {
                var start = definition.StartPosition.Value;
                var end = definition.EndPosition.Value;
                position = new Vector3(
                    Lerp(start.X, end.X, phase),
                    Lerp(start.Y, end.Y, phase),
                    Lerp(start.Z, end.Z, phase));
                return true;
            }

            if (definition.Position.HasValue)
            {
                var anchor = definition.Position.Value;
                var travel = pathLength * phase;
                var z = hasAreaSpan ? (areaStartZ + travel) : (anchor.Z + travel);
                if (_lapDistance > 0f)
                    z = WrapPosition(z);

                position = new Vector3(anchor.X, anchor.Y, z);
                return true;
            }

            var fallbackZ = GetSegmentCenterDistance(segmentIndex) + (pathLength * phase);
            if (_lapDistance > 0f)
                fallbackZ = WrapPosition(fallbackZ);
            position = new Vector3(0f, 0f, fallbackZ);
            return true;
        }

        private bool TryResolveAreaSpan(TrackSoundSourceDefinition definition, out float startZ, out float endZ, out float pathLength)
        {
            startZ = 0f;
            endZ = 0f;
            pathLength = 0f;
            if (string.IsNullOrWhiteSpace(definition.StartAreaId) || string.IsNullOrWhiteSpace(definition.EndAreaId))
                return false;

            if (!_segmentIndexById.TryGetValue(definition.StartAreaId!, out var startIndex))
                return false;
            if (!_segmentIndexById.TryGetValue(definition.EndAreaId!, out var endIndex))
                return false;

            startZ = GetSegmentCenterDistance(startIndex);
            endZ = GetSegmentCenterDistance(endIndex);

            if (_lapDistance > 0f)
            {
                pathLength = endZ - startZ;
                if (pathLength < 0f)
                    pathLength += _lapDistance;
                if (pathLength <= 0f)
                    pathLength = _definition[endIndex].Length;
            }
            else
            {
                pathLength = Math.Max(0.001f, Math.Abs(endZ - startZ));
            }

            return pathLength > 0f;
        }

        private float GetSegmentStartDistance(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= _definition.Length)
                return 0f;

            if (_lapDistance > 0f && segmentIndex < _segmentStartDistances.Length)
                return _segmentStartDistances[segmentIndex];

            var start = 0f;
            for (var i = 0; i < segmentIndex; i++)
                start += _definition[i].Length;
            return start;
        }

        private float GetSegmentCenterDistance(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= _definition.Length)
                return 0f;
            return GetSegmentStartDistance(segmentIndex) + (_definition[segmentIndex].Length * 0.5f);
        }

        private float ComputeAreaProgress(int segmentIndex, TrackSoundSourceDefinition definition)
        {
            if (segmentIndex < 0 || segmentIndex >= _segmentCount)
                return 0f;

            if (definition.StartAreaId == null || definition.EndAreaId == null)
                return 0f;

            if (!_segmentIndexById.TryGetValue(definition.StartAreaId, out var startIndex))
                return 0f;
            if (!_segmentIndexById.TryGetValue(definition.EndAreaId, out var endIndex))
                return 0f;

            if (startIndex == endIndex)
                return 0f;

            var span = (endIndex - startIndex + _segmentCount) % _segmentCount;
            if (span == 0)
                return 0f;

            var delta = (segmentIndex - startIndex + _segmentCount) % _segmentCount;
            var t = delta / (float)span;
            if (t < 0f)
                t = 0f;
            if (t > 1f)
                t = 1f;
            return t;
        }

        private bool IsSegmentInSoundArea(int segmentIndex, TrackSoundSourceDefinition definition)
        {
            if (definition.StartAreaId == null && definition.EndAreaId == null)
                return false;

            if (segmentIndex < 0 || segmentIndex >= _segmentCount)
                return false;

            if (definition.StartAreaId == null || definition.EndAreaId == null)
            {
                if (definition.StartAreaId != null && _segmentIndexById.TryGetValue(definition.StartAreaId, out var startOnly))
                    return startOnly == segmentIndex;
                if (definition.EndAreaId != null && _segmentIndexById.TryGetValue(definition.EndAreaId, out var endOnly))
                    return endOnly == segmentIndex;
                return false;
            }

            if (!_segmentIndexById.TryGetValue(definition.StartAreaId, out var start))
                return false;
            if (!_segmentIndexById.TryGetValue(definition.EndAreaId, out var end))
                return false;

            if (start <= end)
                return segmentIndex >= start && segmentIndex <= end;

            return segmentIndex >= start || segmentIndex <= end;
        }
    }
}
