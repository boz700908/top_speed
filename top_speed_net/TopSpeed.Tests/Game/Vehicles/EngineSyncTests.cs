using TopSpeed.Physics.Torque;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class EngineSyncTests
    {
        [Fact]
        public void SyncFromSpeed_DisengagedAtStandstill_HoldsConfiguredIdle()
        {
            var model = BuildModel(idleRpm: 800f, engineBrakingTorqueNm: 280f, engineFrictionTorqueNm: 20f);
            model.StartEngine();

            for (var i = 0; i < 360; i++)
            {
                model.SyncFromSpeed(
                    speedGameUnits: 0f,
                    gear: 1,
                    elapsed: 1f / 60f,
                    throttleInput: 0,
                    inReverse: false,
                    couplingMode: EngineCouplingMode.Disengaged,
                    couplingFactor: 0f);
            }

            Assert.InRange(model.Rpm, 780f, 820f);
        }

        [Fact]
        public void SyncFromSpeed_DisengagedOffThrottle_FreeRevDecaysNoticeably()
        {
            var model = BuildModel(idleRpm: 800f, engineBrakingTorqueNm: 280f, engineFrictionTorqueNm: 20f);
            model.StartEngine();
            model.OverrideRpm(4500f);

            for (var i = 0; i < 30; i++)
            {
                model.SyncFromSpeed(
                    speedGameUnits: 0f,
                    gear: 1,
                    elapsed: 1f / 60f,
                    throttleInput: 0,
                    inReverse: false,
                    couplingMode: EngineCouplingMode.Disengaged,
                    couplingFactor: 0f);
            }

            Assert.True(model.Rpm < 3600f);
        }

        [Fact]
        public void SyncFromSpeed_StrictModel_DisengagedOffThrottle_DecaysFaster()
        {
            var model = BuildModel(
                idleRpm: 800f,
                engineBrakingTorqueNm: 280f,
                engineFrictionTorqueNm: 20f,
                useStrictEngineClutchModel: true);
            model.StartEngine();
            model.OverrideRpm(4500f);

            for (var i = 0; i < 30; i++)
            {
                model.SyncFromSpeed(
                    speedGameUnits: 0f,
                    gear: 1,
                    elapsed: 1f / 60f,
                    throttleInput: 0,
                    inReverse: false,
                    couplingMode: EngineCouplingMode.Disengaged,
                    couplingFactor: 0f);
            }

            Assert.True(model.Rpm < 3300f);
        }

        private static EngineModel BuildModel(
            float idleRpm,
            float engineBrakingTorqueNm,
            float engineFrictionTorqueNm,
            bool useStrictEngineClutchModel = false)
        {
            var torqueCurve = CurveFactory.FromLegacy(
                idleRpm: idleRpm,
                revLimiter: 6500f,
                peakTorqueRpm: 3500f,
                idleTorqueNm: 100f,
                peakTorqueNm: 220f,
                redlineTorqueNm: 140f);

            return new EngineModel(
                idleRpm: idleRpm,
                maxRpm: 7000f,
                revLimiter: 6500f,
                autoShiftRpm: 0f,
                engineBraking: 0.32f,
                topSpeedKmh: 192f,
                finalDriveRatio: 3.59f,
                tireCircumferenceM: 2.0f,
                gearCount: 6,
                gearRatios: new[] { 3.74f, 2.11f, 1.45f, 1.16f, 0.94f, 0.79f },
                peakTorqueNm: 220f,
                peakTorqueRpm: 3500f,
                idleTorqueNm: 100f,
                redlineTorqueNm: 140f,
                engineBrakingTorqueNm: engineBrakingTorqueNm,
                powerFactor: 0.8f,
                engineInertiaKgm2: 0.24f,
                engineFrictionTorqueNm: engineFrictionTorqueNm,
                drivelineCouplingRate: 12f,
                torqueCurve: torqueCurve,
                useStrictEngineClutchModel: useStrictEngineClutchModel,
                engineFrictionCoulombNm: 22f,
                engineFrictionViscousNmPerRadS: 0.012f,
                enginePumpingLossNmAtClosedThrottle: 92f,
                engineAccessoryTorqueNm: 8f,
                idleTargetRpm: idleRpm,
                idleMaxCorrectionTorqueNm: 180f,
                idleControlKp: 0.08f,
                idleControlKi: 0.22f,
                clutchCapacityNm: 1200f,
                clutchEngageRatePerS: 14f,
                clutchReleaseRatePerS: 20f,
                clutchDragTorqueNm: 30f,
                launchTargetSlipRpm: 350f);
        }
    }
}
