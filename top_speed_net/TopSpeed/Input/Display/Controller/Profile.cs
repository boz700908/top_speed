namespace TopSpeed.Input
{
    internal readonly struct ControllerDisplayProfile
    {
        public ControllerDisplayProfile(ControllerDeviceType deviceType, ControllerGamepadFamily gamepadFamily)
        {
            DeviceType = deviceType;
            GamepadFamily = gamepadFamily;
        }

        public ControllerDeviceType DeviceType { get; }
        public ControllerGamepadFamily GamepadFamily { get; }

        public bool IsGamepad => DeviceType == ControllerDeviceType.Gamepad;

        public static ControllerDisplayProfile Joystick { get; } =
            new ControllerDisplayProfile(ControllerDeviceType.Joystick, ControllerGamepadFamily.None);

        public static ControllerDisplayProfile RacingWheel { get; } =
            new ControllerDisplayProfile(ControllerDeviceType.RacingWheel, ControllerGamepadFamily.None);

        public static ControllerDisplayProfile SemanticGamepad { get; } =
            new ControllerDisplayProfile(ControllerDeviceType.Gamepad, ControllerGamepadFamily.Semantic);
    }
}
