namespace TopSpeed.Vehicles
{
    public static partial class OfficialVehicleCatalog
    {
        public const int VehicleCount = 12;

        public static OfficialVehicleSpec Get(int vehicleIndex)
        {
            if (vehicleIndex < 0 || vehicleIndex >= Vehicles.Length)
                vehicleIndex = 0;
            return Vehicles[vehicleIndex];
        }
    }
}
