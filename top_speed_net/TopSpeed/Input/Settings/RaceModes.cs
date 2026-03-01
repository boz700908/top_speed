namespace TopSpeed.Input
{
    internal enum InputDeviceMode
    {
        Keyboard,
        Joystick,
        Both
    }

    internal enum CopilotMode
    {
        Off = 0,
        CurvesOnly = 1,
        All = 2
    }

    internal enum CurveAnnouncementMode
    {
        FixedDistance = 0,
        SpeedDependent = 1
    }

    internal enum AutomaticInfoMode
    {
        Off = 0,
        LapsOnly = 1,
        On = 2
    }

    internal enum RaceDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    internal enum UnitSystem
    {
        Metric = 0,
        Imperial = 1
    }
}
