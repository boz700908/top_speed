using TS.Sdl.Input;

namespace TopSpeed.Input
{
    internal static class GestureIntentMapper
    {
        public static bool TryMap(in GestureEvent value, out GestureIntent intent)
        {
            switch (value.Kind)
            {
                case GestureKind.Swipe:
                    var twoFinger = value.FingerCount >= 2;
                    intent = value.Direction switch
                    {
                        SwipeDirection.Left => twoFinger ? GestureIntent.TwoFingerSwipeLeft : GestureIntent.SwipeLeft,
                        SwipeDirection.Right => twoFinger ? GestureIntent.TwoFingerSwipeRight : GestureIntent.SwipeRight,
                        SwipeDirection.Up => twoFinger ? GestureIntent.TwoFingerSwipeUp : GestureIntent.SwipeUp,
                        SwipeDirection.Down => twoFinger ? GestureIntent.TwoFingerSwipeDown : GestureIntent.SwipeDown,
                        _ => GestureIntent.Unknown
                    };
                    return intent != GestureIntent.Unknown;

                case GestureKind.Tap:
                    intent = GestureIntent.Tap;
                    return true;

                case GestureKind.DoubleTap:
                    intent = GestureIntent.DoubleTap;
                    return true;

                case GestureKind.TripleTap:
                    intent = GestureIntent.TripleTap;
                    return true;

                case GestureKind.LongPress:
                    intent = GestureIntent.LongPress;
                    return true;

                case GestureKind.TwoFingerTap:
                    intent = GestureIntent.TwoFingerTap;
                    return true;

                case GestureKind.TwoFingerDoubleTap:
                    intent = GestureIntent.TwoFingerDoubleTap;
                    return true;

                case GestureKind.TwoFingerTripleTap:
                    intent = GestureIntent.TwoFingerTripleTap;
                    return true;

                default:
                    intent = GestureIntent.Unknown;
                    return false;
            }
        }
    }
}
