using System;
using System.Runtime.InteropServices;

namespace TopSpeed.Speech
{
    internal sealed partial class SpeechService
    {
        private sealed class ZdsrClient : IDisposable
        {
            private IntPtr _module;
            private bool _initialized;
            private bool _available;

            public bool IsAvailable => EnsureInitialized();

            public ZdsrClient()
            {
                try
                {
                    _module = LoadLibrary("ZDSRAPI_x64.dll");
                }
                catch
                {
                    _module = IntPtr.Zero;
                }
            }

            public bool Speak(string text, bool stop)
            {
                if (!EnsureInitialized())
                    return false;

                try
                {
                    if (stop)
                        StopSpeak();

                    return SpeakText(text, true) == 0;
                }
                catch
                {
                    return false;
                }
            }

            public void Stop()
            {
                if (!EnsureInitialized())
                    return;

                try
                {
                    StopSpeak();
                }
                catch
                {
                }
            }

            private bool EnsureInitialized()
            {
                if (_initialized)
                    return _available;

                _initialized = true;
                if (_module == IntPtr.Zero)
                {
                    _available = false;
                    return false;
                }

                try
                {
                    _available = InitTts(1, null, false) == 0;
                }
                catch
                {
                    _available = false;
                }

                return _available;
            }

            public void Dispose()
            {
                if (_module != IntPtr.Zero)
                {
                    FreeLibrary(_module);
                    _module = IntPtr.Zero;
                }
            }

            [DllImport("ZDSRAPI_x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi, EntryPoint = "InitTTS")]
            private static extern int InitTts(int type, [MarshalAs(UnmanagedType.LPWStr)] string? channelName, bool keyDownInterrupt);

            [DllImport("ZDSRAPI_x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi, EntryPoint = "Speak")]
            private static extern int SpeakText([MarshalAs(UnmanagedType.LPWStr)] string text, bool interrupt);

            [DllImport("ZDSRAPI_x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi, EntryPoint = "StopSpeak")]
            private static extern void StopSpeak();

            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern IntPtr LoadLibrary(string lpFileName);

            [DllImport("kernel32", SetLastError = true)]
            private static extern bool FreeLibrary(IntPtr hModule);
        }
    }
}
