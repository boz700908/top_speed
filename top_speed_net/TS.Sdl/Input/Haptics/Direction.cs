using System.Runtime.InteropServices;

namespace TS.Sdl.Input
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HapticDirection
    {
        public byte Type;
        public fixed int Dir[3];

        public static HapticDirection SteeringAxis()
        {
            return new HapticDirection { Type = (byte)HapticDirectionType.SteeringAxis };
        }

        public static HapticDirection Polar(int hundredthDegrees)
        {
            var direction = new HapticDirection { Type = (byte)HapticDirectionType.Polar };
            direction.Dir[0] = hundredthDegrees;
            return direction;
        }

        public static HapticDirection Cartesian(int x, int y, int z = 0)
        {
            var direction = new HapticDirection { Type = (byte)HapticDirectionType.Cartesian };
            direction.Dir[0] = x;
            direction.Dir[1] = y;
            direction.Dir[2] = z;
            return direction;
        }
    }
}
