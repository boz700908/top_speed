using System;
using System.Collections.Generic;
using TopSpeed.Vehicles;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
    {
        private static void ParseEngineValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.PhysicsModel = VehicleDefinition.LegacyPhysicsModel;
            values.UseStrictEngineClutchModel = false;
            if (section.Entries.TryGetValue("physics_model", out var physicsModelEntry))
            {
                var model = physicsModelEntry.Value.Trim();
                if (model.Length == 0
                    || string.Equals(model, VehicleDefinition.LegacyPhysicsModel, StringComparison.OrdinalIgnoreCase))
                {
                    values.PhysicsModel = VehicleDefinition.LegacyPhysicsModel;
                }
                else if (string.Equals(model, VehicleDefinition.StrictEngineClutchPhysicsModel, StringComparison.OrdinalIgnoreCase))
                {
                    values.PhysicsModel = VehicleDefinition.StrictEngineClutchPhysicsModel;
                    values.UseStrictEngineClutchModel = true;
                }
                else
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        physicsModelEntry.Line,
                        Localized(
                            "physics_model must be '{0}' or '{1}'.",
                            VehicleDefinition.LegacyPhysicsModel,
                            VehicleDefinition.StrictEngineClutchPhysicsModel)));
                }
            }

            values.IdleRpm = RequireFloatRange(section, "idle_rpm", 300f, 3000f, issues);
            values.MaxRpm = RequireFloatRange(section, "max_rpm", 1000f, 20000f, issues);
            values.RevLimiter = RequireFloatRange(section, "rev_limiter", 800f, 18000f, issues);
            values.AutoShiftRpm = RequireFloatRange(section, "auto_shift_rpm", 0f, 18000f, issues);
            values.EngineBraking = RequireFloatRange(section, "engine_braking", 0f, 1.5f, issues);
            values.MassKg = RequireFloatRange(section, "mass_kg", 20f, 10000f, issues);
            values.DrivetrainEfficiency = RequireFloatRange(section, "drivetrain_efficiency", 0.1f, 1.0f, issues);
            values.LaunchRpm = RequireFloatRange(section, "launch_rpm", 0f, 18000f, issues);

            if (values.UseStrictEngineClutchModel)
            {
                values.DragCoefficient = OptionalFloat(section, "drag_coefficient", issues) ?? 0.30f;
                values.FrontalArea = OptionalFloat(section, "frontal_area", issues) ?? 2.2f;
                values.RollingResistance = OptionalFloat(section, "rolling_resistance", issues) ?? 0.015f;
            }
            else
            {
                values.DragCoefficient = RequireFloatRange(section, "drag_coefficient", 0.01f, 1.5f, issues);
                values.FrontalArea = RequireFloatRange(section, "frontal_area", 0.05f, 10f, issues);
                values.RollingResistance = RequireFloatRange(section, "rolling_resistance", 0.001f, 0.1f, issues);
            }

            values.AirDensityKgPerM3 = 1.225f;
            values.RollingResistanceSpeedGainPerMps = 0f;
            values.DrivelineCoastTorqueNm = 0f;
            values.DrivelineCoastViscousNmPerRadS = 0f;
            values.CoastStopSpeedKph = 3f;
            values.CoastStopDecelKphps = 0.7f;

            if (values.UseStrictEngineClutchModel)
            {
                values.IdleTargetRpm = RequireFloatRange(section, "idle_target_rpm", 300f, 3000f, issues);
                values.IdleMaxCorrectionTorqueNm = RequireFloatRange(section, "idle_max_correction_torque_nm", 0f, 1000f, issues);
                values.IdleControlKp = RequireFloatRange(section, "idle_control_kp", 0f, 5f, issues);
                values.IdleControlKi = RequireFloatRange(section, "idle_control_ki", 0f, 20f, issues);
                values.LaunchTargetSlipRpm = RequireFloatRange(section, "launch_target_slip_rpm", 0f, 3000f, issues);
            }
            else
            {
                values.IdleTargetRpm = values.IdleRpm;
                values.IdleMaxCorrectionTorqueNm = 160f;
                values.IdleControlKp = 0.08f;
                values.IdleControlKi = 0.22f;
                values.LaunchTargetSlipRpm = 350f;
            }
        }

        private static void ParseTorqueValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.EngineBrakingTorque = RequireFloatRange(section, "engine_braking_torque", 0f, 3000f, issues);
            values.PeakTorque = RequireFloatRange(section, "peak_torque", 10f, 3000f, issues);
            values.PeakTorqueRpm = RequireFloatRange(section, "peak_torque_rpm", 500f, 18000f, issues);
            values.IdleTorque = RequireFloatRange(section, "idle_torque", 0f, 3000f, issues);
            values.RedlineTorque = RequireFloatRange(section, "redline_torque", 0f, 3000f, issues);
            values.PowerFactor = RequireFloatRange(section, "power_factor", 0.05f, 2f, issues);
            values.EngineInertiaKgm2 = RequireFloatRange(section, "engine_inertia_kgm2", 0.01f, 5f, issues);
            values.EngineFrictionTorqueNm = RequireFloatRange(section, "engine_friction_torque_nm", 0f, 1000f, issues);
            values.DrivelineCouplingRate = RequireFloatRange(section, "driveline_coupling_rate", 0.1f, 80f, issues);

            if (values.UseStrictEngineClutchModel)
            {
                values.EngineFrictionCoulombNm = RequireFloatRange(section, "engine_friction_coulomb_nm", 0f, 1000f, issues);
                values.EngineFrictionViscousNmPerRadS = RequireFloatRange(section, "engine_friction_viscous_nm_per_rad_s", 0f, 5f, issues);
                values.EnginePumpingLossNmAtClosedThrottle = RequireFloatRange(section, "engine_pumping_loss_nm_at_closed_throttle", 0f, 2000f, issues);
                values.EngineAccessoryTorqueNm = RequireFloatRange(section, "engine_accessory_torque_nm", 0f, 200f, issues);
                values.ClutchCapacityNm = RequireFloatRange(section, "clutch_capacity_nm", 1f, 5000f, issues);
                values.ClutchEngageRatePerS = RequireFloatRange(section, "clutch_engage_rate_per_s", 0.1f, 120f, issues);
                values.ClutchReleaseRatePerS = RequireFloatRange(section, "clutch_release_rate_per_s", 0.1f, 120f, issues);
                values.ClutchDragTorqueNm = RequireFloatRange(section, "clutch_drag_torque_nm", 0f, 1000f, issues);
            }
            else
            {
                values.EngineFrictionCoulombNm = values.EngineFrictionTorqueNm;
                values.EngineFrictionViscousNmPerRadS = 0.01f;
                values.EnginePumpingLossNmAtClosedThrottle = values.EngineBrakingTorque * values.EngineBraking;
                values.EngineAccessoryTorqueNm = 8f;
                values.ClutchCapacityNm = 1200f;
                values.ClutchEngageRatePerS = values.DrivelineCouplingRate;
                values.ClutchReleaseRatePerS = values.DrivelineCouplingRate * 1.5f;
                values.ClutchDragTorqueNm = 30f;
            }
        }

        private static void ParseResistanceValues(Section? section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            if (!values.UseStrictEngineClutchModel)
                return;

            if (section == null)
                return;

            values.DragCoefficient = RequireFloatRange(section, "drag_coefficient", 0.01f, 1.5f, issues);
            values.FrontalArea = RequireFloatRange(section, "frontal_area_m2", 0.05f, 10f, issues);
            values.AirDensityKgPerM3 = OptionalFloat(section, "air_density_kg_per_m3", issues) ?? 1.225f;
            values.RollingResistance = RequireFloatRange(section, "rolling_resistance_base", 0.001f, 0.1f, issues);
            values.RollingResistanceSpeedGainPerMps = RequireFloatRange(section, "rolling_resistance_speed_gain_per_mps", 0f, 0.5f, issues);
            values.DrivelineCoastTorqueNm = RequireFloatRange(section, "driveline_coast_torque_nm", 0f, 2000f, issues);
            values.DrivelineCoastViscousNmPerRadS = RequireFloatRange(section, "driveline_coast_viscous_nm_per_rad_s", 0f, 5f, issues);
            values.CoastStopSpeedKph = RequireFloatRange(section, "coast_stop_speed_kph", 0f, 40f, issues);
            values.CoastStopDecelKphps = RequireFloatRange(section, "coast_stop_decel_kphps", 0f, 20f, issues);
        }

        private static void ParseDrivetrainValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.FinalDrive = RequireFloatRange(section, "final_drive", 0.5f, 8f, issues);
            values.ReverseMaxSpeed = RequireFloatRange(section, "reverse_max_speed", 1f, 100f, issues);
            values.ReversePowerFactor = RequireFloatRange(section, "reverse_power_factor", 0.05f, 2f, issues);
            values.ReverseGearRatio = RequireFloatRange(section, "reverse_gear_ratio", 0.5f, 8f, issues);
            values.BrakeStrength = RequireFloatRange(section, "brake_strength", 0.1f, 5f, issues);
        }
    }
}
