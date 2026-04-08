using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Torque;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class PowertrainResistanceBehaviorTests
{
    [Fact]
    public void BuildDefaults_ShouldFillExplicitResistanceValues()
    {
        var build = PowertrainBuild.Create(
            new BuildInput(
                massKg: 1450f,
                drivetrainEfficiency: 0.86f,
                engineBrakingTorqueNm: 240f,
                tireGripCoefficient: 0.92f,
                brakeStrength: 1.0f,
                wheelRadiusM: 0.32f,
                engineBraking: 0.28f,
                idleRpm: 800f,
                revLimiter: 6500f,
                finalDriveRatio: 4.0f,
                powerFactor: 0.75f,
                peakTorqueNm: 260f,
                peakTorqueRpm: 3200f,
                idleTorqueNm: 90f,
                redlineTorqueNm: 150f,
                dragCoefficient: 0.28f,
                frontalAreaM2: 2.10f,
                sideAreaM2: -1f,
                rollingResistanceCoefficient: 0.013f,
                rollingResistanceSpeedFactor: -1f,
                wheelSideDragBaseN: -1f,
                wheelSideDragLinearNPerMps: -1f,
                launchRpm: 2200f,
                reversePowerFactor: 0.55f,
                reverseGearRatio: 3.2f,
                reverseMaxSpeedKph: 35f,
                engineInertiaKgm2: 0.24f,
                engineFrictionTorqueNm: 20f,
                drivelineCouplingRate: 12f,
                gears: 6,
                torqueCurve: CurveFactory.FromLegacy(800f, 6500f, 3200f, 90f, 260f, 150f)));

        build.Powertrain.SideAreaM2.Should().BeApproximately(3.78f, 0.0001f);
        build.Powertrain.RollingResistanceSpeedFactor.Should().BeApproximately(0.01f, 0.0001f);
        build.WheelSideDragBaseN.Should().BeApproximately(0f, 0.0001f);
        build.WheelSideDragLinearNPerMps.Should().BeApproximately(0f, 0.0001f);
        build.CoupledDrivelineDragNm.Should().BeApproximately(18f, 0.0001f);
        build.CoupledDrivelineViscousDragNmPerKrpm.Should().BeApproximately(6f, 0.0001f);
    }

    [Fact]
    public void ExplicitWheelSideLosses_ShouldIncreaseNeutralCoastOverAeroAndRollingOnly()
    {
        var build = BuildWheelLossSample();
        const float speedMps = 100f / 3.6f;

        var passiveOnly = Calculator.AerodynamicDecelKph(build.Powertrain, speedMps, ResistanceEnvironment.Calm)
            + Calculator.RollingResistanceDecelKph(build.Powertrain, speedMps, 1f);
        var neutral = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                build.Powertrain,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 0f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear: 4,
                inReverse: false,
                isNeutral: true,
                transmissionType: TransmissionType.Manual,
                drivelineCouplingFactor: 0f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: 2500f,
                requestDrive: false,
                requestBrake: false,
                applyEngineBraking: false,
                resistanceEnvironment: ResistanceEnvironment.Calm));

        neutral.WheelSideDragDecelKph.Should().BeGreaterThan(0.1f);
        neutral.TotalDecelKph.Should().BeGreaterThan(passiveOnly + 0.1f);
    }

    [Fact]
    public void ClutchDisengagedInGear_ShouldRetainTransmissionDrag_WithoutEngineBraking()
    {
        var build = BuildWheelLossSample();
        const float speedMps = 100f / 3.6f;
        const int gear = 4;
        const float rpm = 3000f;

        var neutral = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                build.Powertrain,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 0f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear,
                inReverse: false,
                isNeutral: true,
                transmissionType: TransmissionType.Manual,
                drivelineCouplingFactor: 0f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: rpm,
                requestDrive: false,
                requestBrake: false,
                applyEngineBraking: false,
                resistanceEnvironment: ResistanceEnvironment.Calm,
                gearPathEngaged: false));

        var clutchDisengagedInGear = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                build.Powertrain,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 0f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear,
                inReverse: false,
                isNeutral: false,
                transmissionType: TransmissionType.Manual,
                drivelineCouplingFactor: 0f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: rpm,
                requestDrive: false,
                requestBrake: false,
                applyEngineBraking: false,
                resistanceEnvironment: ResistanceEnvironment.Calm,
                gearPathEngaged: true));

        clutchDisengagedInGear.EngineBrakeDecelKph.Should().BeApproximately(0f, 0.0001f);
        clutchDisengagedInGear.CoupledDrivelineDragDecelKph.Should().BeGreaterThan(0.1f);
        clutchDisengagedInGear.WheelSideDragDecelKph.Should().BeGreaterThan(0.1f);
        clutchDisengagedInGear.TotalDecelKph.Should().BeGreaterThan(neutral.TotalDecelKph + 0.1f);
    }

    [Fact]
    public void MiniCooperClutchDisengagedInThird_ShouldRemainStrongerThanNeutralAtModerateSpeed()
    {
        var spec = OfficialVehicleCatalog.Get((int)CarType.Vehicle4);
        var config = PowertrainHarness.BuildConfig(spec);
        const float speedMps = 70f / 3.6f;
        const int gear = 3;

        var neutral = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                config,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 0f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear,
                inReverse: false,
                isNeutral: true,
                transmissionType: TransmissionType.Manual,
                drivelineCouplingFactor: 0f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: config.IdleRpm,
                requestDrive: false,
                requestBrake: false,
                applyEngineBraking: false,
                resistanceEnvironment: ResistanceEnvironment.Calm,
                gearPathEngaged: false));

        var clutchHeld = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                config,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 0f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear,
                inReverse: false,
                isNeutral: false,
                transmissionType: TransmissionType.Manual,
                drivelineCouplingFactor: 0f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: config.IdleRpm,
                requestDrive: false,
                requestBrake: false,
                applyEngineBraking: false,
                resistanceEnvironment: ResistanceEnvironment.Calm,
                gearPathEngaged: true));

        clutchHeld.EngineBrakeDecelKph.Should().BeApproximately(0f, 0.0001f);
        clutchHeld.CoupledDrivelineDragDecelKph.Should().BeGreaterThan(0.1f);
        clutchHeld.TotalDecelKph.Should().BeGreaterThan(neutral.TotalDecelKph + 0.1f);
    }

    [Fact]
    public void ClosedThrottleCoupledCoast_ShouldRemainStrongerThanFreeCoast()
    {
        var build = BuildWheelLossSample();
        const float speedMps = 100f / 3.6f;

        var neutralCoast = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                build.Powertrain,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 0f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear: 4,
                inReverse: false,
                isNeutral: true,
                transmissionType: TransmissionType.Manual,
                drivelineCouplingFactor: 0f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: 2500f,
                requestDrive: false,
                requestBrake: false,
                applyEngineBraking: false,
                resistanceEnvironment: ResistanceEnvironment.Calm));

        var coupledCoast = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                build.Powertrain,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 0f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear: 4,
                inReverse: false,
                isNeutral: false,
                transmissionType: TransmissionType.Manual,
                drivelineCouplingFactor: 1f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: 3000f,
                requestDrive: false,
                requestBrake: false,
                applyEngineBraking: true,
                resistanceEnvironment: ResistanceEnvironment.Calm));

        coupledCoast.EngineBrakeDecelKph.Should().BeGreaterThan(0.1f);
        coupledCoast.CoupledDrivelineDragDecelKph.Should().BeGreaterThan(0.1f);
        coupledCoast.TotalDecelKph.Should().BeGreaterThan(neutralCoast.TotalDecelKph + 0.1f);
    }

    [Fact]
    public void BrakeSurfaceModifier_ShouldScaleBrakeDecelWithoutLegacyBaseline()
    {
        var build = BuildWheelLossSample();
        const float speedMps = 60f / 3.6f;

        var lowGripBrake = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                build.Powertrain,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 1f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 0.5f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear: 3,
                inReverse: false,
                isNeutral: false,
                transmissionType: TransmissionType.Manual,
                drivelineCouplingFactor: 1f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: 2200f,
                requestDrive: false,
                requestBrake: true,
                applyEngineBraking: false,
                resistanceEnvironment: ResistanceEnvironment.Calm));

        var highGripBrake = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                build.Powertrain,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 1f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1.5f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear: 3,
                inReverse: false,
                isNeutral: false,
                transmissionType: TransmissionType.Manual,
                drivelineCouplingFactor: 1f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: 2200f,
                requestDrive: false,
                requestBrake: true,
                applyEngineBraking: false,
                resistanceEnvironment: ResistanceEnvironment.Calm));

        highGripBrake.BrakeDecelKph.Should().BeGreaterThan(lowGripBrake.BrakeDecelKph + 0.1f);
        highGripBrake.BrakeDecelKph.Should().BeApproximately(lowGripBrake.BrakeDecelKph * 3f, 0.01f);
    }

    [Fact]
    public void PassiveCoastNearZero_ShouldSnapToZeroWithoutCreep()
    {
        var build = BuildWheelLossSample();
        const float startSpeedKph = 0.05f;

        var result = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                build.Powertrain,
                elapsedSeconds: 0.1f,
                speedMps: startSpeedKph / 3.6f,
                throttle: 0f,
                brake: 0f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear: 2,
                inReverse: false,
                isNeutral: true,
                transmissionType: TransmissionType.Manual,
                drivelineCouplingFactor: 0f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: build.Powertrain.IdleRpm,
                requestDrive: false,
                requestBrake: false,
                applyEngineBraking: false,
                resistanceEnvironment: ResistanceEnvironment.Calm));

        result.SpeedDeltaKph.Should().BeApproximately(-startSpeedKph, 0.0001f);
    }

    [Fact]
    public void AtcSlipCoast_ShouldTransferLessEngineBrakeThanLockup()
    {
        var build = BuildWheelLossSample();
        const float speedMps = 100f / 3.6f;

        var slipping = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                build.Powertrain,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 0f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear: 4,
                inReverse: false,
                isNeutral: false,
                transmissionType: TransmissionType.Atc,
                drivelineCouplingFactor: 0.42f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: 2800f,
                requestDrive: false,
                requestBrake: false,
                applyEngineBraking: true,
                resistanceEnvironment: ResistanceEnvironment.Calm));

        var locked = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                build.Powertrain,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 0f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear: 4,
                inReverse: false,
                isNeutral: false,
                transmissionType: TransmissionType.Atc,
                drivelineCouplingFactor: 1f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: 2800f,
                requestDrive: false,
                requestBrake: false,
                applyEngineBraking: true,
                resistanceEnvironment: ResistanceEnvironment.Calm));

        slipping.EngineBrakeDecelKph.Should().BeGreaterThan(0f);
        slipping.EngineBrakeDecelKph.Should().BeLessThan(locked.EngineBrakeDecelKph);
        slipping.CoupledDrivelineDragDecelKph.Should().BeLessThan(locked.CoupledDrivelineDragDecelKph);
    }

    [Fact]
    public void DctOverlapCoast_ShouldCarryMoreDrivelineDragThanAtcAtSameCoupling()
    {
        var build = BuildWheelLossSample();
        const float speedMps = 100f / 3.6f;
        const float coupling = 0.48f;

        var atc = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                build.Powertrain,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 0f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear: 4,
                inReverse: false,
                isNeutral: false,
                transmissionType: TransmissionType.Atc,
                drivelineCouplingFactor: coupling,
                creepAccelerationMps2: 0f,
                currentEngineRpm: 3000f,
                requestDrive: false,
                requestBrake: false,
                applyEngineBraking: true,
                resistanceEnvironment: ResistanceEnvironment.Calm));

        var dct = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                build.Powertrain,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 0f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear: 4,
                inReverse: false,
                isNeutral: false,
                transmissionType: TransmissionType.Dct,
                drivelineCouplingFactor: coupling,
                creepAccelerationMps2: 0f,
                currentEngineRpm: 3000f,
                requestDrive: false,
                requestBrake: false,
                applyEngineBraking: true,
                resistanceEnvironment: ResistanceEnvironment.Calm));

        dct.CoupledDrivelineDragDecelKph.Should().BeGreaterThan(atc.CoupledDrivelineDragDecelKph + 0.05f);
        dct.EngineBrakeDecelKph.Should().BeApproximately(atc.EngineBrakeDecelKph, 0.0001f);
    }

    [Fact]
    public void CvtSlipCoast_ShouldBlendTowardLockedCoast()
    {
        var build = BuildWheelLossSample();
        const float speedMps = 100f / 3.6f;

        var slipping = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                build.Powertrain,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 0f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear: 4,
                inReverse: false,
                isNeutral: false,
                transmissionType: TransmissionType.Cvt,
                drivelineCouplingFactor: 0.35f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: 2600f,
                requestDrive: false,
                requestBrake: false,
                applyEngineBraking: true,
                resistanceEnvironment: ResistanceEnvironment.Calm));

        var locked = LongitudinalStep.Compute(
            new LongitudinalStepInput(
                build.Powertrain,
                elapsedSeconds: 0.1f,
                speedMps,
                throttle: 0f,
                brake: 0f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear: 4,
                inReverse: false,
                isNeutral: false,
                transmissionType: TransmissionType.Cvt,
                drivelineCouplingFactor: 1f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: 2600f,
                requestDrive: false,
                requestBrake: false,
                applyEngineBraking: true,
                resistanceEnvironment: ResistanceEnvironment.Calm));

        slipping.EngineBrakeDecelKph.Should().BeGreaterThan(0f);
        slipping.EngineBrakeDecelKph.Should().BeLessThan(locked.EngineBrakeDecelKph);
        slipping.CoupledDrivelineDragDecelKph.Should().BeGreaterThan(0f);
        slipping.CoupledDrivelineDragDecelKph.Should().BeLessThan(locked.CoupledDrivelineDragDecelKph);
    }

    private static BuildResult BuildWheelLossSample()
    {
        return PowertrainBuild.Create(
            new BuildInput(
                massKg: 1450f,
                drivetrainEfficiency: 0.86f,
                engineBrakingTorqueNm: 240f,
                tireGripCoefficient: 0.92f,
                brakeStrength: 1.0f,
                wheelRadiusM: 0.32f,
                engineBraking: 0.28f,
                idleRpm: 800f,
                revLimiter: 6500f,
                finalDriveRatio: 4.0f,
                powerFactor: 0.75f,
                peakTorqueNm: 260f,
                peakTorqueRpm: 3200f,
                idleTorqueNm: 90f,
                redlineTorqueNm: 150f,
                dragCoefficient: 0.28f,
                frontalAreaM2: 2.10f,
                sideAreaM2: -1f,
                rollingResistanceCoefficient: 0.013f,
                rollingResistanceSpeedFactor: -1f,
                wheelSideDragBaseN: 90f,
                wheelSideDragLinearNPerMps: 3.0f,
                launchRpm: 2200f,
                reversePowerFactor: 0.55f,
                reverseGearRatio: 3.2f,
                reverseMaxSpeedKph: 35f,
                engineInertiaKgm2: 0.24f,
                engineFrictionTorqueNm: 20f,
                drivelineCouplingRate: 12f,
                gears: 6,
                torqueCurve: CurveFactory.FromLegacy(800f, 6500f, 3200f, 90f, 260f, 150f)));
    }
}
