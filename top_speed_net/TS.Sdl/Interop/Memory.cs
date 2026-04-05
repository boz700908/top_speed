using System;
using System.Runtime.InteropServices;

namespace TS.Sdl.Interop
{
    internal static class Memory
    {
        public static T[] ReadArray<T>(IntPtr pointer, int count)
            where T : struct
        {
            if (pointer == IntPtr.Zero || count <= 0)
                return Array.Empty<T>();

            var size = Marshal.SizeOf(typeof(T));
            var values = new T[count];
            for (var i = 0; i < count; i++)
            {
                var current = IntPtr.Add(pointer, i * size);
                values[i] = Marshal.PtrToStructure<T>(current);
            }

            return values;
        }
    }
}
