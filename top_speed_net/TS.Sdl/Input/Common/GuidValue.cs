using System;
using System.Runtime.InteropServices;

namespace TS.Sdl.Input
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GuidValue
    {
        public ulong Low;
        public ulong High;

        public static GuidValue FromGuid(Guid value)
        {
            var bytes = value.ToByteArray();
            return new GuidValue
            {
                Low = BitConverter.ToUInt64(bytes, 0),
                High = BitConverter.ToUInt64(bytes, 8)
            };
        }

        public Guid ToGuid()
        {
            var bytes = new byte[16];
            Array.Copy(BitConverter.GetBytes(Low), 0, bytes, 0, 8);
            Array.Copy(BitConverter.GetBytes(High), 0, bytes, 8, 8);
            return new Guid(bytes);
        }
    }
}
