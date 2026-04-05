using System.Runtime.InteropServices;

namespace TS.Sdl.Input
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HapticConstantEffect
    {
        public ushort Type;
        public HapticDirection Direction;
        public uint Length;
        public ushort Delay;
        public ushort Button;
        public ushort Interval;
        public short Level;
        public ushort AttackLength;
        public ushort AttackLevel;
        public ushort FadeLength;
        public ushort FadeLevel;
    }
}
