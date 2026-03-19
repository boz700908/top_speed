using System;
using System.Runtime.InteropServices;

namespace TopSpeed.Speech
{
    internal sealed partial class SpeechService
    {
        private sealed class BoyCtrlClient : IDisposable
        {
            private IntPtr _module;
            private bool _initialized;
            private bool _available;

            public bool IsAvailable => EnsureInitialized();

            public BoyCtrlClient()
            {
                try
                {
                    _module = LoadLibrary("BoyCtrl-x64.dll");
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
                        Stop();

                    return BoyCtrlSpeak(
                        text,
                        withSlave: false,
                        append: false,
                        allowBreak: true,
                        onCompletion: null) == BoyCtrlError.Success;
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
                    BoyCtrlStopSpeaking(withSlave: false);
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
                    var result = BoyCtrlInitialize(null);
                    _available = result == BoyCtrlError.Success && BoyCtrlIsReaderRunning();
                }
                catch
                {
                    _available = false;
                }

                return _available;
            }

            public void Dispose()
            {
                try
                {
                    BoyCtrlUninitialize();
                }
                catch
                {
                }

                if (_module != IntPtr.Zero)
                {
                    FreeLibrary(_module);
                    _module = IntPtr.Zero;
                }
            }

            private enum BoyCtrlError
            {
                Success = 0
            }

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private delegate void BoyCtrlSpeakCompleteFunc(int reason);

            [DllImport("BoyCtrl-x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            private static extern BoyCtrlError BoyCtrlInitialize([MarshalAs(UnmanagedType.LPWStr)] string? logPath);

            [DllImport("BoyCtrl-x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            private static extern void BoyCtrlUninitialize();

            [DllImport("BoyCtrl-x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            private static extern BoyCtrlError BoyCtrlSpeak(
                [MarshalAs(UnmanagedType.LPWStr)] string text,
                [MarshalAs(UnmanagedType.I1)] bool withSlave,
                [MarshalAs(UnmanagedType.I1)] bool append,
                [MarshalAs(UnmanagedType.I1)] bool allowBreak,
                BoyCtrlSpeakCompleteFunc? onCompletion);

            [DllImport("BoyCtrl-x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            private static extern BoyCtrlError BoyCtrlStopSpeaking([MarshalAs(UnmanagedType.I1)] bool withSlave);

            [DllImport("BoyCtrl-x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool BoyCtrlIsReaderRunning();

            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern IntPtr LoadLibrary(string lpFileName);

            [DllImport("kernel32", SetLastError = true)]
            private static extern bool FreeLibrary(IntPtr hModule);
        }
    }
}
