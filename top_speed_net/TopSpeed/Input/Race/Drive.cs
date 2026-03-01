using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        public int GetSteering()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var joystickSteer = 0;
            if (UseJoystick)
            {
                var left = GetAxis(_left);
                var right = GetAxis(_right);
                joystickSteer = left != 0 ? -left : right;
                if (joystickSteer != 0 || !UseKeyboard)
                    return joystickSteer;
            }

            if (UseKeyboard)
            {
                if (_lastState.IsDown(_kbLeft))
                    return -100;
                if (_lastState.IsDown(_kbRight))
                    return 100;
            }

            return joystickSteer;
        }

        public int GetThrottle()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var joystickThrottle = UseJoystick ? GetAxis(_throttle) : 0;
            if (joystickThrottle != 0 || !UseKeyboard)
                return joystickThrottle;

            return UseKeyboard && _lastState.IsDown(_kbThrottle) ? 100 : 0;
        }

        public int GetBrake()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var joystickBrake = UseJoystick ? -GetAxis(_brake) : 0;
            if (joystickBrake != 0 || !UseKeyboard)
                return joystickBrake;

            return UseKeyboard && _lastState.IsDown(_kbBrake) ? -100 : 0;
        }

        public bool GetReverseRequested() => _allowDrivingInput && UseKeyboard && WasPressed(Key.Z);

        public bool GetForwardRequested() => _allowDrivingInput && UseKeyboard && WasPressed(Key.A);
    }
}
