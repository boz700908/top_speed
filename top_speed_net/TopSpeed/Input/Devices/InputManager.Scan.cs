using System;
using System.Threading;
using SharpDX;

namespace TopSpeed.Input
{
    internal sealed partial class InputManager
    {
        private bool TryRescanJoystick(bool force = false)
        {
            if (_disposed)
                return false;
            var now = Environment.TickCount;
            if (!force && unchecked((uint)(now - _lastJoystickScan)) < (uint)JoystickRescanIntervalMs)
                return false;
            _lastJoystickScan = now;

            JoystickDevice? newJoystick;
            try
            {
                newJoystick = new JoystickDevice(_directInput, _windowHandle);
            }
            catch (SharpDXException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }

            JoystickDevice? oldJoystick;
            var available = newJoystick.IsAvailable;
            lock (_hidLock)
            {
                oldJoystick = _joystick;
                _joystick = available ? newJoystick : null;
            }
            oldJoystick?.Dispose();
            if (!available)
                newJoystick.Dispose();
            return available;
        }

        private void StartHidScan()
        {
            if (_disposed || !_joystickEnabled || _gamepad.IsAvailable)
                return;
            lock (_hidScanLock)
            {
                if (_hidScanThread != null && _hidScanThread.IsAlive)
                    return;
                _hidScanCts?.Cancel();
                _hidScanCts?.Dispose();
                _hidScanCts = new CancellationTokenSource();
                var token = _hidScanCts.Token;
                _hidScanThread = new Thread(() => HidScanWorker(token))
                {
                    IsBackground = true,
                    Name = "JoystickScan"
                };
                _hidScanThread.Start();
            }
        }

        private void StopHidScan()
        {
            CancellationTokenSource? cts;
            Thread? thread;
            lock (_hidScanLock)
            {
                cts = _hidScanCts;
                thread = _hidScanThread;
                _hidScanCts = null;
                _hidScanThread = null;
            }

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            if (thread != null && thread.IsAlive)
                thread.Join(JoystickRescanIntervalMs + 500);
        }

        private void HidScanWorker(CancellationToken token)
        {
            var start = Environment.TickCount;
            while (true)
            {
                if (token.IsCancellationRequested || _disposed || !_joystickEnabled)
                    return;

                if (_gamepad.IsAvailable)
                    return;

                if (TryRescanJoystick(force: true))
                    return;

                var elapsed = unchecked((uint)(Environment.TickCount - start));
                if (elapsed >= (uint)JoystickScanTimeoutMs)
                {
                    JoystickScanTimedOut?.Invoke();
                    return;
                }

                if (token.WaitHandle.WaitOne(JoystickRescanIntervalMs))
                    return;
            }
        }

        private void ClearJoystickDevice()
        {
            JoystickDevice? oldJoystick;
            lock (_hidLock)
            {
                oldJoystick = _joystick;
                _joystick = null;
            }
            oldJoystick?.Dispose();
        }
    }
}
