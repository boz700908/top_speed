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
            return RuntimeInformation.IsOSPlatform(Android);
#endif
        }

        public static string ForPlatform(string desktopText, string touchText)
        {
            return IsTouchPlatform() ? touchText : desktopText;
        }
    }
}
