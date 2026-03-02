using System;

namespace TopSpeed.Vehicles
{
    public static partial class OfficialVehicleCatalog
    {
        private static TransmissionPolicy Policy(
            int intendedTopSpeedGear,
            bool allowOverdriveAboveGameTopSpeed,
            float[]? upshiftCooldownBySourceGear = null,
            float upshiftRpmFraction = 0.92f,
            float downshiftRpmFraction = 0.35f,
            float upshiftHysteresis = 0.05f,
            float baseAutoShiftCooldownSeconds = 0.15f,
            float minUpshiftNetAccelerationMps2 = -0.05f,
            float topSpeedPursuitSpeedFraction = 0.97f)
        {
            return new TransmissionPolicy(
                intendedTopSpeedGear: intendedTopSpeedGear,
                allowOverdriveAboveGameTopSpeed: allowOverdriveAboveGameTopSpeed,
                upshiftRpmFraction: upshiftRpmFraction,
                downshiftRpmFraction: downshiftRpmFraction,
                upshiftHysteresis: upshiftHysteresis,
                baseAutoShiftCooldownSeconds: baseAutoShiftCooldownSeconds,
                minUpshiftNetAccelerationMps2: minUpshiftNetAccelerationMps2,
                topSpeedPursuitSpeedFraction: topSpeedPursuitSpeedFraction,
                preferIntendedTopSpeedGearNearLimit: true,
                upshiftCooldownBySourceGear: upshiftCooldownBySourceGear);
        }

        private static float TireCircumferenceM(int widthMm, int aspectPercent, int rimInches)
        {
            var sidewallMm = widthMm * (aspectPercent / 100f);
            var diameterMm = (rimInches * 25.4f) + (2f * sidewallMm);
            return (float)(Math.PI * (diameterMm / 1000f));
        }
    }
}
