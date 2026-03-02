using System;

namespace TopSpeed.Server.Logging
{
    internal static class ConsoleSink
    {
        public static bool WriteLine(string text)
        {
            try
            {
                Console.WriteLine(text);
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
        }
    }
}
