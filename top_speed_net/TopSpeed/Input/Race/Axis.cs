using System;
using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        private int GetAxis(JoystickAxisOrButton axis)
        {
            return GetAxis(axis, _lastJoystick);
        }

        private int GetAxis(JoystickAxisOrButton axis, JoystickStateSnapshot state)
        {
            switch (axis)
            {
                case JoystickAxisOrButton.AxisNone:
                    return 0;
                case JoystickAxisOrButton.AxisXNeg:
                    if (_center.X - state.X > 0)
                        return Math.Min(_center.X - state.X, 100);
                    break;
                case JoystickAxisOrButton.AxisXPos:
                    if (state.X - _center.X > 0)
                        return Math.Min(state.X - _center.X, 100);
                    break;
                case JoystickAxisOrButton.AxisYNeg:
                    if (_center.Y - state.Y > 0)
                        return Math.Min(_center.Y - state.Y, 100);
                    break;
                case JoystickAxisOrButton.AxisYPos:
                    if (state.Y - _center.Y > 0)
                        return Math.Min(state.Y - _center.Y, 100);
                    break;
                case JoystickAxisOrButton.AxisZNeg:
                    if (_center.Z - state.Z > 0)
                        return Math.Min(_center.Z - state.Z, 100);
                    break;
                case JoystickAxisOrButton.AxisZPos:
                    if (state.Z - _center.Z > 0)
                        return Math.Min(state.Z - _center.Z, 100);
                    break;
                case JoystickAxisOrButton.AxisRxNeg:
                    if (_center.Rx - state.Rx > 0)
                        return Math.Min(_center.Rx - state.Rx, 100);
                    break;
                case JoystickAxisOrButton.AxisRxPos:
                    if (state.Rx - _center.Rx > 0)
                        return Math.Min(state.Rx - _center.Rx, 100);
                    break;
                case JoystickAxisOrButton.AxisRyNeg:
                    if (_center.Ry - state.Ry > 0)
                        return Math.Min(_center.Ry - state.Ry, 100);
                    break;
                case JoystickAxisOrButton.AxisRyPos:
                    if (state.Ry - _center.Ry > 0)
                        return Math.Min(state.Ry - _center.Ry, 100);
                    break;
                case JoystickAxisOrButton.AxisRzNeg:
                    if (_center.Rz - state.Rz > 0)
                        return Math.Min(_center.Rz - state.Rz, 100);
                    break;
                case JoystickAxisOrButton.AxisRzPos:
                    if (state.Rz - _center.Rz > 0)
                        return Math.Min(state.Rz - _center.Rz, 100);
                    break;
                case JoystickAxisOrButton.AxisSlider1Neg:
                    if (_center.Slider1 - state.Slider1 > 0)
                        return Math.Min(_center.Slider1 - state.Slider1, 100);
                    break;
                case JoystickAxisOrButton.AxisSlider1Pos:
                    if (state.Slider1 - _center.Slider1 > 0)
                        return Math.Min(state.Slider1 - _center.Slider1, 100);
                    break;
                case JoystickAxisOrButton.AxisSlider2Neg:
                    if (_center.Slider2 - state.Slider2 > 0)
                        return Math.Min(_center.Slider2 - state.Slider2, 100);
                    break;
                case JoystickAxisOrButton.AxisSlider2Pos:
                    if (state.Slider2 - _center.Slider2 > 0)
                        return Math.Min(state.Slider2 - _center.Slider2, 100);
                    break;
                case JoystickAxisOrButton.Button1:
                    return state.B1 ? 100 : 0;
                case JoystickAxisOrButton.Button2:
                    return state.B2 ? 100 : 0;
                case JoystickAxisOrButton.Button3:
                    return state.B3 ? 100 : 0;
                case JoystickAxisOrButton.Button4:
                    return state.B4 ? 100 : 0;
                case JoystickAxisOrButton.Button5:
                    return state.B5 ? 100 : 0;
                case JoystickAxisOrButton.Button6:
                    return state.B6 ? 100 : 0;
                case JoystickAxisOrButton.Button7:
                    return state.B7 ? 100 : 0;
                case JoystickAxisOrButton.Button8:
                    return state.B8 ? 100 : 0;
                case JoystickAxisOrButton.Button9:
                    return state.B9 ? 100 : 0;
                case JoystickAxisOrButton.Button10:
                    return state.B10 ? 100 : 0;
                case JoystickAxisOrButton.Button11:
                    return state.B11 ? 100 : 0;
                case JoystickAxisOrButton.Button12:
                    return state.B12 ? 100 : 0;
                case JoystickAxisOrButton.Button13:
                    return state.B13 ? 100 : 0;
                case JoystickAxisOrButton.Button14:
                    return state.B14 ? 100 : 0;
                case JoystickAxisOrButton.Button15:
                    return state.B15 ? 100 : 0;
                case JoystickAxisOrButton.Button16:
                    return state.B16 ? 100 : 0;
                case JoystickAxisOrButton.Pov1:
                    return state.Pov1 ? 100 : 0;
                case JoystickAxisOrButton.Pov2:
                    return state.Pov2 ? 100 : 0;
                case JoystickAxisOrButton.Pov3:
                    return state.Pov3 ? 100 : 0;
                case JoystickAxisOrButton.Pov4:
                    return state.Pov4 ? 100 : 0;
                case JoystickAxisOrButton.Pov5:
                    return state.Pov5 ? 100 : 0;
                case JoystickAxisOrButton.Pov6:
                    return state.Pov6 ? 100 : 0;
                case JoystickAxisOrButton.Pov7:
                    return state.Pov7 ? 100 : 0;
                case JoystickAxisOrButton.Pov8:
                    return state.Pov8 ? 100 : 0;
                default:
                    return 0;
            }

            return 0;
        }

        private bool AxisPressed(JoystickAxisOrButton axis)
        {
            if (!UseJoystick)
                return false;
            var current = GetAxis(axis, _lastJoystick);
            var previous = _hasPrevJoystick ? GetAxis(axis, _prevJoystick) : 0;
            return current > 50 && previous <= 50;
        }
    }
}
