using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TS.Sdl.Interop
{
    internal static class Library
    {
        private const int RtldNow = 2;
        private const int RtldGlobal = 0x100;

        private static readonly object Sync = new object();
        private static bool _attempted;
        private static bool _loaded;

        public static bool EnsureLoaded()
        {
            lock (Sync)
            {
                if (_attempted)
                    return _loaded;

                _attempted = true;
                foreach (var candidate in GetCandidates())
                {
                    if (TryLoad(candidate))
                    {
                        _loaded = true;
                        break;
                    }
                }

                return _loaded;
            }
        }

        private static string[] GetCandidates()
        {
            var baseDir = AppContext.BaseDirectory;
            var fileName = GetLibraryFileName();
            return new[]
            {
                Path.Combine(baseDir, "lib", fileName),
                Path.Combine(baseDir, fileName)
            };
        }

        private static string GetLibraryFileName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "SDL3.dll";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "libSDL3.dylib";
            return "libSDL3.so";
        }

        private static bool TryLoad(string path)
        {
            if (!File.Exists(path))
                return false;

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return LoadLibrary(path) != IntPtr.Zero;

                return Dlopen(path, RtldNow | RtldGlobal) != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("libdl.so.2", EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
        private static extern IntPtr DlopenLinux(string fileName, int flags);

        [DllImport("libSystem.B.dylib", EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
        private static extern IntPtr DlopenMac(string fileName, int flags);

        private static IntPtr Dlopen(string fileName, int flags)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? DlopenMac(fileName, flags)
                : DlopenLinux(fileName, flags);
        }
    }
}
