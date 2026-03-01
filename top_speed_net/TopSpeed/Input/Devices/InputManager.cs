using System;
using System.Threading;
using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed partial class InputManager : IDisposable
    {
        private const int JoystickRescanIntervalMs = 1000;
        private const int JoystickScanTimeoutMs = 5000;
        private const int MenuBackThreshold = 50;

        private readonly DirectInput _directInput;
        private readonly Keyboard _keyboard;
        private readonly GamepadDevice _gamepad;
        private JoystickDevice? _joystick;
        private readonly InputState _current;
        private readonly InputState _previous;
        private readonly bool[] _keyLatch;
        private readonly IntPtr _windowHandle;
        private int _lastJoystickScan;
        private bool _suspended;
        private bool _menuBackLatched;
        private readonly object _hidLock = new object();
        private readonly object _hidScanLock = new object();
        private Thread? _hidScanThread;
        private CancellationTokenSource? _hidScanCts;
        private bool _joystickEnabled;
        private bool _disposed;

        public InputState Current => _current;

        public event Action? JoystickScanTimedOut;

        public InputManager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            _directInput = new DirectInput();
            _keyboard = new Keyboard(_directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            _gamepad = new GamepadDevice();
            _current = new InputState();
            _previous = new InputState();
            _keyLatch = new bool[256];
            TryAcquire();
        }

        public bool IsDown(Key key) => _current.IsDown(key);

        public IVibrationDevice? VibrationDevice => _gamepad.IsAvailable
            ? (_joystickEnabled ? _gamepad : null)
            : (_joystickEnabled ? GetJoystickDevice() : null);

        public void SetDeviceMode(InputDeviceMode mode)
        {
            var enableJoystick = mode != InputDeviceMode.Keyboard;
            if (enableJoystick == _joystickEnabled)
                return;

            _joystickEnabled = enableJoystick;
            if (!_joystickEnabled)
            {
                StopHidScan();
                ClearJoystickDevice();
                return;
            }

            if (!_gamepad.IsAvailable && GetJoystickDevice() == null)
                StartHidScan();
        }

        private JoystickDevice? GetJoystickDevice()
        {
            lock (_hidLock)
            {
                return _joystick != null && _joystick.IsAvailable ? _joystick : null;
            }
        }
    }
}
