using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal readonly struct VehicleDimensions
    {
        public VehicleDimensions(float widthM, float lengthM)
        {
            WidthM = widthM;
            LengthM = lengthM;
        }

        public float WidthM { get; }
        public float LengthM { get; }
    }

}
