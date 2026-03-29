using System;
using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
    {
        private static readonly Dictionary<string, HashSet<string>> s_allowedKeys = BuildAllowedKeys();

        private static Dictionary<string, HashSet<string>> BuildAllowedKeys()
        {
            return new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["meta"] = Set("name", "version", "description"),
                ["sounds"] = Set("engine", "start", "horn", "throttle", "crash", "brake", "backfire", "idle_freq", "top_freq", "shift_freq", "pitch_curve_exponent"),
                ["general"] = Set("surface_traction_factor", "deceleration", "max_speed", "has_wipers"),
                ["engine"] = Set(
                    "idle_rpm", "max_rpm", "rev_limiter", "auto_shift_rpm", "engine_braking", "mass_kg", "drivetrain_efficiency",
                    "drag_coefficient", "frontal_area", "rolling_resistance", "launch_rpm", "physics_model",
                    "idle_target_rpm", "idle_max_correction_torque_nm", "idle_control_kp", "idle_control_ki",
                    "launch_target_slip_rpm"),
                ["resistance"] = Set(
                    "drag_coefficient",
                    "frontal_area_m2",
                    "air_density_kg_per_m3",
                    "rolling_resistance_base",
                    "rolling_resistance_speed_gain_per_mps",
                    "driveline_coast_torque_nm",
                    "driveline_coast_viscous_nm_per_rad_s",
                    "coast_stop_speed_kph",
                    "coast_stop_decel_kphps"),
                ["torque"] = Set(
                    "engine_braking_torque", "peak_torque", "peak_torque_rpm", "idle_torque", "redline_torque",
                    "power_factor", "engine_inertia_kgm2", "engine_friction_torque_nm", "driveline_coupling_rate",
                    "engine_friction_coulomb_nm", "engine_friction_viscous_nm_per_rad_s",
                    "engine_pumping_loss_nm_at_closed_throttle", "engine_accessory_torque_nm",
                    "clutch_capacity_nm", "clutch_engage_rate_per_s", "clutch_release_rate_per_s",
                    "clutch_drag_torque_nm"),
                ["torque_curve"] = Set("preset"),
                ["transmission"] = Set(
                    "primary_type",
                    "supported_types",
                    "shift_on_demand"),
                ["transmission_atc"] = Set(
                    "creep_accel_kphps",
                    "launch_coupling_min",
                    "launch_coupling_max",
                    "lock_speed_kph",
                    "lock_throttle_min",
                    "shift_release_coupling",
                    "engage_rate",
                    "disengage_rate"),
                ["transmission_dct"] = Set(
                    "launch_coupling_min",
                    "launch_coupling_max",
                    "lock_speed_kph",
                    "lock_throttle_min",
                    "shift_overlap_coupling",
                    "engage_rate",
                    "disengage_rate"),
                ["transmission_cvt"] = Set(
                    "ratio_min",
                    "ratio_max",
                    "target_rpm_low",
                    "target_rpm_high",
                    "ratio_change_rate",
                    "launch_coupling_min",
                    "launch_coupling_max",
                    "lock_speed_kph",
                    "lock_throttle_min",
                    "creep_accel_kphps",
                    "shift_hold_coupling",
                    "engage_rate",
                    "disengage_rate"),
                ["drivetrain"] = Set("final_drive", "reverse_max_speed", "reverse_power_factor", "reverse_gear_ratio", "brake_strength"),
                ["gears"] = Set("number_of_gears", "gear_ratios"),
                ["steering"] = Set("steering_response", "wheelbase", "max_steer_deg", "high_speed_stability", "high_speed_steer_gain", "high_speed_steer_start_kph", "high_speed_steer_full_kph"),
                ["tire_model"] = Set("tire_grip", "lateral_grip", "combined_grip_penalty", "slip_angle_peak_deg", "slip_angle_falloff", "turn_response", "mass_sensitivity", "downforce_grip_gain"),
                ["dynamics"] = Set("corner_stiffness_front", "corner_stiffness_rear", "yaw_inertia_scale", "steering_curve", "transient_damping"),
                ["dimensions"] = Set("vehicle_width", "vehicle_length"),
                ["tires"] = Set("tire_circumference", "tire_width", "tire_aspect", "tire_rim"),
                ["policy"] = Set(
                    "top_speed_gear",
                    "allow_overdrive_above_game_top_speed",
                    "base_auto_shift_cooldown",
                    "upshift_delay_default",
                    "auto_upshift_rpm_fraction",
                    "auto_upshift_rpm",
                    "auto_downshift_rpm_fraction",
                    "auto_downshift_rpm",
                    "top_speed_pursuit_speed_fraction",
                    "upshift_hysteresis",
                    "min_upshift_net_accel_mps2",
                    "prefer_intended_top_speed_gear_near_limit")
            };
        }

        private static HashSet<string> Set(params string[] values) => new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);

        private static bool IsAllowedKey(string section, string key)
        {
            if (s_allowedKeys.TryGetValue(section, out var keys) && keys.Contains(key))
                return true;

            if (!string.Equals(section, "policy", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(section, "torque_curve", StringComparison.OrdinalIgnoreCase))
                    return false;

                return key.EndsWith("rpm", StringComparison.OrdinalIgnoreCase);
            }

            if (key.StartsWith("upshift_delay_", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}
