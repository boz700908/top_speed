using System;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Vibration;
using TS.Sdl.Input;

namespace TopSpeed.Input.Backends.Sdl
{
    internal sealed partial class Device : IVibrationDevice
    {
        private readonly Choice _choice;
        private readonly Joystick _joystick;
        private readonly Gamepad? _gamepad;
        private readonly HapticDevice? _haptic;
        private readonly ControllerDisplayProfile _displayProfile;
        private State _state;
        private PowerInfo _powerInfo;
        private bool _connected;

        private Device(
            Choice choice,
            Joystick joystick,
            Gamepad? gamepad,
            HapticDevice? haptic,
            ControllerDisplayProfile displayProfile)
        {
            _choice = choice;
            _joystick = joystick;
            _gamepad = gamepad;
            _haptic = haptic;
            _displayProfile = displayProfile;
            _connected = true;
        }

        public bool IsAvailable => _connected;
        public uint InstanceId => _joystick.InstanceId;
        public State State => _state;
        public bool IsRacingWheel => _choice.IsRacingWheel;
        public DeviceMetadata Metadata => _gamepad != null ? _gamepad.Metadata : _joystick.Metadata;
        public PowerInfo PowerInfo => _powerInfo;
        public ControllerDisplayProfile DisplayProfile => _displayProfile;
        public bool ForceFeedbackCapable => _gamepad != null || (_haptic != null && (_haptic.SupportsRumble() || _haptic.Features != HapticFeatures.None));

        public static Device? Open(DiscoveredDevice discovered)
        {
            if (discovered == null)
                throw new ArgumentNullException(nameof(discovered));

            var joystick = Joystick.Open(discovered.InstanceId);
            if (joystick == null)
                return null;

            try
            {
                Gamepad? gamepad = null;
                if (discovered.IsGamepad)
                    gamepad = Gamepad.Open(discovered.InstanceId);

                var haptic = HapticDevice.OpenFromJoystick(joystick);
                var displayProfile = Display.CreateProfile(discovered.Metadata, discovered.Choice.IsRacingWheel);
                return new Device(discovered.Choice, joystick, gamepad, haptic, displayProfile);
            }
            catch
            {
                joystick.Dispose();
                throw;
            }
        }

        public bool Update()
        {
            if (_joystick.ConnectionState == JoystickConnectionState.Invalid)
            {
                _connected = false;
                return false;
            }

            _connected = true;
            _state = _gamepad != null
                ? BuildGamepadState(_gamepad)
                : BuildJoystickState(_joystick);
            _powerInfo = _gamepad != null ? _gamepad.PowerInfo : _joystick.PowerInfo;
            return true;
        }

        public void SetPowerInfo(PowerInfo value)
        {
            _powerInfo = value;
        }

        public void Dispose()
        {
            StopAllFeedback();
            _haptic?.Dispose();
            _gamepad?.Dispose();
            _joystick.Dispose();
        }

        private static State BuildGamepadState(Gamepad gamepad)
        {
            var state = new State
            {
                X = ScaleAxis(gamepad.GetAxis(GamepadAxis.LeftX)),
                Y = -ScaleAxis(gamepad.GetAxis(GamepadAxis.LeftY)),
                Rx = ScaleAxis(gamepad.GetAxis(GamepadAxis.RightX)),
                Ry = -ScaleAxis(gamepad.GetAxis(GamepadAxis.RightY)),
                Z = ScaleTrigger(gamepad.GetAxis(GamepadAxis.LeftTrigger)),
                Rz = ScaleTrigger(gamepad.GetAxis(GamepadAxis.RightTrigger)),
                B1 = gamepad.GetButton(GamepadButton.South),
                B2 = gamepad.GetButton(GamepadButton.East),
                B3 = gamepad.GetButton(GamepadButton.West),
                B4 = gamepad.GetButton(GamepadButton.North),
                B5 = gamepad.GetButton(GamepadButton.LeftShoulder),
                B6 = gamepad.GetButton(GamepadButton.RightShoulder),
                B7 = gamepad.GetButton(GamepadButton.Back),
                B8 = gamepad.GetButton(GamepadButton.Start),
                B9 = gamepad.GetButton(GamepadButton.LeftStick),
                B10 = gamepad.GetButton(GamepadButton.RightStick),
                B11 = gamepad.GetButton(GamepadButton.Guide),
                B12 = gamepad.GetButton(GamepadButton.Misc1),
                B13 = gamepad.GetButton(GamepadButton.LeftPaddle1),
                B14 = gamepad.GetButton(GamepadButton.RightPaddle1),
                B15 = gamepad.GetButton(GamepadButton.LeftPaddle2),
                B16 = gamepad.GetButton(GamepadButton.RightPaddle2),
                Pov1 = gamepad.GetButton(GamepadButton.DPadUp),
                Pov2 = gamepad.GetButton(GamepadButton.DPadRight),
                Pov3 = gamepad.GetButton(GamepadButton.DPadDown),
                Pov4 = gamepad.GetButton(GamepadButton.DPadLeft)
            };
            return state;
        }

        private static State BuildJoystickState(Joystick joystick)
        {
            var state = new State();
            var axisCount = joystick.AxisCount;
            if (axisCount > 0) state.X = ScaleAxis(joystick.GetAxis(0));
            if (axisCount > 1) state.Y = ScaleAxis(joystick.GetAxis(1));
            if (axisCount > 2) state.Z = ScaleAxis(joystick.GetAxis(2));
            if (axisCount > 3) state.Rx = ScaleAxis(joystick.GetAxis(3));
            if (axisCount > 4) state.Ry = ScaleAxis(joystick.GetAxis(4));
            if (axisCount > 5) state.Rz = ScaleAxis(joystick.GetAxis(5));
            if (axisCount > 6) state.Slider1 = ScaleAxis(joystick.GetAxis(6));
            if (axisCount > 7) state.Slider2 = ScaleAxis(joystick.GetAxis(7));

            var buttonCount = joystick.ButtonCount;
            if (buttonCount > 0) state.B1 = joystick.GetButton(0);
            if (buttonCount > 1) state.B2 = joystick.GetButton(1);
            if (buttonCount > 2) state.B3 = joystick.GetButton(2);
            if (buttonCount > 3) state.B4 = joystick.GetButton(3);
            if (buttonCount > 4) state.B5 = joystick.GetButton(4);
            if (buttonCount > 5) state.B6 = joystick.GetButton(5);
            if (buttonCount > 6) state.B7 = joystick.GetButton(6);
            if (buttonCount > 7) state.B8 = joystick.GetButton(7);
            if (buttonCount > 8) state.B9 = joystick.GetButton(8);
            if (buttonCount > 9) state.B10 = joystick.GetButton(9);
            if (buttonCount > 10) state.B11 = joystick.GetButton(10);
            if (buttonCount > 11) state.B12 = joystick.GetButton(11);
            if (buttonCount > 12) state.B13 = joystick.GetButton(12);
            if (buttonCount > 13) state.B14 = joystick.GetButton(13);
            if (buttonCount > 14) state.B15 = joystick.GetButton(14);
            if (buttonCount > 15) state.B16 = joystick.GetButton(15);

            if (joystick.HatCount > 0)
                ApplyHat(joystick.GetHat(0), ref state.Pov1, ref state.Pov2, ref state.Pov3, ref state.Pov4);
            if (joystick.HatCount > 1)
                ApplyHat(joystick.GetHat(1), ref state.Pov5, ref state.Pov6, ref state.Pov7, ref state.Pov8);

            return state;
        }

        private static void ApplyHat(JoystickHat value, ref bool up, ref bool right, ref bool down, ref bool left)
        {
            up = (value & JoystickHat.Up) != 0;
            right = (value & JoystickHat.Right) != 0;
            down = (value & JoystickHat.Down) != 0;
            left = (value & JoystickHat.Left) != 0;
        }

        private static int ScaleAxis(short value)
        {
            return (int)Math.Round(value / 327.67f);
        }

        private static int ScaleTrigger(short value)
        {
            return (int)Math.Round(Math.Max(0, (int)value) / 327.67f);
        }
    }
}
