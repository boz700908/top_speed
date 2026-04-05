using System.Runtime.InteropServices;

namespace TS.Sdl.Input
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct HapticEffect
    {
        [FieldOffset(0)]
        public ushort Type;

        [FieldOffset(0)]
        public HapticConstantEffect Constant;

        [FieldOffset(0)]
        public HapticPeriodicEffect Periodic;

        [FieldOffset(0)]
        public HapticConditionEffect Condition;

        [FieldOffset(0)]
        public HapticRampEffect Ramp;

        [FieldOffset(0)]
        public HapticLeftRightEffect LeftRight;
    }
}
