using System.Runtime.InteropServices;

namespace TS.Sdl.Input
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HapticRampEffect
    {
        public ushort Type;
        public HapticDirection Direction;
        public uint Length;
        public ushort Delay;
        public ushort Button;
        public ushort Interval;
        public short Start;
        public short End;
        public ushort AttackLength;
        public ushort AttackLevel;
        public ushort FadeLength;
        public ushort FadeLevel;
    }
}
