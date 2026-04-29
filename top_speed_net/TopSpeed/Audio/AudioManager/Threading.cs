using System;
using System.Threading;
using System.Diagnostics;

namespace TopSpeed.Audio
{
    internal sealed partial class AudioManager
    {
        public void StartUpdateThread(int intervalMs = 8)
        {
            if (_updateRunning)
                return;
            if (intervalMs <= 0)
                intervalMs = 8;

            _updateWake.Reset();
            _updateRunning = true;
            _updateThread = new Thread(() => UpdateLoop(intervalMs))
            {
                IsBackground = true,
                Name = "AudioUpdate"
            };
            if (IsAndroid())
                _updateThread.Priority = ThreadPriority.AboveNormal;
            _updateThread.Start();
        }

        public void StopUpdateThread()
        {
            _updateRunning = false;
            _updateWake.Set();
            if (_updateThread == null)
                return;
            if (_updateThread.IsAlive)
                _updateThread.Join();
            _updateThread = null;
        }

        private void UpdateLoop(int intervalMs)
        {
            var intervalTicks = Math.Max(1L, (long)Math.Round(intervalMs * (double)Stopwatch.Frequency / 1000d));
            var nextDeadline = Stopwatch.GetTimestamp();
            while (_updateRunning)
            {
                try
                {
                    _engine.Update();
                    var updateEndTicks = Stopwatch.GetTimestamp();

                    nextDeadline += intervalTicks;
                    var nowTicks = updateEndTicks;
                    if (nextDeadline <= nowTicks)
                    {
                        var lagTicks = nowTicks - nextDeadline;
                        if (lagTicks >= intervalTicks)
                            nextDeadline = nowTicks + intervalTicks;
                        continue;
                    }

                    var waitTicks = nextDeadline - nowTicks;
                    var waitMs = (int)Math.Max(1L, (waitTicks * 1000L) / Stopwatch.Frequency);
                    if (_updateWake.Wait(waitMs))
                        break;
                }
                catch
                {
                    var nowTicks = Stopwatch.GetTimestamp();
                    nextDeadline = nowTicks + intervalTicks;
                    if (_updateWake.Wait(Math.Max(intervalMs, 50)))
                        break;
                }
            }
        }
    }
}

