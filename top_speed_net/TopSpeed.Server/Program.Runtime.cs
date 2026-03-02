using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using TopSpeed.Server.Network;

namespace TopSpeed.Server
{
    internal static partial class Program
    {
        private sealed class WindowsTimerResolution : IDisposable
        {
            private readonly uint _milliseconds;
            private readonly bool _active;

            public WindowsTimerResolution(uint milliseconds)
            {
                _milliseconds = milliseconds;
                try
                {
                    _active = timeBeginPeriod(_milliseconds) == 0;
                }
                catch
                {
                    _active = false;
                }
            }

            public void Dispose()
            {
                if (!_active)
                    return;

                try
                {
                    timeEndPeriod(_milliseconds);
                }
                catch
                {
                    // Ignore timer API shutdown failures.
                }
            }

            [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
            private static extern uint timeBeginPeriod(uint uPeriod);

            [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
            private static extern uint timeEndPeriod(uint uPeriod);
        }

        private static void RunLoop(RaceServer server, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();
            var last = stopwatch.Elapsed;
            while (!token.IsCancellationRequested)
            {
                var now = stopwatch.Elapsed;
                var deltaSeconds = (float)(now - last).TotalSeconds;
                last = now;
                server.Update(deltaSeconds);
                Thread.Sleep(1);
            }
        }
    }
}
