using System.Runtime.InteropServices;

namespace TS.Sdl.Input
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HapticLeftRightEffect
    {
        public ushort Type;
        public uint Length;
        public ushort LargeMagnitude;
        public ushort SmallMagnitude;
    }
}
