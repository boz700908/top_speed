using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TopSpeed.Speech.Prism
{
    internal static class Strings
    {
        public static T WithUtf8<T>(string text, Func<IntPtr, T> action)
        {
            var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            var buffer = Marshal.AllocHGlobal(bytes.Length + 1);

            try
            {
                Marshal.Copy(bytes, 0, buffer, bytes.Length);
                Marshal.WriteByte(buffer, bytes.Length, 0);
                return action(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public static string? FromUtf8(IntPtr value)
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
    }
}
