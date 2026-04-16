using System;
using System.Collections.Generic;
using TS.Sdl.Events;

namespace TS.Sdl.Input
{
    public sealed class GestureRecognizer
    {
        private const float NanosToSeconds = 1f / 1000000000f;

        private readonly GestureOptions _options;
        private readonly Dictionary<ulong, TouchTrack> _touches;
        private readonly Dictionary<ulong, TapTrack> _lastTaps;
        private readonly Dictionary<ulong, TapTrack> _lastTwoFingerTaps;

        public GestureRecognizer()
            : this(null)
        {
        }

        public GestureRecognizer(GestureOptions? options)
        {
            _options = options ?? new GestureOptions();
            _touches = new Dictionary<ulong, TouchTrack>();
            _lastTaps = new Dictionary<ulong, TapTrack>();
            _lastTwoFingerTaps = new Dictionary<ulong, TapTrack>();
        }

        public event Action<GestureEvent>? Raised;

        public void Reset()
        {
            _touches.Clear();
            _lastTaps.Clear();
            _lastTwoFingerTaps.Clear();
        }

        public void Update()
        {
            Update(Runtime.GetTicksNs());
        }

        public void Update(ulong timestamp)
        {
            CheckLongPresses(timestamp);
        }

        public void Process(in Event value)
        {
            switch ((EventType)value.Type)
            {
                case EventType.FingerDown:
                    Update(value.TouchFinger.Timestamp);
                    HandleFingerDown(value.TouchFinger);
                    return;

                case EventType.FingerMotion:
                    Update(value.TouchFinger.Timestamp);
                    HandleFingerMotion(value.TouchFinger);
                    return;

                case EventType.FingerUp:
                    Update(value.TouchFinger.Timestamp);
                    HandleFingerUp(value.TouchFinger, canceled: false);
                    return;

                case EventType.FingerCanceled:
                    Update(value.TouchFinger.Timestamp);
                    HandleFingerUp(value.TouchFinger, canceled: true);
                    return;
            }
        }

        private void HandleFingerDown(TouchFingerEvent value)
        {
            var track = GetTrack(value.TouchId);
            var finger = new FingerTrack(value);
            track.Fingers[value.FingerId] = finger;
            track.WindowId = value.WindowId;

            StartMultiIfNeeded(track, value.Timestamp);
        }

        private void HandleFingerMotion(TouchFingerEvent value)
        {
            if (!_touches.TryGetValue(value.TouchId, out var track))
                return;

            if (!track.Fingers.TryGetValue(value.FingerId, out var finger))
                return;

            finger.Update(value);
            track.WindowId = value.WindowId;

            if (track.Multi != null && track.Multi.Contains(value.FingerId))
                UpdateMulti(track, value.Timestamp);
        }

        private void HandleFingerUp(TouchFingerEvent value, bool canceled)
        {
            if (!_touches.TryGetValue(value.TouchId, out var track))
                return;

            if (!track.Fingers.TryGetValue(value.FingerId, out var finger))
                return;

            finger.Update(value);
            finger.Canceled = canceled;

            if (track.Multi != null && track.Multi.Contains(value.FingerId))
            {
                HandleMultiUp(track, finger, value.Timestamp, canceled);
            }
            else if (!finger.PartOfMulti && !canceled)
            {
                if (!TryEmitSwipe(finger, value.TouchId, value.WindowId, value.Timestamp))
                    TryEmitTap(finger, value.TouchId, value.WindowId, value.Timestamp);
            }

            track.Fingers.Remove(value.FingerId);
            if (track.Fingers.Count == 0)
                _touches.Remove(value.TouchId);
        }

        private void CheckLongPresses(ulong timestamp)
        {
            var minDuration = ToNanoseconds(_options.LongPressTime);
            var maxMoveSq = _options.LongPressMove * _options.LongPressMove;

            foreach (var touchPair in _touches)
            {
                var touchId = touchPair.Key;
                var track = touchPair.Value;

                foreach (var finger in track.Fingers.Values)
                {
                    if (finger.Canceled || finger.PartOfMulti || finger.LongPressRaised)
                        continue;

                    if (timestamp < finger.StartTimestamp)
                        continue;

                    var duration = timestamp - finger.StartTimestamp;
                    if (duration < minDuration)
                        continue;

                    if (DistanceSquared(finger.StartX, finger.StartY, finger.LastX, finger.LastY) > maxMoveSq)
                        continue;

                    finger.LongPressRaised = true;
                    Emit(new GestureEvent
                    {
                        Kind = GestureKind.LongPress,
                        FingerCount = 1,
                        Timestamp = timestamp,
                        TouchId = touchId,
                        FingerId = finger.Id,
                        WindowId = track.WindowId,
                        X = finger.LastX,
                        Y = finger.LastY
                    });
                }
            }
        }

        private bool TryEmitSwipe(FingerTrack finger, ulong touchId, uint windowId, ulong timestamp)
        {
            var dx = finger.LastX - finger.StartX;
            var dy = finger.LastY - finger.StartY;
            var distance = Distance(dx, dy);

            if (distance < _options.SwipeMinDistance)
                return false;

            if (timestamp <= finger.StartTimestamp)
                return false;

            var seconds = (timestamp - finger.StartTimestamp) * NanosToSeconds;
            if (seconds <= 0f)
                return false;

            var velocity = distance / seconds;
            if (velocity < _options.SwipeMinVelocity)
                return false;

            finger.SwipeRaised = true;
            Emit(new GestureEvent
            {
                Kind = GestureKind.Swipe,
                FingerCount = 1,
                Timestamp = timestamp,
                TouchId = touchId,
                FingerId = finger.Id,
                WindowId = windowId,
                X = finger.LastX,
                Y = finger.LastY,
                DeltaX = dx,
                DeltaY = dy,
                Distance = distance,
                Velocity = velocity,
                Direction = ResolveSwipe(dx, dy)
            });

            return true;
        }

        private void TryEmitTap(FingerTrack finger, ulong touchId, uint windowId, ulong timestamp)
        {
            if (finger.LongPressRaised || finger.SwipeRaised || finger.Canceled)
                return;

            if (timestamp < finger.StartTimestamp)
                return;

            var tapDuration = timestamp - finger.StartTimestamp;
            if (tapDuration > ToNanoseconds(_options.TapMaxTime))
                return;

            if (DistanceSquared(finger.StartX, finger.StartY, finger.LastX, finger.LastY) > (_options.TapMove * _options.TapMove))
                return;

            var tapCount = 1;
            if (_lastTaps.TryGetValue(touchId, out var previous))
            {
                var dt = timestamp > previous.Timestamp ? timestamp - previous.Timestamp : ulong.MaxValue;
                var maxDelay = ToNanoseconds(_options.DoubleTapGap);
                var maxMoveSq = _options.DoubleTapMove * _options.DoubleTapMove;

                if (dt <= maxDelay &&
                    DistanceSquared(previous.X, previous.Y, finger.LastX, finger.LastY) <= maxMoveSq)
                {
                    tapCount = previous.Count + 1;
                }
            }

            tapCount = ClampTapCount(tapCount);
            _lastTaps[touchId] = new TapTrack(timestamp, finger.LastX, finger.LastY, tapCount);
            Emit(new GestureEvent
            {
                Kind = ResolveSingleTapKind(tapCount),
                FingerCount = 1,
                TapCount = (byte)tapCount,
                Timestamp = timestamp,
                TouchId = touchId,
                FingerId = finger.Id,
                WindowId = windowId,
                X = finger.LastX,
                Y = finger.LastY
            });
        }

        private void TryEmitTwoFingerTap(TouchTrack track, MultiTrack multi, ulong timestamp)
        {
            var centerX = (multi.FirstStartX + multi.SecondStartX) * 0.5f;
            var centerY = (multi.FirstStartY + multi.SecondStartY) * 0.5f;
            var tapCount = 1;

            if (_lastTwoFingerTaps.TryGetValue(track.TouchId, out var previous))
            {
                var dt = timestamp > previous.Timestamp ? timestamp - previous.Timestamp : ulong.MaxValue;
                var maxDelay = ToNanoseconds(_options.DoubleTapGap);
                var maxMoveSq = _options.DoubleTapMove * _options.DoubleTapMove;

                if (dt <= maxDelay &&
                    DistanceSquared(previous.X, previous.Y, centerX, centerY) <= maxMoveSq)
                {
                    tapCount = previous.Count + 1;
                }
            }

            tapCount = ClampTapCount(tapCount);
            _lastTwoFingerTaps[track.TouchId] = new TapTrack(timestamp, centerX, centerY, tapCount);
            Emit(new GestureEvent
            {
                Kind = ResolveTwoFingerTapKind(tapCount),
                FingerCount = 2,
                TapCount = (byte)tapCount,
                Timestamp = timestamp,
                TouchId = track.TouchId,
                WindowId = track.WindowId,
                X = centerX,
                Y = centerY
            });
        }

        private void StartMultiIfNeeded(TouchTrack track, ulong timestamp)
        {
            if (track.Multi != null)
                return;

            ulong firstId = 0;
            ulong secondId = 0;
            var found = 0;
            foreach (var pair in track.Fingers)
            {
                if (found == 0)
                {
                    firstId = pair.Key;
                    found = 1;
                    continue;
                }

                secondId = pair.Key;
                found = 2;
                break;
            }

            if (found < 2)
                return;

            if (!track.Fingers.TryGetValue(firstId, out var first))
                return;

            if (!track.Fingers.TryGetValue(secondId, out var second))
                return;

            first.PartOfMulti = true;
            second.PartOfMulti = true;

            var distance = Distance(second.LastX - first.LastX, second.LastY - first.LastY);
            var angle = (float)Math.Atan2(second.LastY - first.LastY, second.LastX - first.LastX);
            track.Multi = new MultiTrack(
                firstId,
                secondId,
                timestamp,
                distance,
                angle,
                first.LastX,
                first.LastY,
                second.LastX,
                second.LastY);
        }

        private void UpdateMulti(TouchTrack track, ulong timestamp)
        {
            var multi = track.Multi;
            if (multi == null || multi.FirstUp || multi.SecondUp)
                return;

            if (!track.Fingers.TryGetValue(multi.FirstId, out var first))
                return;

            if (!track.Fingers.TryGetValue(multi.SecondId, out var second))
                return;

            var dx = second.LastX - first.LastX;
            var dy = second.LastY - first.LastY;
            var distance = Distance(dx, dy);
            var angle = (float)Math.Atan2(dy, dx);
            var centerX = (first.LastX + second.LastX) * 0.5f;
            var centerY = (first.LastY + second.LastY) * 0.5f;

            var dtNanos = timestamp > multi.LastTimestamp ? timestamp - multi.LastTimestamp : 1ul;
            var dtSeconds = dtNanos * NanosToSeconds;
            if (dtSeconds <= 0f)
                dtSeconds = 0.000001f;

            var scale = multi.StartDistance > 0f ? distance / multi.StartDistance : 1f;
            var scaleDelta = distance - multi.LastDistance;
            var scaleVelocity = scaleDelta / dtSeconds;

            var rotation = NormalizeAngle(angle - multi.StartAngle);
            var rotationDelta = NormalizeAngle(angle - multi.LastAngle);
            var rotationVelocity = rotationDelta / dtSeconds;

            if (!multi.PinchActive && Math.Abs(distance - multi.StartDistance) >= _options.PinchStartDistance)
            {
                multi.PinchActive = true;
                multi.TwoTapEligible = false;
                Emit(new GestureEvent
                {
                    Kind = GestureKind.PinchBegin,
                    FingerCount = 2,
                    Timestamp = timestamp,
                    TouchId = track.TouchId,
                    WindowId = track.WindowId,
                    X = centerX,
                    Y = centerY,
                    Distance = distance,
                    Scale = scale,
                    ScaleDelta = scaleDelta,
                    ScaleVelocity = scaleVelocity
                });
            }

            if (multi.PinchActive)
            {
                Emit(new GestureEvent
                {
                    Kind = GestureKind.PinchUpdate,
                    FingerCount = 2,
                    Timestamp = timestamp,
                    TouchId = track.TouchId,
                    WindowId = track.WindowId,
                    X = centerX,
                    Y = centerY,
                    Distance = distance,
                    Scale = scale,
                    ScaleDelta = scaleDelta,
                    ScaleVelocity = scaleVelocity
                });
            }

            if (!multi.RotateActive && Math.Abs(rotation) >= _options.RotateStartRadians)
            {
                multi.RotateActive = true;
                multi.TwoTapEligible = false;
                Emit(new GestureEvent
                {
                    Kind = GestureKind.RotateBegin,
                    FingerCount = 2,
                    Timestamp = timestamp,
                    TouchId = track.TouchId,
                    WindowId = track.WindowId,
                    X = centerX,
                    Y = centerY,
                    Rotation = rotation,
                    RotationDelta = rotationDelta,
                    RotationVelocity = rotationVelocity
                });
            }

            if (multi.RotateActive)
            {
                Emit(new GestureEvent
                {
                    Kind = GestureKind.RotateUpdate,
                    FingerCount = 2,
                    Timestamp = timestamp,
                    TouchId = track.TouchId,
                    WindowId = track.WindowId,
                    X = centerX,
                    Y = centerY,
                    Rotation = rotation,
                    RotationDelta = rotationDelta,
                    RotationVelocity = rotationVelocity
                });
            }

            var twoTapMoveSq = _options.TwoTapMove * _options.TwoTapMove;
            if (DistanceSquared(first.LastX, first.LastY, multi.FirstStartX, multi.FirstStartY) > twoTapMoveSq ||
                DistanceSquared(second.LastX, second.LastY, multi.SecondStartX, multi.SecondStartY) > twoTapMoveSq)
            {
                multi.TwoTapEligible = false;
            }

            if (timestamp - multi.StartTimestamp > ToNanoseconds(_options.TwoTapMaxTime))
                multi.TwoTapEligible = false;

            multi.LastDistance = distance;
            multi.LastAngle = angle;
            multi.LastTimestamp = timestamp;
            multi.LastFirstX = first.LastX;
            multi.LastFirstY = first.LastY;
            multi.LastSecondX = second.LastX;
            multi.LastSecondY = second.LastY;
        }

        private void HandleMultiUp(TouchTrack track, FingerTrack finger, ulong timestamp, bool canceled)
        {
            var multi = track.Multi;
            if (multi == null)
                return;

            if (!multi.FirstUp && !multi.SecondUp)
                UpdateMulti(track, timestamp);

            if (finger.Id == multi.FirstId)
            {
                multi.FirstUp = true;
                multi.LastFirstX = finger.LastX;
                multi.LastFirstY = finger.LastY;
            }
            else if (finger.Id == multi.SecondId)
            {
                multi.SecondUp = true;
                multi.LastSecondX = finger.LastX;
                multi.LastSecondY = finger.LastY;
            }

            if (canceled)
            {
                multi.TwoTapEligible = false;
                multi.Canceled = true;
            }

            if (multi.PinchActive)
            {
                multi.PinchActive = false;
                Emit(new GestureEvent
                {
                    Kind = GestureKind.PinchEnd,
                    FingerCount = 2,
                    Timestamp = timestamp,
                    TouchId = track.TouchId,
                    WindowId = track.WindowId,
                    Scale = multi.StartDistance > 0f ? multi.LastDistance / multi.StartDistance : 1f,
                    ScaleDelta = 0f,
                    ScaleVelocity = 0f
                });
            }

            if (multi.RotateActive)
            {
                multi.RotateActive = false;
                Emit(new GestureEvent
                {
                    Kind = GestureKind.RotateEnd,
                    FingerCount = 2,
                    Timestamp = timestamp,
                    TouchId = track.TouchId,
                    WindowId = track.WindowId,
                    Rotation = NormalizeAngle(multi.LastAngle - multi.StartAngle),
                    RotationDelta = 0f,
                    RotationVelocity = 0f
                });
            }

            if (multi.FirstUp && multi.SecondUp)
            {
                var emittedTwoFingerSwipe = TryEmitTwoFingerSwipe(track, multi, timestamp);
                if (!emittedTwoFingerSwipe &&
                    multi.TwoTapEligible &&
                    !multi.Canceled &&
                    timestamp - multi.StartTimestamp <= ToNanoseconds(_options.TwoTapMaxTime))
                {
                    TryEmitTwoFingerTap(track, multi, timestamp);
                }

                track.Multi = null;
            }
        }

        private bool TryEmitTwoFingerSwipe(TouchTrack track, MultiTrack multi, ulong timestamp)
        {
            if (multi.Canceled || multi.TwoTapEligible || multi.PinchActive || multi.RotateActive)
                return false;
            if (timestamp <= multi.StartTimestamp)
                return false;

            var startCenterX = (multi.FirstStartX + multi.SecondStartX) * 0.5f;
            var startCenterY = (multi.FirstStartY + multi.SecondStartY) * 0.5f;
            var endCenterX = (multi.LastFirstX + multi.LastSecondX) * 0.5f;
            var endCenterY = (multi.LastFirstY + multi.LastSecondY) * 0.5f;

            var dx = endCenterX - startCenterX;
            var dy = endCenterY - startCenterY;
            var distance = Distance(dx, dy);
            if (distance < _options.SwipeMinDistance)
                return false;

            var seconds = (timestamp - multi.StartTimestamp) * NanosToSeconds;
            if (seconds <= 0f)
                return false;

            var velocity = distance / seconds;
            if (velocity < _options.SwipeMinVelocity)
                return false;

            Emit(new GestureEvent
            {
                Kind = GestureKind.Swipe,
                FingerCount = 2,
                Timestamp = timestamp,
                TouchId = track.TouchId,
                WindowId = track.WindowId,
                X = endCenterX,
                Y = endCenterY,
                DeltaX = dx,
                DeltaY = dy,
                Distance = distance,
                Velocity = velocity,
                Direction = ResolveSwipe(dx, dy)
            });

            return true;
        }

        private TouchTrack GetTrack(ulong touchId)
        {
            if (_touches.TryGetValue(touchId, out var track))
                return track;

            track = new TouchTrack(touchId);
            _touches.Add(touchId, track);
            return track;
        }

        private void Emit(GestureEvent value)
        {
            Raised?.Invoke(value);
        }

        private static int ClampTapCount(int value)
        {
            if (value <= 1)
                return 1;
            if (value == 2)
                return 2;
            return 3;
        }

        private static GestureKind ResolveSingleTapKind(int tapCount)
        {
            switch (tapCount)
            {
                case 1:
                    return GestureKind.Tap;
                case 2:
                    return GestureKind.DoubleTap;
                default:
                    return GestureKind.TripleTap;
            }
        }

        private static GestureKind ResolveTwoFingerTapKind(int tapCount)
        {
            switch (tapCount)
            {
                case 1:
                    return GestureKind.TwoFingerTap;
                case 2:
                    return GestureKind.TwoFingerDoubleTap;
                default:
                    return GestureKind.TwoFingerTripleTap;
            }
        }

        private static SwipeDirection ResolveSwipe(float dx, float dy)
        {
            if (Math.Abs(dx) >= Math.Abs(dy))
                return dx >= 0f ? SwipeDirection.Right : SwipeDirection.Left;

            return dy >= 0f ? SwipeDirection.Down : SwipeDirection.Up;
        }

        private static float Distance(float dx, float dy)
        {
            return (float)Math.Sqrt((dx * dx) + (dy * dy));
        }

        private static float DistanceSquared(float x1, float y1, float x2, float y2)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            return (dx * dx) + (dy * dy);
        }

        private static float NormalizeAngle(float value)
        {
            const float Pi = (float)Math.PI;
            const float TwoPi = Pi * 2f;

            while (value > Pi)
                value -= TwoPi;

            while (value < -Pi)
                value += TwoPi;

            return value;
        }

        private static ulong ToNanoseconds(TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
                return 0;

            return (ulong)(value.Ticks * 100L);
        }

        private sealed class TapTrack
        {
            public TapTrack(ulong timestamp, float x, float y, int count)
            {
                Timestamp = timestamp;
                X = x;
                Y = y;
                Count = count;
            }

            public ulong Timestamp { get; }
            public float X { get; }
            public float Y { get; }
            public int Count { get; }
        }

        private sealed class TouchTrack
        {
            public TouchTrack(ulong touchId)
            {
                TouchId = touchId;
                Fingers = new Dictionary<ulong, FingerTrack>();
            }

            public ulong TouchId { get; }
            public uint WindowId { get; set; }
            public Dictionary<ulong, FingerTrack> Fingers { get; }
            public MultiTrack? Multi { get; set; }
        }

        private sealed class FingerTrack
        {
            public FingerTrack(TouchFingerEvent value)
            {
                Id = value.FingerId;
                StartTimestamp = value.Timestamp;
                LastTimestamp = value.Timestamp;
                StartX = value.X;
                StartY = value.Y;
                LastX = value.X;
                LastY = value.Y;
            }

            public ulong Id { get; }
            public ulong StartTimestamp { get; }
            public ulong LastTimestamp { get; private set; }
            public float StartX { get; }
            public float StartY { get; }
            public float LastX { get; private set; }
            public float LastY { get; private set; }
            public bool LongPressRaised { get; set; }
            public bool SwipeRaised { get; set; }
            public bool PartOfMulti { get; set; }
            public bool Canceled { get; set; }

            public void Update(TouchFingerEvent value)
            {
                LastTimestamp = value.Timestamp;
                LastX = value.X;
                LastY = value.Y;
            }
        }

        private sealed class MultiTrack
        {
            public MultiTrack(
                ulong firstId,
                ulong secondId,
                ulong timestamp,
                float distance,
                float angle,
                float firstStartX,
                float firstStartY,
                float secondStartX,
                float secondStartY)
            {
                FirstId = firstId;
                SecondId = secondId;
                StartTimestamp = timestamp;
                LastTimestamp = timestamp;
                StartDistance = distance;
                LastDistance = distance;
                StartAngle = angle;
                LastAngle = angle;
                FirstStartX = firstStartX;
                FirstStartY = firstStartY;
                SecondStartX = secondStartX;
                SecondStartY = secondStartY;
                LastFirstX = firstStartX;
                LastFirstY = firstStartY;
                LastSecondX = secondStartX;
                LastSecondY = secondStartY;
                TwoTapEligible = true;
            }

            public ulong FirstId { get; }
            public ulong SecondId { get; }
            public ulong StartTimestamp { get; }
            public ulong LastTimestamp { get; set; }
            public float StartDistance { get; }
            public float LastDistance { get; set; }
            public float StartAngle { get; }
            public float LastAngle { get; set; }
            public float FirstStartX { get; }
            public float FirstStartY { get; }
            public float SecondStartX { get; }
            public float SecondStartY { get; }
            public float LastFirstX { get; set; }
            public float LastFirstY { get; set; }
            public float LastSecondX { get; set; }
            public float LastSecondY { get; set; }
            public bool TwoTapEligible { get; set; }
            public bool PinchActive { get; set; }
            public bool RotateActive { get; set; }
            public bool FirstUp { get; set; }
            public bool SecondUp { get; set; }
            public bool Canceled { get; set; }

            public bool Contains(ulong fingerId)
            {
                return fingerId == FirstId || fingerId == SecondId;
            }
        }
    }
}
