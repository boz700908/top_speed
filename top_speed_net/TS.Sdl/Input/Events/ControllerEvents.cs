using TS.Sdl.Events;

namespace TS.Sdl.Input
{
    public static class ControllerEvents
    {
        public static bool TryPoll(out ControllerEvent controllerEvent)
        {
            while (Runtime.PollEvent(out var value))
            {
                if (TryConvert(value, out controllerEvent))
                    return true;
            }

            controllerEvent = default;
            return false;
        }

        private static bool TryConvert(Event value, out ControllerEvent controllerEvent)
        {
            switch ((EventType)value.Type)
            {
                case EventType.JoystickAdded:
                case EventType.JoystickRemoved:
                case EventType.JoystickUpdateComplete:
                    controllerEvent = new ControllerEvent(
                        ConvertDeviceKind(value.JoyDevice.Type),
                        ControllerEventSource.Joystick,
                        value.JoyDevice.Which,
                        value.JoyDevice.Timestamp);
                    return true;

                case EventType.JoystickAxisMotion:
                    controllerEvent = new ControllerEvent(
                        ControllerEventKind.AxisMotion,
                        ControllerEventSource.Joystick,
                        value.JoyAxis.Which,
                        value.JoyAxis.Timestamp,
                        value.JoyAxis.Axis,
                        value.JoyAxis.Value);
                    return true;

                case EventType.JoystickHatMotion:
                    controllerEvent = new ControllerEvent(
                        ControllerEventKind.HatMotion,
                        ControllerEventSource.Joystick,
                        value.JoyHat.Which,
                        value.JoyHat.Timestamp,
                        value.JoyHat.Hat,
                        value.JoyHat.Value);
                    return true;

                case EventType.JoystickButtonDown:
                case EventType.JoystickButtonUp:
                    controllerEvent = new ControllerEvent(
                        value.JoyButton.Down ? ControllerEventKind.ButtonDown : ControllerEventKind.ButtonUp,
                        ControllerEventSource.Joystick,
                        value.JoyButton.Which,
                        value.JoyButton.Timestamp,
                        value.JoyButton.Button,
                        value.JoyButton.Down ? 1 : 0,
                        value.JoyButton.Down);
                    return true;

                case EventType.JoystickBatteryUpdated:
                    controllerEvent = new ControllerEvent(
                        ControllerEventKind.BatteryUpdated,
                        ControllerEventSource.Joystick,
                        value.JoyBattery.Which,
                        value.JoyBattery.Timestamp,
                        power: new PowerInfo(value.JoyBattery.State, value.JoyBattery.Percent));
                    return true;

                case EventType.GamepadAdded:
                case EventType.GamepadRemoved:
                case EventType.GamepadRemapped:
                case EventType.GamepadUpdateComplete:
                    controllerEvent = new ControllerEvent(
                        ConvertDeviceKind(value.GamepadDevice.Type),
                        ControllerEventSource.Gamepad,
                        value.GamepadDevice.Which,
                        value.GamepadDevice.Timestamp);
                    return true;

                case EventType.GamepadAxisMotion:
                    controllerEvent = new ControllerEvent(
                        ControllerEventKind.AxisMotion,
                        ControllerEventSource.Gamepad,
                        value.GamepadAxis.Which,
                        value.GamepadAxis.Timestamp,
                        value.GamepadAxis.Axis,
                        value.GamepadAxis.Value);
                    return true;

                case EventType.GamepadButtonDown:
                case EventType.GamepadButtonUp:
                    controllerEvent = new ControllerEvent(
                        value.GamepadButton.Down ? ControllerEventKind.ButtonDown : ControllerEventKind.ButtonUp,
                        ControllerEventSource.Gamepad,
                        value.GamepadButton.Which,
                        value.GamepadButton.Timestamp,
                        value.GamepadButton.Button,
                        value.GamepadButton.Down ? 1 : 0,
                        value.GamepadButton.Down);
                    return true;

                case EventType.SensorUpdate:
                    controllerEvent = new ControllerEvent(
                        ControllerEventKind.SensorUpdated,
                        ControllerEventSource.Sensor,
                        value.Sensor.Which,
                        value.Sensor.Timestamp);
                    return true;
            }

            controllerEvent = default;
            return false;
        }

        private static ControllerEventKind ConvertDeviceKind(EventType type)
        {
            switch (type)
            {
                case EventType.JoystickAdded:
                case EventType.GamepadAdded:
                    return ControllerEventKind.Added;
                case EventType.JoystickRemoved:
                case EventType.GamepadRemoved:
                    return ControllerEventKind.Removed;
                case EventType.GamepadRemapped:
                    return ControllerEventKind.Remapped;
                case EventType.JoystickUpdateComplete:
                case EventType.GamepadUpdateComplete:
                    return ControllerEventKind.UpdateComplete;
                default:
                    return ControllerEventKind.Unknown;
            }
        }
    }
}
