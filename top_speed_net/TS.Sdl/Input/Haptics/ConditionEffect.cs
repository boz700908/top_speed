using System.Runtime.InteropServices;

namespace TS.Sdl.Input
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HapticConditionEffect
    {
        public ushort Type;
        public HapticDirection Direction;
        public uint Length;
        public ushort Delay;
        public ushort Button;
        public ushort Interval;
        public fixed ushort RightSat[3];
        public fixed ushort LeftSat[3];
        public fixed short RightCoeff[3];
        public fixed short LeftCoeff[3];
        public fixed ushort Deadband[3];
        public fixed short Center[3];

        public void SetAxis(
            int axis,
            ushort rightSat,
            ushort leftSat,
            short rightCoeff,
            short leftCoeff,
            ushort deadband = 0,
            short center = 0)
        {
            if (axis < 0 || axis > 2)
                return;

            RightSat[axis] = rightSat;
            LeftSat[axis] = leftSat;
            RightCoeff[axis] = rightCoeff;
            LeftCoeff[axis] = leftCoeff;
            Deadband[axis] = deadband;
            Center[axis] = center;
        }
    }
}
