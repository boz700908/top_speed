namespace TS.Sdl.Input
{
    public enum ControllerEventKind
    {
        Unknown = 0,
        Added = 1,
        Removed = 2,
        Remapped = 3,
        AxisMotion = 4,
        ButtonDown = 5,
        ButtonUp = 6,
        HatMotion = 7,
        BatteryUpdated = 8,
        UpdateComplete = 9,
        SensorUpdated = 10
    }
}
