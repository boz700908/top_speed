using System;

namespace TS.Sdl.Input
{
    [Flags]
    public enum HapticFeatures : uint
    {
        None = 0,
        Constant = 1u << 0,
        Sine = 1u << 1,
        Square = 1u << 2,
        Triangle = 1u << 3,
        SawToothUp = 1u << 4,
        SawToothDown = 1u << 5,
        Ramp = 1u << 6,
        Spring = 1u << 7,
        Damper = 1u << 8,
        Inertia = 1u << 9,
        Friction = 1u << 10,
        LeftRight = 1u << 11,
        Custom = 1u << 15,
        Gain = 1u << 16,
        AutoCenter = 1u << 17,
        Status = 1u << 18,
        Pause = 1u << 19
    }
}
