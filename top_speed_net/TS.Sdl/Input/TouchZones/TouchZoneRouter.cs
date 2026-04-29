using System;
using System.Collections.Generic;
using TS.Sdl.Events;

namespace TS.Sdl.Input
{
    public sealed class TouchZoneRouter : IDisposable
    {
        private readonly GestureOptions _gestureOptions;
        private readonly RecognizerEntry _defaultRecognizer;
        private readonly Dictionary<string, RecognizerEntry> _zoneRecognizers;
        private readonly Dictionary<ulong, TouchTrack> _touches;
        private bool _disposed;

        public TouchZoneRouter()
            : this(null)
        {
        }

        public TouchZoneRouter(GestureRecognizer? recognizer)
        {
            Zones = new TouchZoneRegistry();
            var defaultRecognizer = recognizer ?? new GestureRecognizer();
            _gestureOptions = defaultRecognizer.Options;
            _defaultRecognizer = CreateRecognizerEntry(string.Empty, defaultRecognizer);
            _zoneRecognizers = new Dictionary<string, RecognizerEntry>(StringComparer.Ordinal);
            _touches = new Dictionary<ulong, TouchTrack>();
        }

        public GestureRecognizer Recognizer => _defaultRecognizer.Recognizer;
        public TouchZoneRegistry Zones { get; }

        public event Action<TouchZoneTouchEvent>? TouchRaised;
        public event Action<TouchZoneGestureEvent>? GestureRaised;

        public void SetZone(in TouchZone zone) => Zones.Set(zone);

        public bool RemoveZone(string id)
        {
            var removed = Zones.Remove(id);
            if (removed)
                RemoveZoneRecognizer(NormalizeZoneId(id));
            return removed;
        }

        public void ClearZones()
        {
            Zones.Clear();
            _touches.Clear();
            ClearZoneRecognizers();
        }

        public void Reset()
        {
            _touches.Clear();
            _defaultRecognizer.Recognizer.Reset();
            foreach (var entry in _zoneRecognizers.Values)
                entry.Recognizer.Reset();
        }

        public void Update()
        {
            ThrowIfDisposed();
            Update(Runtime.GetTicksNs());
        }

        public void Update(ulong timestamp)
        {
            ThrowIfDisposed();
            _defaultRecognizer.Recognizer.Update(timestamp);
            foreach (var entry in _zoneRecognizers.Values)
                entry.Recognizer.Update(timestamp);
        }

        public void Process(in Event value)
        {
            ThrowIfDisposed();
            var type = (EventType)value.Type;
            switch (type)
            {
                case EventType.FingerDown:
                    HandleFingerDown(value);
                    return;

                case EventType.FingerMotion:
                    HandleFingerMotion(value);
                    return;

                case EventType.FingerUp:
                case EventType.FingerCanceled:
                    HandleFingerUp(type, value);
                    CleanupTouch(value.TouchFinger.TouchId);
                    return;

                default:
                    _defaultRecognizer.Recognizer.Process(value);
                    return;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _defaultRecognizer.Detach();
            ClearZoneRecognizers();
            _touches.Clear();
        }

        private void HandleFingerDown(in Event value)
        {
            var finger = value.TouchFinger;
            var track = GetOrCreateTrack(finger.TouchId);
            var hit = ResolveHit(finger.X, finger.Y, out var zone);
            var behavior = zone.HasValue ? zone.Value.Behavior : TouchZoneBehavior.Lock;
            var recognitionZoneId = hit.IsAssigned ? NormalizeZoneId(hit.ZoneId) : string.Empty;
            var state = new FingerState(hit, behavior, down: true, recognitionZoneId);
            track.Fingers[finger.FingerId] = state;
            TouchRaised?.Invoke(new TouchZoneTouchEvent(EventType.FingerDown, finger, hit));
            ProcessGesture(value, recognitionZoneId);
        }

        private void HandleFingerMotion(in Event value)
        {
            var finger = value.TouchFinger;
            if (!_touches.TryGetValue(finger.TouchId, out var track))
                track = GetOrCreateTrack(finger.TouchId);

            if (!track.Fingers.TryGetValue(finger.FingerId, out var state))
            {
                var fallback = ResolveHit(finger.X, finger.Y, out _);
                TouchRaised?.Invoke(new TouchZoneTouchEvent(EventType.FingerMotion, finger, fallback));
                ProcessGesture(value, fallback.IsAssigned ? NormalizeZoneId(fallback.ZoneId) : string.Empty);
                return;
            }

            if (state.Behavior == TouchZoneBehavior.Dynamic)
                state.Zone = ResolveHit(finger.X, finger.Y, out _);

            state.Down = true;
            track.Fingers[finger.FingerId] = state;
            TouchRaised?.Invoke(new TouchZoneTouchEvent(EventType.FingerMotion, finger, state.Zone));
            ProcessGesture(value, state.RecognitionZoneId);
        }

        private void HandleFingerUp(EventType type, in Event value)
        {
            var finger = value.TouchFinger;
            var hit = TouchZoneHit.None;
            var recognitionZoneId = string.Empty;
            if (_touches.TryGetValue(finger.TouchId, out var track) &&
                track.Fingers.TryGetValue(finger.FingerId, out var state))
            {
                state.Down = false;
                track.Fingers[finger.FingerId] = state;
                hit = state.Zone;
                recognitionZoneId = state.RecognitionZoneId;
            }
            else
            {
                hit = ResolveHit(finger.X, finger.Y, out _);
                recognitionZoneId = hit.IsAssigned ? NormalizeZoneId(hit.ZoneId) : string.Empty;
            }

            TouchRaised?.Invoke(new TouchZoneTouchEvent(type, finger, hit));
            ProcessGesture(value, recognitionZoneId);
        }

        private TouchZoneHit ResolveGestureZone(in GestureEvent value, string recognitionZoneId)
        {
            if (value.FingerCount <= 1)
            {
                if (_touches.TryGetValue(value.TouchId, out var track) &&
                    track.Fingers.TryGetValue(value.FingerId, out var state))
                    return state.Zone;

                return ResolveHit(value.X, value.Y, out _);
            }

            if (!_touches.TryGetValue(value.TouchId, out var multiTrack))
                return ResolveHit(value.X, value.Y, out _);

            var foundAssigned = false;
            var foundUnassigned = false;
            var matchedZoneId = string.Empty;
            var matched = TouchZoneHit.None;
            foreach (var finger in multiTrack.Fingers.Values)
            {
                if (!string.Equals(finger.RecognitionZoneId, recognitionZoneId, StringComparison.Ordinal))
                    continue;

                if (!finger.Zone.IsAssigned)
                {
                    foundUnassigned = true;
                    continue;
                }

                if (!foundAssigned)
                {
                    foundAssigned = true;
                    matchedZoneId = finger.Zone.ZoneId ?? string.Empty;
                    matched = finger.Zone;
                    continue;
                }

                if (!string.Equals(matchedZoneId, finger.Zone.ZoneId, StringComparison.Ordinal))
                    return TouchZoneHit.None;
            }

            if (!foundAssigned || foundUnassigned)
                return TouchZoneHit.None;

            return matched;
        }

        private void OnGestureRaised(GestureEvent value, string recognitionZoneId)
        {
            var zone = ResolveGestureZone(value, recognitionZoneId);
            GestureRaised?.Invoke(new TouchZoneGestureEvent(value, zone));
        }

        private void ProcessGesture(in Event value, string recognitionZoneId)
        {
            var entry = GetRecognizerEntry(recognitionZoneId);
            entry.Recognizer.Process(value);
        }

        private RecognizerEntry GetRecognizerEntry(string recognitionZoneId)
        {
            if (string.IsNullOrWhiteSpace(recognitionZoneId))
                return _defaultRecognizer;

            if (_zoneRecognizers.TryGetValue(recognitionZoneId, out var entry))
                return entry;

            entry = CreateRecognizerEntry(recognitionZoneId, new GestureRecognizer(_gestureOptions));
            _zoneRecognizers.Add(recognitionZoneId, entry);
            return entry;
        }

        private RecognizerEntry CreateRecognizerEntry(string recognitionZoneId, GestureRecognizer recognizer)
        {
            Action<GestureEvent> handler = value => OnGestureRaised(value, recognitionZoneId);
            recognizer.Raised += handler;
            return new RecognizerEntry(recognizer, handler);
        }

        private TouchTrack GetOrCreateTrack(ulong touchId)
        {
            if (_touches.TryGetValue(touchId, out var existing))
                return existing;

            var track = new TouchTrack();
            _touches.Add(touchId, track);
            return track;
        }

        private TouchZoneHit ResolveHit(float x, float y, out TouchZone? zone)
        {
            if (Zones.TryResolve(x, y, out var resolved))
            {
                zone = resolved;
                return TouchZoneHit.From(resolved);
            }

            zone = null;
            return TouchZoneHit.None;
        }

        private void CleanupTouch(ulong touchId)
        {
            if (!_touches.TryGetValue(touchId, out var track))
                return;

            foreach (var state in track.Fingers.Values)
            {
                if (state.Down)
                    return;
            }

            _touches.Remove(touchId);
        }

        private void ClearZoneRecognizers()
        {
            foreach (var entry in _zoneRecognizers.Values)
                entry.Detach();
            _zoneRecognizers.Clear();
        }

        private void RemoveZoneRecognizer(string zoneId)
        {
            if (!_zoneRecognizers.TryGetValue(zoneId, out var entry))
                return;

            entry.Detach();
            _zoneRecognizers.Remove(zoneId);
        }

        private static string NormalizeZoneId(string? zoneId)
        {
            return string.IsNullOrWhiteSpace(zoneId) ? string.Empty : zoneId!.Trim();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TouchZoneRouter));
        }

        private sealed class TouchTrack
        {
            public Dictionary<ulong, FingerState> Fingers { get; } = new Dictionary<ulong, FingerState>();
        }

        private struct FingerState
        {
            public FingerState(TouchZoneHit zone, TouchZoneBehavior behavior, bool down, string recognitionZoneId)
            {
                Zone = zone;
                Behavior = behavior;
                Down = down;
                RecognitionZoneId = recognitionZoneId ?? string.Empty;
            }

            public TouchZoneHit Zone;
            public TouchZoneBehavior Behavior;
            public bool Down;
            public string RecognitionZoneId;
        }

        private sealed class RecognizerEntry
        {
            private readonly Action<GestureEvent> _handler;

            public RecognizerEntry(GestureRecognizer recognizer, Action<GestureEvent> handler)
            {
                Recognizer = recognizer;
                _handler = handler;
            }

            public GestureRecognizer Recognizer { get; }

            public void Detach()
            {
                Recognizer.Raised -= _handler;
            }
        }
    }
}
