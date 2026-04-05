using System;

namespace TopSpeed.Speech.Prism
{
    internal sealed class PrismException : Exception
    {
        public PrismException(Error error)
            : base(Native.ErrorString(error) ?? error.ToString())
        {
            Error = error;
        }

        public Error Error { get; }
    }
}
