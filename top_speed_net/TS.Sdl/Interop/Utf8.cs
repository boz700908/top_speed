using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TS.Sdl.Interop
{
    internal static class Utf8
    {
        public static IntPtr ToNative(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return IntPtr.Zero;

            var bytes = Encoding.UTF8.GetBytes(value);
            var pointer = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            Marshal.WriteByte(pointer, bytes.Length, 0);
            return pointer;
        }

        public static string? FromNative(IntPtr value)
        {
            if (value == IntPtr.Zero)
                return null;

            var length = 0;
            while (Marshal.ReadByte(value, length) != 0)
                length++;

            if (length == 0)
                return string.Empty;

            var bytes = new byte[length];
            Marshal.Copy(value, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }

        public static string[] ReadStringArray(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
                return Array.Empty<string>();

            var values = new List<string>();
            var offset = 0;
            while (true)
            {
                var current = Marshal.ReadIntPtr(pointer, offset * IntPtr.Size);
                if (current == IntPtr.Zero)
                    break;

                values.Add(FromNative(current) ?? string.Empty);
                offset++;
            }

            return values.ToArray();
        }
    }
}
