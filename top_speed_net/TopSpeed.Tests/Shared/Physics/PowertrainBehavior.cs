using System;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Torque;
using Xunit;

namespace TopSpeed.Tests.Physics
{
    [Trait("Category", "SharedPhysics")]
    public sealed class PowertrainBehaviorTests
    {
        [Fact]
        public void DriveRpm_AtStandstill_TracksLaunchTarget()
        {
            var config = BuildConfiguration();
            var rpm = Calculator.DriveRpm(
                config,
                gear: 1,
                speedMps: 0f,
                throttle: 1f,
                inReverse: false);

            Assert.Equal(config.LaunchRpm, rpm, 3);
        }

        [Fact]
        public void DriveRpm_AtExtremeSpeed_ClampsToRevLimiter()
        {
            var config = BuildConfiguration();
            var rpm = Calculator.DriveRpm(
                config,
                gear: 1,
                speedMps: 400f,
                throttle: 1f,
                inReverse: false);

            Assert.Equal(config.RevLimiter, rpm, 3);
        }

        [Fact]
        public void DriveAccel_WithZeroLongitudinalGrip_IsNonPositive()
        {
            var config = BuildConfiguration();
            var accel = Calculator.DriveAccel(
                config,
                gear: 2,
                speedMps: 25f,
                throttle: 0.9f,
                surfaceTractionModifier: 1f,
                longitudinalGripFactor: 0f);

            Assert.True(accel <= 0f);
        }

        [Fact]
        public void ReverseAccel_IsLowerThanForwardAccel_ForSameInput()
        {
            var config = BuildConfiguration();
            var speedMps = 12f;
            var throttle = 0.8f;

            var forward = Calculator.DriveAccel(
                config,
                gear: 1,
                speedMps: speedMps,
                throttle: throttle,
                surfaceTractionModifier: 1f,
                longitudinalGripFactor: 1f);
            var reverse = Calculator.ReverseAccel(
                config,
                speedMps: speedMps,
                throttle: throttle,
                surfaceTractionModifier: 1f,
                longitudinalGripFactor: 1f);

            Assert.True(forward > reverse);
        }

        [Fact]
        public void EngineBrakeDecel_RisesWithHigherEngineRpm()
        {
            var config = BuildConfiguration();

            var lowRpmDecel = Calculator.EngineBrakeDecelKph(
                config,
                gear: 2,
                inReverse: false,
                speedMps: 8f,
                surfaceDecelerationModifier: 1f,
                currentEngineRpm: 1400f);
            var highRpmDecel = Calculator.EngineBrakeDecelKph(
                config,
                gear: 2,
                inReverse: false,
                speedMps: 8f,
                surfaceDecelerationModifier: 1f,
                currentEngineRpm: 5200f);

            Assert.True(highRpmDecel > lowRpmDecel);
        }

        [Fact]
        public void EngineBrakeDecel_AtLowRpm_RemainsPositive()
        {
            var config = BuildConfiguration();
            var decel = Calculator.EngineBrakeDecelKph(
                config,
                gear: 3,
                inReverse: false,
                speedMps: 6f,
                surfaceDecelerationModifier: 1f,
                currentEngineRpm: config.IdleRpm);

            Assert.True(decel > 0f);
        }

        [Fact]
        public void EngineBrakeDecel_IncreasesWithEngineFrictionTorque()
        {
            var lowFriction = BuildConfiguration(engineFrictionTorqueNm: 0f);
            var highFriction = BuildConfiguration(engineFrictionTorqueNm: 40f);

            var low = Calculator.EngineBrakeDecelKph(
                lowFriction,
                gear: 3,
                inReverse: false,
                speedMps: 12f,
                surfaceDecelerationModifier: 1f,
                currentEngineRpm: 2200f);
            var high = Calculator.EngineBrakeDecelKph(
                highFriction,
                gear: 3,
                inReverse: false,
                speedMps: 12f,
                surfaceDecelerationModifier: 1f,
                currentEngineRpm: 2200f);

            Assert.True(high > low);
        }

        [Fact]
        public void EngineBrakeDecel_StrictModel_IncreasesWithViscousLoss()
        {
            var lowViscous = BuildConfiguration(
                useStrictEngineClutchModel: true,
                engineFrictionViscousNmPerRadS: 0.003f);
            var highViscous = BuildConfiguration(
                useStrictEngineClutchModel: true,
                engineFrictionViscousNmPerRadS: 0.03f);

            var low = Calculator.EngineBrakeDecelKph(
                lowViscous,
                gear: 3,
                inReverse: false,
                speedMps: 16f,
                surfaceDecelerationModifier: 1f,
                currentEngineRpm: 3200f);
            var high = Calculator.EngineBrakeDecelKph(
                highViscous,
                gear: 3,
                inReverse: false,
                speedMps: 16f,
                surfaceDecelerationModifier: 1f,
                currentEngineRpm: 3200f);

            Assert.True(high > low);
        }

        [Fact]
        public void ResistiveDecel_IsPositiveAtStandstill_AndIncreasesWithSpeed()
        {
            var config = BuildConfiguration();

            var standstill = Calculator.ResistiveDecelKph(config, speedMps: 0f, surfaceDecelerationModifier: 1f);
            var highway = Calculator.ResistiveDecelKph(config, speedMps: 35f, surfaceDecelerationModifier: 1f);

            Assert.True(standstill > 0f);
            Assert.True(highway > standstill);
        }

        [Fact]
        public void ResistiveDecel_RollingComponent_TracksSurfaceDecelerationModifier()
        {
            var config = BuildConfiguration();
            const float speedMps = 5f;

            var lowSurface = Calculator.ResistiveDecelKph(config, speedMps, surfaceDecelerationModifier: 0.5f);
            var baseSurface = Calculator.ResistiveDecelKph(config, speedMps, surfaceDecelerationModifier: 1f);
            var highSurface = Calculator.ResistiveDecelKph(config, speedMps, surfaceDecelerationModifier: 1.5f);

            Assert.True(baseSurface > lowSurface);
            Assert.True(highSurface > baseSurface);
        }

        [Fact]
        public void ResistiveDecel_StrictModel_IsSurfaceIndependent()
        {
            var config = BuildConfiguration(
                useStrictEngineClutchModel: true,
                rollingResistanceSpeedGainPerMps: 0.01f,
                drivelineCoastTorqueNm: 15f);
            const float speedMps = 8f;
            const float currentEngineRpm = 1800f;

            var lowSurface = Calculator.ResistiveDecelKph(config, speedMps, surfaceDecelerationModifier: 0.5f, currentEngineRpm: currentEngineRpm);
            var baseSurface = Calculator.ResistiveDecelKph(config, speedMps, surfaceDecelerationModifier: 1f, currentEngineRpm: currentEngineRpm);
            var highSurface = Calculator.ResistiveDecelKph(config, speedMps, surfaceDecelerationModifier: 1.5f, currentEngineRpm: currentEngineRpm);

            Assert.InRange(Math.Abs(baseSurface - lowSurface), 0f, 0.0001f);
            Assert.InRange(Math.Abs(highSurface - baseSurface), 0f, 0.0001f);
        }

        [Fact]
        public void ResistiveDecel_StrictModel_LowSpeedSettleAddsDecel()
        {
            var config = BuildConfiguration(
                useStrictEngineClutchModel: true,
                coastStopSpeedKph: 4f,
                coastStopDecelKphps: 1.2f);
            const float rpm = 900f;

            var nearStop = Calculator.ResistiveDecelKph(config, speedMps: 0.2f, surfaceDecelerationModifier: 1f, currentEngineRpm: rpm);
            var aboveSettleBand = Calculator.ResistiveDecelKph(config, speedMps: 3.0f, surfaceDecelerationModifier: 1f, currentEngineRpm: rpm);

            Assert.True(nearStop > aboveSettleBand);
        }

        private static Config BuildConfiguration(
            float engineBrakingTorqueNm = 300f,
            float engineBraking = 0.3f,
            float engineFrictionTorqueNm = 20f,
            bool useStrictEngineClutchModel = false,
            float engineFrictionViscousNmPerRadS = 0.012f,
            float rollingResistanceSpeedGainPerMps = 0f,
            float drivelineCoastTorqueNm = 0f,
            float coastStopSpeedKph = 3f,
            float coastStopDecelKphps = 0.7f)
        {
            var torqueCurve = CurveFactory.FromLegacy(
                idleRpm: 900f,
                revLimiter: 7600f,
                peakTorqueRpm: 3600f,
                idleTorqueNm: 180f,
                peakTorqueNm: 650f,
                redlineTorqueNm: 360f);

            return new Config(
                massKg: 1650f,
                drivetrainEfficiency: 0.85f,
                engineBrakingTorqueNm: engineBrakingTorqueNm,
                tireGripCoefficient: 1.0f,
                brakeStrength: 1.0f,
                wheelRadiusM: 0.34f,
                engineBraking: engineBraking,
                idleRpm: 900f,
                revLimiter: 7600f,
                finalDriveRatio: 3.70f,
                powerFactor: 0.7f,
                peakTorqueNm: 650f,
                peakTorqueRpm: 3600f,
                idleTorqueNm: 180f,
                redlineTorqueNm: 360f,
                dragCoefficient: 0.30f,
                frontalAreaM2: 2.2f,
                rollingResistanceCoefficient: 0.015f,
                launchRpm: 2400f,
                reversePowerFactor: 0.55f,
                reverseGearRatio: 3.2f,
                engineInertiaKgm2: 0.24f,
                engineFrictionTorqueNm: engineFrictionTorqueNm,
                drivelineCouplingRate: 12f,
                gears: 6,
                gearRatios: new[] { 3.5f, 2.2f, 1.5f, 1.2f, 1.0f, 0.85f },
                torqueCurve: torqueCurve,
                useStrictEngineClutchModel: useStrictEngineClutchModel,
                engineFrictionCoulombNm: 22f,
                engineFrictionViscousNmPerRadS: engineFrictionViscousNmPerRadS,
                enginePumpingLossNmAtClosedThrottle: 90f,
                engineAccessoryTorqueNm: 8f,
                idleTargetRpm: 900f,
                idleMaxCorrectionTorqueNm: 180f,
                idleControlKp: 0.08f,
                idleControlKi: 0.22f,
                clutchCapacityNm: 1200f,
                clutchEngageRatePerS: 14f,
                clutchReleaseRatePerS: 20f,
                clutchDragTorqueNm: 30f,
                launchTargetSlipRpm: 350f,
                airDensityKgPerM3: 1.225f,
                rollingResistanceSpeedGainPerMps: rollingResistanceSpeedGainPerMps,
                drivelineCoastTorqueNm: drivelineCoastTorqueNm,
                drivelineCoastViscousNmPerRadS: 0.01f,
                coastStopSpeedKph: coastStopSpeedKph,
                coastStopDecelKphps: coastStopDecelKphps);
        }
    }
}
