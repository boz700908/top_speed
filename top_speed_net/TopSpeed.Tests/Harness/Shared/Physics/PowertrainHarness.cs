using System;
using System.Collections.Generic;
using System.Linq;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Torque;
using TopSpeed.Vehicles;

namespace TopSpeed.Tests
{
    internal static class PowertrainHarness
    {
        public static Config BuildConfig(OfficialVehicleSpec spec)
        {
            var torqueCurve = CurveFactory.FromLegacy(
                spec.IdleRpm,
                spec.RevLimiter,
                spec.PeakTorqueRpm,
                spec.IdleTorqueNm,
                spec.PeakTorqueNm,
                spec.RedlineTorqueNm);

            return PowertrainBuild.Create(new BuildInput(
                massKg: spec.MassKg,
                drivetrainEfficiency: spec.DrivetrainEfficiency,
                engineBrakingTorqueNm: spec.EngineBrakingTorqueNm,
                tireGripCoefficient: spec.TireGripCoefficient,
                brakeStrength: spec.BrakeStrength,
                wheelRadiusM: spec.TireCircumferenceM / (2.0f * (float)Math.PI),
                engineBraking: spec.EngineBraking,
                idleRpm: spec.IdleRpm,
                revLimiter: spec.RevLimiter,
                finalDriveRatio: spec.FinalDriveRatio,
                powerFactor: spec.PowerFactor,
                peakTorqueNm: spec.PeakTorqueNm,
                peakTorqueRpm: spec.PeakTorqueRpm,
                idleTorqueNm: spec.IdleTorqueNm,
                redlineTorqueNm: spec.RedlineTorqueNm,
                dragCoefficient: spec.DragCoefficient,
                frontalAreaM2: spec.FrontalAreaM2,
                sideAreaM2: spec.SideAreaM2 > 0f ? spec.SideAreaM2 : spec.FrontalAreaM2 * 1.8f,
                rollingResistanceCoefficient: spec.RollingResistanceCoefficient,
                rollingResistanceSpeedFactor: spec.RollingResistanceSpeedFactor >= 0f ? spec.RollingResistanceSpeedFactor : 0.01f,
                wheelSideDragBaseN: spec.WheelSideDragBaseN >= 0f ? spec.WheelSideDragBaseN : 0f,
                wheelSideDragLinearNPerMps: spec.WheelSideDragLinearNPerMps >= 0f ? spec.WheelSideDragLinearNPerMps : 0f,
                launchRpm: spec.LaunchRpm,
                reversePowerFactor: spec.ReversePowerFactor,
                reverseGearRatio: spec.ReverseGearRatio,
                reverseMaxSpeedKph: spec.ReverseMaxSpeedKph,
                engineInertiaKgm2: spec.EngineInertiaKgm2,
                engineFrictionTorqueNm: spec.EngineFrictionTorqueNm,
                drivelineCouplingRate: spec.DrivelineCouplingRate,
                gears: spec.Gears,
                gearRatios: spec.GearRatios,
                torqueCurve: torqueCurve,
                coupledDrivelineDragNm: spec.CoupledDrivelineDragNm >= 0f ? spec.CoupledDrivelineDragNm : 18f,
                coupledDrivelineViscousDragNmPerKrpm: spec.CoupledDrivelineViscousDragNmPerKrpm >= 0f ? spec.CoupledDrivelineViscousDragNmPerKrpm : 6f,
                frictionLinearNmPerKrpm: spec.FrictionLinearNmPerKrpm >= 0f ? spec.FrictionLinearNmPerKrpm : 0f,
                frictionQuadraticNmPerKrpm2: spec.FrictionQuadraticNmPerKrpm2 >= 0f ? spec.FrictionQuadraticNmPerKrpm2 : 0f,
                idleControlWindowRpm: spec.IdleControlWindowRpm >= 0f ? spec.IdleControlWindowRpm : 150f,
                idleControlGainNmPerRpm: spec.IdleControlGainNmPerRpm >= 0f ? spec.IdleControlGainNmPerRpm : 0.08f,
                minCoupledRiseIdleRpmPerSecond: spec.MinCoupledRiseIdleRpmPerSecond >= 0f ? spec.MinCoupledRiseIdleRpmPerSecond : 2200f,
                minCoupledRiseFullRpmPerSecond: spec.MinCoupledRiseFullRpmPerSecond >= 0f ? spec.MinCoupledRiseFullRpmPerSecond : 6200f,
                engineOverrunIdleLossFraction: spec.EngineOverrunIdleLossFraction >= 0f ? spec.EngineOverrunIdleLossFraction : 0.35f,
                overrunCurveExponent: spec.OverrunCurveExponent >= 0f ? spec.OverrunCurveExponent : 1f,
                engineBrakeTransferEfficiency: spec.EngineBrakeTransferEfficiency >= 0f ? spec.EngineBrakeTransferEfficiency : 0.68f)).Powertrain;
        }

        public static CoastTrace SimulateNeutralCoast(OfficialVehicleSpec spec, float startSpeedKph = 100f, float seconds = 8f)
        {
            var config = BuildConfig(spec);
            const float elapsed = 0.05f;
            var speedKph = startSpeedKph;
            var steps = (int)(seconds / elapsed);
            var samples = new List<CoastSample>();

            for (var i = 0; i < steps; i++)
            {
                var speedMps = speedKph / 3.6f;
                var result = LongitudinalStep.Compute(new LongitudinalStepInput(
                    config,
                    elapsed,
                    speedMps,
                    throttle: 0f,
                    brake: 0f,
                    surfaceTractionModifier: 1f,
                    surfaceBrakeModifier: 1f,
                    surfaceRollingResistanceModifier: 1f,
                    longitudinalGripFactor: 1f,
                    gear: config.Gears,
                    inReverse: false,
                    isNeutral: true,
                    transmissionType: spec.PrimaryTransmissionType,
                    drivelineCouplingFactor: 0f,
                    creepAccelerationMps2: 0f,
                    currentEngineRpm: config.IdleRpm,
                    requestDrive: false,
                    requestBrake: false,
                    applyEngineBraking: false,
                    resistanceEnvironment: ResistanceEnvironment.Calm));
                speedKph = Math.Max(0f, speedKph + result.SpeedDeltaKph);

                if (i % 20 == 0 || i == steps - 1)
                {
                    samples.Add(new CoastSample(
                        TimeSeconds: Rounding.F((i + 1) * elapsed, 2),
                        SpeedKph: Rounding.F(speedKph, 2),
                        AerodynamicDecelKph: Rounding.F(result.AerodynamicDecelKph),
                        RollingResistanceDecelKph: Rounding.F(result.RollingResistanceDecelKph),
                        WheelSideDragDecelKph: Rounding.F(result.WheelSideDragDecelKph)));
                }
            }

            return new CoastTrace(
                spec.Name,
                StartSpeedKph: startSpeedKph,
                FinalSpeedKph: Rounding.F(speedKph, 2),
                Samples: samples);
        }

        public static IReadOnlyList<VehicleCatalogSnapshot> BuildCatalogSnapshot()
        {
            return OfficialVehicleCatalog.Vehicles
                .Select(spec => new VehicleCatalogSnapshot(
                    spec.Name,
                    spec.PrimaryTransmissionType.ToString(),
                    spec.GearRatios.Length,
                    spec.TopSpeed,
                    TopGearKph: Rounding.F(GearTopSpeedKph(spec, spec.GearRatios.Length), 1),
                    PreviousGearKph: spec.GearRatios.Length > 1 ? Rounding.F(GearTopSpeedKph(spec, spec.GearRatios.Length - 1), 1) : 0f,
                    SideAreaM2: Rounding.F(spec.SideAreaM2 > 0f ? spec.SideAreaM2 : spec.FrontalAreaM2 * 1.8f),
                    RollingResistanceSpeedFactor: Rounding.F(spec.RollingResistanceSpeedFactor >= 0f ? spec.RollingResistanceSpeedFactor : 0.01f),
                    WheelSideDragBaseN: Rounding.F(spec.WheelSideDragBaseN >= 0f ? spec.WheelSideDragBaseN : 0f),
                    WheelSideDragLinearNPerMps: Rounding.F(spec.WheelSideDragLinearNPerMps >= 0f ? spec.WheelSideDragLinearNPerMps : 0f),
                    CoupledDrivelineDragNm: Rounding.F(spec.CoupledDrivelineDragNm >= 0f ? spec.CoupledDrivelineDragNm : 18f),
                    CoupledDrivelineViscousDragNmPerKrpm: Rounding.F(spec.CoupledDrivelineViscousDragNmPerKrpm >= 0f ? spec.CoupledDrivelineViscousDragNmPerKrpm : 6f)))
                .ToArray();
        }

        public static SpeedTrace SimulateTopGearPull(OfficialVehicleSpec spec, float seconds = 8f)
        {
            var startSpeedKph = Math.Max(60f, spec.TopSpeed * 0.70f);
            return SimulateSpeedTrace(spec, startSpeedKph, throttle: 1f, isNeutral: false, coupling: 1f, applyEngineBraking: false, seconds: seconds);
        }

        public static SpeedTrace SimulateHighSpeedNeutralCoast(OfficialVehicleSpec spec, float seconds = 8f)
        {
            var startSpeedKph = Math.Max(80f, spec.TopSpeed * 0.80f);
            return SimulateSpeedTrace(spec, startSpeedKph, throttle: 0f, isNeutral: true, coupling: 0f, applyEngineBraking: false, seconds: seconds);
        }

        public static SpeedTrace SimulateHighSpeedClosedThrottle(OfficialVehicleSpec spec, float seconds = 8f)
        {
            var startSpeedKph = Math.Max(80f, spec.TopSpeed * 0.80f);
            return SimulateSpeedTrace(spec, startSpeedKph, throttle: 0f, isNeutral: false, coupling: 1f, applyEngineBraking: true, seconds: seconds);
        }

        public static float GearTopSpeedKph(OfficialVehicleSpec spec, int gear)
        {
            var ratio = spec.GearRatios[gear - 1] * spec.FinalDriveRatio;
            var speedMps = (spec.RevLimiter / 60f) * spec.TireCircumferenceM / ratio;
            return speedMps * 3.6f;
        }

        private static SpeedTrace SimulateSpeedTrace(
            OfficialVehicleSpec spec,
            float startSpeedKph,
            float throttle,
            bool isNeutral,
            float coupling,
            bool applyEngineBraking,
            float seconds)
        {
            var config = BuildConfig(spec);
            var speedKph = startSpeedKph;
            var gear = spec.GearRatios.Length;
            const float elapsed = 0.05f;
            var steps = (int)(seconds / elapsed);

            for (var i = 0; i < steps; i++)
            {
                var speedMps = speedKph / 3.6f;
                var rpm = Clamp(Calculator.RpmAtSpeed(config, speedMps, gear), config.IdleRpm, config.RevLimiter);
                var result = LongitudinalStep.Compute(new LongitudinalStepInput(
                    config,
                    elapsed,
                    speedMps,
                    throttle,
                    0f,
                    1f,
                    1f,
                    1f,
                    1f,
                    gear,
                    false,
                    isNeutral,
                    spec.PrimaryTransmissionType,
                    coupling,
                    0f,
                    rpm,
                    throttle > 0f,
                    false,
                    applyEngineBraking,
                    ResistanceEnvironment.Calm));
                speedKph = Math.Max(0f, speedKph + result.SpeedDeltaKph);
            }

            var endRpm = isNeutral
                ? config.IdleRpm
                : Clamp(Calculator.RpmAtSpeed(config, speedKph / 3.6f, gear), config.IdleRpm, config.RevLimiter);
            var deltaKph = throttle > 0f
                ? speedKph - startSpeedKph
                : startSpeedKph - speedKph;

            return new SpeedTrace(
                spec.Name,
                StartSpeedKph: Rounding.F(startSpeedKph, 2),
                FinalSpeedKph: Rounding.F(speedKph, 2),
                DeltaKph: Rounding.F(deltaKph, 2),
                EndRpm: Rounding.F(endRpm, 1),
                LossFraction: Rounding.F(startSpeedKph > 0f ? deltaKph / startSpeedKph : 0f, 3));
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }

    internal sealed record CoastTrace(
        string Vehicle,
        float StartSpeedKph,
        float FinalSpeedKph,
        IReadOnlyList<CoastSample> Samples);

internal sealed record CoastSample(
    float TimeSeconds,
    float SpeedKph,
    float AerodynamicDecelKph,
    float RollingResistanceDecelKph,
    float WheelSideDragDecelKph);

    internal sealed record VehicleCatalogSnapshot(
        string Vehicle,
    string Transmission,
    int Gears,
    float ConfiguredTopSpeedKph,
    float TopGearKph,
    float PreviousGearKph,
    float SideAreaM2,
    float RollingResistanceSpeedFactor,
    float WheelSideDragBaseN,
    float WheelSideDragLinearNPerMps,
    float CoupledDrivelineDragNm,
    float CoupledDrivelineViscousDragNmPerKrpm);

    internal sealed record SpeedTrace(
        string Vehicle,
        float StartSpeedKph,
        float FinalSpeedKph,
        float DeltaKph,
        float EndRpm,
        float LossFraction);
}
