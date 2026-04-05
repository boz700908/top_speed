namespace TS.Sdl.Input
{
    public enum HapticEffectType : ushort
    {
        Constant = 1 << 0,
        Sine = 1 << 1,
        Square = 1 << 2,
        Triangle = 1 << 3,
        SawToothUp = 1 << 4,
        SawToothDown = 1 << 5,
        Ramp = 1 << 6,
        Spring = 1 << 7,
        Damper = 1 << 8,
        Inertia = 1 << 9,
        Friction = 1 << 10,
        LeftRight = 1 << 11,
        Custom = 1 << 15
    }
}
