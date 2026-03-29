using System;
using System.IO;
using System.Linq;
using System.Text;
using TopSpeed.Vehicles.Parsing;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class VehicleParserStrictTests
    {
        [Fact]
        public void TryLoadFromFile_MissingTorqueCurveSection_ReturnsError()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(includeTorqueCurveSection: false));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("Missing required section [torque_curve]", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_GearRatioCountMismatch_ReturnsError()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(numberOfGears: 5, gearRatios: "3.6,2.1,1.4,1.0,0.84,0.72"));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("gear_ratios count", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_PrimaryTypeMustExistInSupportedTypes()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "atc",
                includeAtcSection: true));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("Primary transmission type", allText, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("supported transmission types", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_OnlyOneAutomaticFamilyAllowed()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                primaryType: "atc",
                supportedTypes: "atc,dct",
                includeAtcSection: true,
                includeDctSection: true));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("Only one automatic transmission family is allowed", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_MissingRequiredCvtSection_ReturnsError()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                primaryType: "cvt",
                supportedTypes: "cvt",
                includeAtcSection: false,
                includeCvtSection: false));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("Missing required section [transmission_cvt]", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_CvtRatioBounds_AreValidated()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                primaryType: "cvt",
                supportedTypes: "cvt",
                includeAtcSection: false,
                includeCvtSection: true,
                cvtRatioMin: 2.0f,
                cvtRatioMax: 1.2f));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("ratio_max must be greater than or equal to ratio_min", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_StrictPhysicsModel_MissingRequiredKeys_ReturnsError()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                physicsModel: "strict_v2_engine_clutch",
                includeResistanceSection: true,
                includeStrictPhysicsKeys: false));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("Missing required key 'idle_target_rpm'", allText, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Missing required key 'engine_friction_coulomb_nm'", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_StrictPhysicsModel_WithAllKeys_LoadsSuccessfully()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                physicsModel: "strict_v2_engine_clutch",
                includeResistanceSection: true,
                includeStrictPhysicsKeys: true));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                Assert.True(ok);
                Assert.DoesNotContain(VehicleTsvIssueSeverity.Error, issues.Select(i => i.Severity));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_StrictPhysicsModel_MissingResistanceSection_ReturnsError()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                physicsModel: "strict_v2_engine_clutch",
                includeResistanceSection: false,
                includeStrictPhysicsKeys: true));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("Missing required section [resistance]", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_StrictPhysicsModel_WithLegacyEngineResistanceKeys_ReturnsError()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                physicsModel: "strict_v2_engine_clutch",
                includeResistanceSection: true,
                includeLegacyResistanceInStrict: true,
                includeStrictPhysicsKeys: true));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("not allowed for strict physics", allText, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Move resistance values to [resistance]", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static string WriteTempVehicle(string content)
        {
            var path = Path.Combine(Path.GetTempPath(), $"topspeed_vehicle_strict_{Guid.NewGuid():N}.tsv");
            File.WriteAllText(path, content);
            return path;
        }

        private static string BuildVehicleTsv(
            string primaryType = "atc",
            string supportedTypes = "atc",
            bool includeTorqueCurveSection = true,
            bool includeAtcSection = true,
            bool includeDctSection = false,
            bool includeCvtSection = false,
            int numberOfGears = 6,
            string gearRatios = "3.6,2.1,1.4,1.0,0.84,0.72",
            string physicsModel = "legacy",
            bool includeResistanceSection = false,
            bool includeLegacyResistanceInStrict = false,
            bool includeStrictPhysicsKeys = false,
            float cvtRatioMin = 0.45f,
            float cvtRatioMax = 3.40f)
        {
            var sb = new StringBuilder();

            sb.AppendLine("[meta]");
            sb.AppendLine("name=Parser Strict Vehicle");
            sb.AppendLine("version=1");
            sb.AppendLine("description=Parser strict validation");
            sb.AppendLine();

            sb.AppendLine("[sounds]");
            sb.AppendLine("engine=builtin/engine.ogg");
            sb.AppendLine("start=builtin/start.ogg");
            sb.AppendLine("horn=builtin/horn.ogg");
            sb.AppendLine("crash=builtin/crash.ogg");
            sb.AppendLine("brake=builtin/brake.ogg");
            sb.AppendLine("idle_freq=400");
            sb.AppendLine("top_freq=2200");
            sb.AppendLine("shift_freq=1200");
            sb.AppendLine();

            sb.AppendLine("[general]");
            sb.AppendLine("surface_traction_factor=1");
            sb.AppendLine("deceleration=0.1");
            sb.AppendLine("max_speed=180");
            sb.AppendLine("has_wipers=0");
            sb.AppendLine();

            sb.AppendLine("[engine]");
            sb.AppendLine("idle_rpm=700");
            sb.AppendLine("max_rpm=7000");
            sb.AppendLine("rev_limiter=6500");
            sb.AppendLine("auto_shift_rpm=0");
            sb.AppendLine("engine_braking=0.3");
            sb.AppendLine("mass_kg=1500");
            sb.AppendLine("drivetrain_efficiency=0.85");
            if (!string.Equals(physicsModel, "strict_v2_engine_clutch", StringComparison.OrdinalIgnoreCase) || includeLegacyResistanceInStrict)
            {
                sb.AppendLine("drag_coefficient=0.30");
                sb.AppendLine("frontal_area=2.2");
                sb.AppendLine("rolling_resistance=0.015");
            }
            sb.AppendLine("launch_rpm=1800");
            if (!string.IsNullOrWhiteSpace(physicsModel))
                sb.AppendLine($"physics_model={physicsModel}");
            if (includeStrictPhysicsKeys)
            {
                sb.AppendLine("idle_target_rpm=760");
                sb.AppendLine("idle_max_correction_torque_nm=180");
                sb.AppendLine("idle_control_kp=0.08");
                sb.AppendLine("idle_control_ki=0.22");
                sb.AppendLine("launch_target_slip_rpm=350");
            }
            sb.AppendLine();

            sb.AppendLine("[torque]");
            sb.AppendLine("engine_braking_torque=150");
            sb.AppendLine("peak_torque=280");
            sb.AppendLine("peak_torque_rpm=3500");
            sb.AppendLine("idle_torque=120");
            sb.AppendLine("redline_torque=180");
            sb.AppendLine("power_factor=0.5");
            sb.AppendLine("engine_inertia_kgm2=0.24");
            sb.AppendLine("engine_friction_torque_nm=20");
            sb.AppendLine("driveline_coupling_rate=12");
            if (includeStrictPhysicsKeys)
            {
                sb.AppendLine("engine_friction_coulomb_nm=22");
                sb.AppendLine("engine_friction_viscous_nm_per_rad_s=0.012");
                sb.AppendLine("engine_pumping_loss_nm_at_closed_throttle=90");
                sb.AppendLine("engine_accessory_torque_nm=8");
                sb.AppendLine("clutch_capacity_nm=1250");
                sb.AppendLine("clutch_engage_rate_per_s=14");
                sb.AppendLine("clutch_release_rate_per_s=20");
            sb.AppendLine("clutch_drag_torque_nm=35");
            }
            sb.AppendLine();

            if (includeResistanceSection)
            {
                sb.AppendLine("[resistance]");
                sb.AppendLine("drag_coefficient=0.30");
                sb.AppendLine("frontal_area_m2=2.2");
                sb.AppendLine("air_density_kg_per_m3=1.225");
                sb.AppendLine("rolling_resistance_base=0.015");
                sb.AppendLine("rolling_resistance_speed_gain_per_mps=0.01");
                sb.AppendLine("driveline_coast_torque_nm=15");
                sb.AppendLine("driveline_coast_viscous_nm_per_rad_s=0.01");
                sb.AppendLine("coast_stop_speed_kph=3");
                sb.AppendLine("coast_stop_decel_kphps=0.7");
                sb.AppendLine();
            }

            if (includeTorqueCurveSection)
            {
                sb.AppendLine("[torque_curve]");
                sb.AppendLine("1000rpm=120");
                sb.AppendLine("3000rpm=280");
                sb.AppendLine("6000rpm=180");
                sb.AppendLine();
            }

            sb.AppendLine("[transmission]");
            sb.AppendLine($"primary_type={primaryType}");
            sb.AppendLine($"supported_types={supportedTypes}");
            sb.AppendLine("shift_on_demand=0");
            sb.AppendLine();

            if (includeAtcSection)
            {
                sb.AppendLine("[transmission_atc]");
                sb.AppendLine("creep_accel_kphps=0.7");
                sb.AppendLine("launch_coupling_min=0.2");
                sb.AppendLine("launch_coupling_max=0.9");
                sb.AppendLine("lock_speed_kph=30");
                sb.AppendLine("lock_throttle_min=0.2");
                sb.AppendLine("shift_release_coupling=0.5");
                sb.AppendLine("engage_rate=12");
                sb.AppendLine("disengage_rate=18");
                sb.AppendLine();
            }

            if (includeDctSection)
            {
                sb.AppendLine("[transmission_dct]");
                sb.AppendLine("launch_coupling_min=0.2");
                sb.AppendLine("launch_coupling_max=0.9");
                sb.AppendLine("lock_speed_kph=30");
                sb.AppendLine("lock_throttle_min=0.2");
                sb.AppendLine("shift_overlap_coupling=0.4");
                sb.AppendLine("engage_rate=12");
                sb.AppendLine("disengage_rate=18");
                sb.AppendLine();
            }

            if (includeCvtSection)
            {
                sb.AppendLine("[transmission_cvt]");
                sb.AppendLine($"ratio_min={cvtRatioMin.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                sb.AppendLine($"ratio_max={cvtRatioMax.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                sb.AppendLine("target_rpm_low=1700");
                sb.AppendLine("target_rpm_high=4200");
                sb.AppendLine("ratio_change_rate=4.5");
                sb.AppendLine("launch_coupling_min=0.24");
                sb.AppendLine("launch_coupling_max=0.85");
                sb.AppendLine("lock_speed_kph=24");
                sb.AppendLine("lock_throttle_min=0.12");
                sb.AppendLine("creep_accel_kphps=0.7");
                sb.AppendLine("shift_hold_coupling=0.75");
                sb.AppendLine("engage_rate=4.5");
                sb.AppendLine("disengage_rate=8.5");
                sb.AppendLine();
            }

            sb.AppendLine("[drivetrain]");
            sb.AppendLine("final_drive=3.5");
            sb.AppendLine("reverse_max_speed=35");
            sb.AppendLine("reverse_power_factor=0.55");
            sb.AppendLine("reverse_gear_ratio=3.2");
            sb.AppendLine("brake_strength=1.0");
            sb.AppendLine();

            sb.AppendLine("[gears]");
            sb.AppendLine($"number_of_gears={numberOfGears}");
            sb.AppendLine($"gear_ratios={gearRatios}");
            sb.AppendLine();

            sb.AppendLine("[steering]");
            sb.AppendLine("steering_response=1.0");
            sb.AppendLine("wheelbase=2.7");
            sb.AppendLine("max_steer_deg=35");
            sb.AppendLine("high_speed_stability=0.1");
            sb.AppendLine("high_speed_steer_gain=1.05");
            sb.AppendLine("high_speed_steer_start_kph=120");
            sb.AppendLine("high_speed_steer_full_kph=220");
            sb.AppendLine();

            sb.AppendLine("[tire_model]");
            sb.AppendLine("tire_grip=1.0");
            sb.AppendLine("lateral_grip=1.0");
            sb.AppendLine("combined_grip_penalty=0.72");
            sb.AppendLine("slip_angle_peak_deg=8");
            sb.AppendLine("slip_angle_falloff=1.25");
            sb.AppendLine("turn_response=1.0");
            sb.AppendLine("mass_sensitivity=0.75");
            sb.AppendLine("downforce_grip_gain=0.05");
            sb.AppendLine();

            sb.AppendLine("[dynamics]");
            sb.AppendLine("corner_stiffness_front=1.0");
            sb.AppendLine("corner_stiffness_rear=1.0");
            sb.AppendLine("yaw_inertia_scale=1.0");
            sb.AppendLine("steering_curve=1.0");
            sb.AppendLine("transient_damping=1.0");
            sb.AppendLine();

            sb.AppendLine("[dimensions]");
            sb.AppendLine("vehicle_width=1.8");
            sb.AppendLine("vehicle_length=4.5");
            sb.AppendLine();

            sb.AppendLine("[tires]");
            sb.AppendLine("tire_circumference=2.0");

            return sb.ToString();
        }
    }
}
