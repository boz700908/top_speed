using System;
using System.Runtime.InteropServices;

namespace TopSpeed.Menu
{
    internal static class InteractionHints
    {
        private static readonly OSPlatform Android = OSPlatform.Create("ANDROID");

        public static bool IsTouchPlatform()
        {
#if NETFRAMEWORK
            return false;
#else
            var explicitTouchHints = Environment.GetEnvironmentVariable("TOPSPEED_TOUCH_HINTS");
            if (string.Equals(explicitTouchHints, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(explicitTouchHints, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (OperatingSystem.IsAndroid())
                return true;
            if (RuntimeInformation.IsOSPlatform(Android))
                return true;
            if (RuntimeInformation.RuntimeIdentifier.StartsWith("android", StringComparison.OrdinalIgnoreCase))
                return true;
            if (Type.GetType("Android.OS.Build, Mono.Android", throwOnError: false) != null)
                return true;
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANDROID_ROOT")))
                return true;
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANDROID_DATA")))
                return true;

            return RuntimeInformation.IsOSPlatform(Android);
#endif
        }

        public static string ForPlatform(string desktopText, string touchText)
        {
            return IsTouchPlatform() ? touchText : desktopText;
        }
    }
}
