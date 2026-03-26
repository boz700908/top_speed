using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Torque;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
    {
        private static bool TryBuild(string fullPath, Dictionary<string, Section> sections, List<VehicleTsvIssue> issues, out CustomVehicleTsvData data)
        {
            data = null!;

            var meta = sections["meta"];
            var sounds = sections["sounds"];
            var general = sections["general"];
            var engine = sections["engine"];
            var torque = sections["torque"];
            var torqueCurve = sections["torque_curve"];
            var transmission = sections["transmission"];
            sections.TryGetValue("transmission_atc", out var transmissionAtc);
            sections.TryGetValue("transmission_dct", out var transmissionDct);
            sections.TryGetValue("transmission_cvt", out var transmissionCvt);
            var drivetrain = sections["drivetrain"];
            var gears = sections["gears"];
            var steeringSection = sections["steering"];
            var tireModelSection = sections["tire_model"];
            var dynamicsSection = sections["dynamics"];
            var dimensions = sections["dimensions"];
            var tires = sections["tires"];
            sections.TryGetValue("policy", out var policy);

            var metaName = RequireString(meta, "name", issues);
            var metaVersion = RequireString(meta, "version", issues);
            var metaDescription = RequireString(meta, "description", issues);

            var engineSound = RequireString(sounds, "engine", issues);
            var startSound = RequireString(sounds, "start", issues);
            var hornSound = RequireString(sounds, "horn", issues);
            var throttleSound = OptionalString(sounds, "throttle");
            var crashVariants = RequireCsvStrings(sounds, "crash", issues);
            var brakeSound = RequireString(sounds, "brake", issues);
            var backfireVariants = OptionalCsvStrings(sounds, "backfire");

            var idleFreq = RequireIntRange(sounds, "idle_freq", 100, 200000, issues);
            var topFreq = RequireIntRange(sounds, "top_freq", 100, 200000, issues);
            var shiftFreq = RequireIntRange(sounds, "shift_freq", 100, 200000, issues);
            var pitchCurveExponent = OptionalFloat(sounds, "pitch_curve_exponent", issues)
                ?? TopSpeed.Vehicles.VehicleDefinition.PitchCurveExponentDefault;

            var surfaceTractionFactor = RequireFloatRange(general, "surface_traction_factor", 0f, 5f, issues);
            var deceleration = RequireFloatRange(general, "deceleration", 0f, 5f, issues);
            var topSpeed = RequireFloatRange(general, "max_speed", 10f, 500f, issues);
            var hasWipers = RequireBoolInt(general, "has_wipers", issues);

            var gearCount = RequireIntRange(gears, "number_of_gears", 1, 10, issues);
            var gearRatios = RequireFloatCsv(gears, "gear_ratios", issues);
            var primaryTransmissionType = RequireTransmissionType(transmission, "primary_type", issues);
            var supportedTransmissionTypes = RequireTransmissionTypes(transmission, "supported_types", issues);
            var shiftOnDemand = OptionalBoolInt(transmission, "shift_on_demand", issues) ?? false;

            var idleRpm = RequireFloatRange(engine, "idle_rpm", 300f, 3000f, issues);
            var maxRpm = RequireFloatRange(engine, "max_rpm", 1000f, 20000f, issues);
            var revLimiter = RequireFloatRange(engine, "rev_limiter", 800f, 18000f, issues);
            var autoShiftRpm = RequireFloatRange(engine, "auto_shift_rpm", 0f, 18000f, issues);
            var engineBraking = RequireFloatRange(engine, "engine_braking", 0f, 1.5f, issues);
            var massKg = RequireFloatRange(engine, "mass_kg", 20f, 10000f, issues);
            var drivetrainEfficiency = RequireFloatRange(engine, "drivetrain_efficiency", 0.1f, 1.0f, issues);
            var dragCoefficient = RequireFloatRange(engine, "drag_coefficient", 0.01f, 1.5f, issues);
            var frontalArea = RequireFloatRange(engine, "frontal_area", 0.05f, 10f, issues);
            var rollingResistance = RequireFloatRange(engine, "rolling_resistance", 0.001f, 0.1f, issues);
            var launchRpm = RequireFloatRange(engine, "launch_rpm", 0f, 18000f, issues);

            var engineBrakingTorque = RequireFloatRange(torque, "engine_braking_torque", 0f, 3000f, issues);
            var peakTorque = RequireFloatRange(torque, "peak_torque", 10f, 3000f, issues);
            var peakTorqueRpm = RequireFloatRange(torque, "peak_torque_rpm", 500f, 18000f, issues);
            var idleTorque = RequireFloatRange(torque, "idle_torque", 0f, 3000f, issues);
            var redlineTorque = RequireFloatRange(torque, "redline_torque", 0f, 3000f, issues);
            var powerFactor = RequireFloatRange(torque, "power_factor", 0.05f, 2f, issues);
            var engineInertiaKgm2 = RequireFloatRange(torque, "engine_inertia_kgm2", 0.01f, 5f, issues);
            var engineFrictionTorqueNm = RequireFloatRange(torque, "engine_friction_torque_nm", 0f, 1000f, issues);
            var drivelineCouplingRate = RequireFloatRange(torque, "driveline_coupling_rate", 0.1f, 80f, issues);

            var finalDrive = RequireFloatRange(drivetrain, "final_drive", 0.5f, 8f, issues);
            var reverseMaxSpeed = RequireFloatRange(drivetrain, "reverse_max_speed", 1f, 100f, issues);
            var reversePowerFactor = RequireFloatRange(drivetrain, "reverse_power_factor", 0.05f, 2f, issues);
            var reverseGearRatio = RequireFloatRange(drivetrain, "reverse_gear_ratio", 0.5f, 8f, issues);
            var brakeStrength = RequireFloatRange(drivetrain, "brake_strength", 0.1f, 5f, issues);

            var steeringResponse = RequireFloatRange(steeringSection, "steering_response", 0.1f, 5f, issues);
            var wheelbase = RequireFloatRange(steeringSection, "wheelbase", 0.3f, 8f, issues);
            var maxSteerDeg = RequireFloatRange(steeringSection, "max_steer_deg", 5f, 60f, issues);
            var highSpeedStability = RequireFloatRange(steeringSection, "high_speed_stability", 0f, 1f, issues);
            var highSpeedSteerGain = RequireFloatRange(steeringSection, "high_speed_steer_gain", 0.7f, 1.6f, issues);
            var highSpeedSteerStartKph = RequireFloatRange(steeringSection, "high_speed_steer_start_kph", 60f, 260f, issues);
            var highSpeedSteerFullKph = RequireFloatRange(steeringSection, "high_speed_steer_full_kph", 100f, 350f, issues);

            var tireGrip = RequireFloatRange(tireModelSection, "tire_grip", 0.1f, 3f, issues);
            var lateralGrip = RequireFloatRange(tireModelSection, "lateral_grip", 0.1f, 3f, issues);
            var combinedGripPenalty = RequireFloatRange(tireModelSection, "combined_grip_penalty", 0f, 1f, issues);
            var slipAnglePeakDeg = RequireFloatRange(tireModelSection, "slip_angle_peak_deg", 0.5f, 20f, issues);
            var slipAngleFalloff = RequireFloatRange(tireModelSection, "slip_angle_falloff", 0.01f, 5f, issues);
            var turnResponse = RequireFloatRange(tireModelSection, "turn_response", 0.2f, 2.5f, issues);
            var massSensitivity = RequireFloatRange(tireModelSection, "mass_sensitivity", 0f, 1f, issues);
            var downforceGripGain = RequireFloatRange(tireModelSection, "downforce_grip_gain", 0f, 1f, issues);

            var cornerStiffnessFront = RequireFloatRange(dynamicsSection, "corner_stiffness_front", 0.2f, 3f, issues);
            var cornerStiffnessRear = RequireFloatRange(dynamicsSection, "corner_stiffness_rear", 0.2f, 3f, issues);
            var yawInertiaScale = RequireFloatRange(dynamicsSection, "yaw_inertia_scale", 0.5f, 2f, issues);
            var steeringCurve = RequireFloatRange(dynamicsSection, "steering_curve", 0.5f, 2f, issues);
            var transientDamping = RequireFloatRange(dynamicsSection, "transient_damping", 0f, 6f, issues);

            var widthM = RequireFloatRange(dimensions, "vehicle_width", 0.2f, 5f, issues);
            var lengthM = RequireFloatRange(dimensions, "vehicle_length", 0.3f, 20f, issues);

            var tireCircumference = OptionalFloat(tires, "tire_circumference", issues);
            var tireWidth = OptionalInt(tires, "tire_width", issues);
            var tireAspect = OptionalInt(tires, "tire_aspect", issues);
            var tireRim = OptionalInt(tires, "tire_rim", issues);
            var automaticTuning = BuildAutomaticTuning(
                transmission,
                transmissionAtc,
                transmissionDct,
                transmissionCvt,
                supportedTransmissionTypes,
                idleRpm,
                revLimiter,
                issues);

            if (gearRatios != null)
            {
                if (gearRatios.Count != gearCount)
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, gears.Entries["gear_ratios"].Line,
                        Localized("gear_ratios count ({0}) must match number_of_gears ({1}).", gearRatios.Count, gearCount)));
                }
                else
                {
                    for (var i = 0; i < gearRatios.Count; i++)
                    {
                        var value = gearRatios[i];
                        if (value < 0.20f || value > 8.00f)
                            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, gears.Entries["gear_ratios"].Line, Localized("gear_ratios[{0}] is outside allowed range 0.20 to 8.00.", i + 1)));
                    }
                    for (var i = 1; i < gearRatios.Count; i++)
                    {
                        if (gearRatios[i] > gearRatios[i - 1] + 0.0001f)
                        {
                            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, gears.Entries["gear_ratios"].Line, Localized("gear_ratios must be non-increasing from gear 1 to last gear.")));
                            break;
                        }
                    }
                }
            }

            ValidateTransmissionTypes(
                transmission,
                primaryTransmissionType,
                supportedTransmissionTypes,
                issues);
            if (shiftOnDemand)
            {
                var hasAutomaticFamily = false;
                for (var i = 0; i < supportedTransmissionTypes.Count; i++)
                {
                    if (!TransmissionTypes.IsAutomaticFamily(supportedTransmissionTypes[i]))
                        continue;

                    hasAutomaticFamily = true;
                    break;
                }

                if (!hasAutomaticFamily)
                {
                    var line = transmission.Line;
                    if (transmission.Entries.TryGetValue("shift_on_demand", out var shiftOnDemandEntry))
                        line = shiftOnDemandEntry.Line;
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Warning,
                        line,
                        Localized("shift_on_demand is ignored because supported_types does not include an automatic transmission type.")));
                }
            }

            if (maxRpm < idleRpm)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, engine.Entries["max_rpm"].Line, Localized("max_rpm must be greater than or equal to idle_rpm.")));
            if (revLimiter < idleRpm || revLimiter > maxRpm)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, engine.Entries["rev_limiter"].Line, Localized("rev_limiter must be between idle_rpm and max_rpm.")));
            if (autoShiftRpm > 0f && (autoShiftRpm < idleRpm || autoShiftRpm > revLimiter))
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, engine.Entries["auto_shift_rpm"].Line, Localized("auto_shift_rpm must be 0 or between idle_rpm and rev_limiter.")));
            if (peakTorqueRpm < idleRpm || peakTorqueRpm > revLimiter)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, torque.Entries["peak_torque_rpm"].Line, Localized("peak_torque_rpm must be between idle_rpm and rev_limiter.")));
            if (launchRpm > revLimiter)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, engine.Entries["launch_rpm"].Line, Localized("launch_rpm must not exceed rev_limiter.")));
            if (topFreq < idleFreq)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, sounds.Entries["top_freq"].Line, Localized("top_freq must be greater than or equal to idle_freq.")));
            if (shiftFreq < idleFreq || shiftFreq > topFreq)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, sounds.Entries["shift_freq"].Line, Localized("shift_freq must be between idle_freq and top_freq.")));
            if (float.IsNaN(pitchCurveExponent)
                || float.IsInfinity(pitchCurveExponent)
                || pitchCurveExponent < TopSpeed.Vehicles.VehicleDefinition.PitchCurveExponentMin
                || pitchCurveExponent > TopSpeed.Vehicles.VehicleDefinition.PitchCurveExponentMax)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    sounds.Entries["pitch_curve_exponent"].Line,
                    Localized(
                        "pitch_curve_exponent must be between {0} and {1}.",
                        TopSpeed.Vehicles.VehicleDefinition.PitchCurveExponentMin,
                        TopSpeed.Vehicles.VehicleDefinition.PitchCurveExponentMax)));
            }
            if (highSpeedSteerFullKph <= highSpeedSteerStartKph)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, steeringSection.Entries["high_speed_steer_full_kph"].Line, Localized("high_speed_steer_full_kph must be greater than high_speed_steer_start_kph.")));

            if (!TryBuildTorqueCurve(
                    torqueCurve,
                    idleRpm,
                    revLimiter,
                    peakTorqueRpm,
                    idleTorque,
                    peakTorque,
                    redlineTorque,
                    issues,
                    out var torqueCurvePreset,
                    out var torqueCurveRpm,
                    out var torqueCurveTorqueNm))
            {
                return false;
            }

            float tireCircumferenceResolved = 0f;
            if (tireCircumference.HasValue && tireCircumference.Value > 0f)
            {
                if (tireCircumference.Value < 0.2f || tireCircumference.Value > 5f)
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Entries["tire_circumference"].Line, Localized("tire_circumference must be between 0.2 and 5.0 meters.")));
                else
                    tireCircumferenceResolved = tireCircumference.Value;
            }
            else
            {
                if (!tireWidth.HasValue || !tireAspect.HasValue || !tireRim.HasValue)
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Line, Localized("Provide tire_circumference or all of tire_width, tire_aspect, and tire_rim.")));
                }
                else
                {
                    if (tireWidth.Value < 20 || tireWidth.Value > 450)
                        issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Entries["tire_width"].Line, Localized("tire_width must be between 20 and 450 mm.")));
                    if (tireAspect.Value < 5 || tireAspect.Value > 150)
                        issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Entries["tire_aspect"].Line, Localized("tire_aspect must be between 5 and 150.")));
                    if (tireRim.Value < 4 || tireRim.Value > 30)
                        issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Entries["tire_rim"].Line, Localized("tire_rim must be between 4 and 30 inches.")));
                    if (!HasErrors(issues))
                        tireCircumferenceResolved = CalculateTireCircumferenceM(tireWidth.Value, tireAspect.Value, tireRim.Value);
                }
            }

            if (!ValidatePolicy(policy, gearCount, idleRpm, revLimiter, issues))
                return false;

            if (HasErrors(issues))
                return false;

            data = new CustomVehicleTsvData
            {
                SourcePath = fullPath,
                SourceDirectory = Path.GetDirectoryName(fullPath) ?? string.Empty,
                Meta = new CustomVehicleMeta(metaName, metaVersion, metaDescription),
                Sounds = new CustomVehicleSounds
                {
                    Engine = engineSound,
                    Start = startSound,
                    Horn = hornSound,
                    Throttle = throttleSound,
                    CrashVariants = crashVariants,
                    Brake = brakeSound,
                    BackfireVariants = backfireVariants
                },
                SurfaceTractionFactor = surfaceTractionFactor,
                Deceleration = deceleration,
                TopSpeed = topSpeed,
                HasWipers = hasWipers ? 1 : 0,
                IdleFreq = idleFreq,
                TopFreq = topFreq,
                ShiftFreq = shiftFreq,
                PitchCurveExponent = TopSpeed.Vehicles.VehicleDefinition.ClampPitchCurveExponent(pitchCurveExponent),
                Gears = gearCount,
                GearRatios = gearRatios!.ToArray(),
                PrimaryTransmissionType = primaryTransmissionType,
                SupportedTransmissionTypes = supportedTransmissionTypes.ToArray(),
                ShiftOnDemand = shiftOnDemand,
                AutomaticTuning = automaticTuning,
                IdleRpm = idleRpm,
                MaxRpm = maxRpm,
                RevLimiter = revLimiter,
                AutoShiftRpm = autoShiftRpm,
                EngineBraking = engineBraking,
                MassKg = massKg,
                DrivetrainEfficiency = drivetrainEfficiency,
                EngineBrakingTorqueNm = engineBrakingTorque,
                PeakTorqueNm = peakTorque,
                PeakTorqueRpm = peakTorqueRpm,
                IdleTorqueNm = idleTorque,
                RedlineTorqueNm = redlineTorque,
                DragCoefficient = dragCoefficient,
                FrontalAreaM2 = frontalArea,
                RollingResistanceCoefficient = rollingResistance,
                LaunchRpm = launchRpm,
                EngineInertiaKgm2 = engineInertiaKgm2,
                EngineFrictionTorqueNm = engineFrictionTorqueNm,
                DrivelineCouplingRate = drivelineCouplingRate,
                PowerFactor = powerFactor,
                TorqueCurvePreset = torqueCurvePreset,
                TorqueCurveRpm = torqueCurveRpm,
                TorqueCurveTorqueNm = torqueCurveTorqueNm,
                FinalDriveRatio = finalDrive,
                ReverseMaxSpeedKph = reverseMaxSpeed,
                ReversePowerFactor = reversePowerFactor,
                ReverseGearRatio = reverseGearRatio,
                BrakeStrength = brakeStrength,
                Steering = steeringResponse,
                TireGripCoefficient = tireGrip,
                LateralGripCoefficient = lateralGrip,
                HighSpeedStability = highSpeedStability,
                WheelbaseM = wheelbase,
                MaxSteerDeg = maxSteerDeg,
                HighSpeedSteerGain = highSpeedSteerGain,
                HighSpeedSteerStartKph = highSpeedSteerStartKph,
                HighSpeedSteerFullKph = highSpeedSteerFullKph,
                CombinedGripPenalty = combinedGripPenalty,
                SlipAnglePeakDeg = slipAnglePeakDeg,
                SlipAngleFalloff = slipAngleFalloff,
                TurnResponse = turnResponse,
                MassSensitivity = massSensitivity,
                DownforceGripGain = downforceGripGain,
                CornerStiffnessFront = cornerStiffnessFront,
                CornerStiffnessRear = cornerStiffnessRear,
                YawInertiaScale = yawInertiaScale,
                SteeringCurve = steeringCurve,
                TransientDamping = transientDamping,
                WidthM = widthM,
                LengthM = lengthM,
                TireCircumferenceM = tireCircumferenceResolved,
                TransmissionPolicy = BuildTransmissionPolicy(policy, gearCount, idleRpm, revLimiter, autoShiftRpm)
            };

            return true;
        }

        private static TransmissionType RequireTransmissionType(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    section.Line,
                    Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return TransmissionType.Atc;
            }

            var raw = entry.Value.Trim();
            if (!TransmissionTypes.TryParse(raw, out var type))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    entry.Line,
                    Localized("Unsupported transmission type '{0}'. Valid values: atc, cvt, dct, manual.", raw)));
                return TransmissionType.Atc;
            }

            return type;
        }

        private static IReadOnlyList<TransmissionType> RequireTransmissionTypes(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    section.Line,
                    Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return Array.Empty<TransmissionType>();
            }

            var tokens = entry.Value.Split(',');
            var values = new List<TransmissionType>(tokens.Length);
            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i].Trim();
                if (token.Length == 0)
                    continue;

                if (!TransmissionTypes.TryParse(token, out var type))
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        entry.Line,
                        Localized("Unsupported transmission type '{0}' in supported_types. Valid values: atc, cvt, dct, manual.", token)));
                    continue;
                }

                values.Add(type);
            }

            if (values.Count == 0)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    entry.Line,
                    Localized("Key '{0}' must contain at least one transmission type.", key)));
            }

            return values;
        }

        private static void ValidateTransmissionTypes(
            Section transmissionSection,
            TransmissionType primaryType,
            IReadOnlyList<TransmissionType> supportedTypes,
            List<VehicleTsvIssue> issues)
        {
            if (!transmissionSection.Entries.ContainsKey("primary_type")
                || !transmissionSection.Entries.ContainsKey("supported_types"))
            {
                return;
            }

            var line = transmissionSection.Line;
            if (transmissionSection.Entries.TryGetValue("supported_types", out var supportedEntry))
                line = supportedEntry.Line;
            else if (transmissionSection.Entries.TryGetValue("primary_type", out var primaryEntry))
                line = primaryEntry.Line;

            if (!TransmissionTypes.TryValidate(primaryType, supportedTypes, out var validationError))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    line,
                    Localized(validationError)));
            }
        }

        private static AutomaticDrivelineTuning BuildAutomaticTuning(
            Section transmission,
            Section? transmissionAtc,
            Section? transmissionDct,
            Section? transmissionCvt,
            IReadOnlyList<TransmissionType> supportedTypes,
            float idleRpm,
            float revLimiter,
            List<VehicleTsvIssue> issues)
        {
            var atc = AutomaticDrivelineTuning.Default.Atc;
            var dct = AutomaticDrivelineTuning.Default.Dct;
            var cvt = AutomaticDrivelineTuning.Default.Cvt;

            var supportsAtc = Contains(supportedTypes, TransmissionType.Atc);
            var supportsDct = Contains(supportedTypes, TransmissionType.Dct);
            var supportsCvt = Contains(supportedTypes, TransmissionType.Cvt);

            if (supportsAtc)
            {
                if (transmissionAtc == null)
                {
                    AddTransmissionIssue(
                        issues,
                        transmission,
                        key: null,
                        Localized("Missing required section [transmission_atc] for supported type 'atc'."));
                }
                else
                {
                    atc = new AtcDrivelineTuning(
                        creepAccelKphPerSecond: RequireFloatRange(transmissionAtc, "creep_accel_kphps", 0f, 12f, issues),
                        launchCouplingMin: RequireFloatRange(transmissionAtc, "launch_coupling_min", 0f, 1f, issues),
                        launchCouplingMax: RequireFloatRange(transmissionAtc, "launch_coupling_max", 0f, 1f, issues),
                        lockSpeedKph: RequireFloatRange(transmissionAtc, "lock_speed_kph", 2f, 300f, issues),
                        lockThrottleMin: RequireFloatRange(transmissionAtc, "lock_throttle_min", 0f, 1f, issues),
                        shiftReleaseCoupling: RequireFloatRange(transmissionAtc, "shift_release_coupling", 0f, 1f, issues),
                        engageRate: RequireFloatRange(transmissionAtc, "engage_rate", 0.1f, 80f, issues),
                        disengageRate: RequireFloatRange(transmissionAtc, "disengage_rate", 0.1f, 80f, issues));
                    if (atc.LaunchCouplingMin > atc.LaunchCouplingMax)
                    {
                        AddTransmissionIssue(
                            issues,
                            transmissionAtc,
                            "launch_coupling_max",
                            Localized("launch_coupling_max must be greater than or equal to launch_coupling_min in [transmission_atc]."));
                    }
                }
            }
            else if (transmissionAtc != null)
            {
                AddTransmissionWarning(
                    issues,
                    transmissionAtc,
                    Localized("Section [transmission_atc] is unused because supported_types does not include 'atc'."));
            }

            if (supportsDct)
            {
                if (transmissionDct == null)
                {
                    AddTransmissionIssue(
                        issues,
                        transmission,
                        key: null,
                        Localized("Missing required section [transmission_dct] for supported type 'dct'."));
                }
                else
                {
                    dct = new DctDrivelineTuning(
                        launchCouplingMin: RequireFloatRange(transmissionDct, "launch_coupling_min", 0f, 1f, issues),
                        launchCouplingMax: RequireFloatRange(transmissionDct, "launch_coupling_max", 0f, 1f, issues),
                        lockSpeedKph: RequireFloatRange(transmissionDct, "lock_speed_kph", 2f, 300f, issues),
                        lockThrottleMin: RequireFloatRange(transmissionDct, "lock_throttle_min", 0f, 1f, issues),
                        shiftOverlapCoupling: RequireFloatRange(transmissionDct, "shift_overlap_coupling", 0f, 1f, issues),
                        engageRate: RequireFloatRange(transmissionDct, "engage_rate", 0.1f, 80f, issues),
                        disengageRate: RequireFloatRange(transmissionDct, "disengage_rate", 0.1f, 80f, issues));
                    if (dct.LaunchCouplingMin > dct.LaunchCouplingMax)
                    {
                        AddTransmissionIssue(
                            issues,
                            transmissionDct,
                            "launch_coupling_max",
                            Localized("launch_coupling_max must be greater than or equal to launch_coupling_min in [transmission_dct]."));
                    }
                }
            }
            else if (transmissionDct != null)
            {
                AddTransmissionWarning(
                    issues,
                    transmissionDct,
                    Localized("Section [transmission_dct] is unused because supported_types does not include 'dct'."));
            }

            if (supportsCvt)
            {
                if (transmissionCvt == null)
                {
                    AddTransmissionIssue(
                        issues,
                        transmission,
                        key: null,
                        Localized("Missing required section [transmission_cvt] for supported type 'cvt'."));
                }
                else
                {
                    cvt = new CvtDrivelineTuning(
                        ratioMin: RequireFloatRange(transmissionCvt, "ratio_min", 0.1f, 8f, issues),
                        ratioMax: RequireFloatRange(transmissionCvt, "ratio_max", 0.2f, 10f, issues),
                        targetRpmLow: RequireFloatRange(transmissionCvt, "target_rpm_low", idleRpm, revLimiter, issues),
                        targetRpmHigh: RequireFloatRange(transmissionCvt, "target_rpm_high", idleRpm, revLimiter, issues),
                        ratioChangeRate: RequireFloatRange(transmissionCvt, "ratio_change_rate", 0.1f, 20f, issues),
                        launchCouplingMin: RequireFloatRange(transmissionCvt, "launch_coupling_min", 0f, 1f, issues),
                        launchCouplingMax: RequireFloatRange(transmissionCvt, "launch_coupling_max", 0f, 1f, issues),
                        lockSpeedKph: RequireFloatRange(transmissionCvt, "lock_speed_kph", 2f, 300f, issues),
                        lockThrottleMin: RequireFloatRange(transmissionCvt, "lock_throttle_min", 0f, 1f, issues),
                        creepAccelKphPerSecond: RequireFloatRange(transmissionCvt, "creep_accel_kphps", 0f, 12f, issues),
                        shiftHoldCoupling: RequireFloatRange(transmissionCvt, "shift_hold_coupling", 0f, 1f, issues),
                        engageRate: RequireFloatRange(transmissionCvt, "engage_rate", 0.1f, 80f, issues),
                        disengageRate: RequireFloatRange(transmissionCvt, "disengage_rate", 0.1f, 80f, issues));
                    if (cvt.RatioMax < cvt.RatioMin)
                    {
                        AddTransmissionIssue(
                            issues,
                            transmissionCvt,
                            "ratio_max",
                            Localized("ratio_max must be greater than or equal to ratio_min in [transmission_cvt]."));
                    }
                    if (cvt.TargetRpmHigh < cvt.TargetRpmLow)
                    {
                        AddTransmissionIssue(
                            issues,
                            transmissionCvt,
                            "target_rpm_high",
                            Localized("target_rpm_high must be greater than or equal to target_rpm_low in [transmission_cvt]."));
                    }
                    if (cvt.LaunchCouplingMin > cvt.LaunchCouplingMax)
                    {
                        AddTransmissionIssue(
                            issues,
                            transmissionCvt,
                            "launch_coupling_max",
                            Localized("launch_coupling_max must be greater than or equal to launch_coupling_min in [transmission_cvt]."));
                    }
                }
            }
            else if (transmissionCvt != null)
            {
                AddTransmissionWarning(
                    issues,
                    transmissionCvt,
                    Localized("Section [transmission_cvt] is unused because supported_types does not include 'cvt'."));
            }

            return new AutomaticDrivelineTuning(atc, dct, cvt);
        }

        private static void AddTransmissionIssue(
            List<VehicleTsvIssue> issues,
            Section transmission,
            string? key,
            string message)
        {
            var line = transmission.Line;
            if (!string.IsNullOrWhiteSpace(key))
            {
                var lookupKey = key!;
                if (transmission.Entries.TryGetValue(lookupKey, out var entry))
                    line = entry.Line;
            }
            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, line, message));
        }

        private static void AddTransmissionWarning(
            List<VehicleTsvIssue> issues,
            Section transmission,
            string message)
        {
            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Warning, transmission.Line, message));
        }

        private static bool Contains(IReadOnlyList<TransmissionType> values, TransmissionType expected)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (values[i] == expected)
                    return true;
            }

            return false;
        }

        private static bool TryBuildTorqueCurve(
            Section torqueCurveSection,
            float idleRpm,
            float revLimiter,
            float peakTorqueRpm,
            float idleTorque,
            float peakTorque,
            float redlineTorque,
            List<VehicleTsvIssue> issues,
            out string? presetName,
            out float[] rpmPoints,
            out float[] torquePoints)
        {
            presetName = null;
            var merged = new SortedDictionary<float, float>();

            if (torqueCurveSection.Entries.TryGetValue("preset", out var presetEntry))
            {
                var rawPreset = presetEntry.Value.Trim();
                if (!PresetCatalog.TryNormalize(rawPreset, out var normalizedPreset))
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        presetEntry.Line,
                        Localized("Unknown torque curve preset '{0}'. Valid values: {1}.", rawPreset, PresetCatalog.NamesText)));
                    rpmPoints = Array.Empty<float>();
                    torquePoints = Array.Empty<float>();
                    return false;
                }

                presetName = normalizedPreset;
                var presetPoints = CurveFactory.BuildPreset(
                    normalizedPreset,
                    idleRpm,
                    revLimiter,
                    peakTorqueRpm,
                    idleTorque,
                    peakTorque,
                    redlineTorque);
                for (var i = 0; i < presetPoints.Count; i++)
                    merged[presetPoints[i].Rpm] = presetPoints[i].TorqueNm;
            }

            foreach (var entryPair in torqueCurveSection.Entries)
            {
                if (string.Equals(entryPair.Key, "preset", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!TryParseRpmKey(entryPair.Key, out var rpm))
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        entryPair.Value.Line,
                        Localized("Invalid torque curve key '{0}'. Use format like 2000rpm=200.", entryPair.Key)));
                    continue;
                }

                if (!TryParseFloat(entryPair.Value.Value, out var torqueNm))
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        entryPair.Value.Line,
                        Localized("Invalid torque value '{0}' for '{1}'.", entryPair.Value.Value, entryPair.Key)));
                    continue;
                }

                if (rpm < 300f || rpm > 25000f)
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        entryPair.Value.Line,
                        Localized("Torque curve RPM '{0:F0}' must be between 300 and 25000.", rpm)));
                    continue;
                }

                if (torqueNm < 0f || torqueNm > 5000f)
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        entryPair.Value.Line,
                        Localized("Torque value '{0:F1}' must be between 0 and 5000 Nm.", torqueNm)));
                    continue;
                }

                merged[rpm] = torqueNm;
            }

            if (HasErrors(issues))
            {
                rpmPoints = Array.Empty<float>();
                torquePoints = Array.Empty<float>();
                return false;
            }

            if (merged.Count < 2)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    torqueCurveSection.Line,
                    Localized("Section [torque_curve] must define at least two RPM points, or a preset with enough points.")));
                rpmPoints = Array.Empty<float>();
                torquePoints = Array.Empty<float>();
                return false;
            }

            rpmPoints = new float[merged.Count];
            torquePoints = new float[merged.Count];
            var index = 0;
            foreach (var point in merged)
            {
                rpmPoints[index] = point.Key;
                torquePoints[index] = point.Value;
                index++;
            }

            return true;
        }

        private static bool TryParseRpmKey(string key, out float rpm)
        {
            rpm = 0f;
            if (string.IsNullOrWhiteSpace(key))
                return false;

            var trimmed = key.Trim();
            if (!trimmed.EndsWith("rpm", StringComparison.OrdinalIgnoreCase))
                return false;

            var numberPart = trimmed.Substring(0, trimmed.Length - 3);
            return TryParseFloat(numberPart, out rpm);
        }

        private static float ReadFloat(Dictionary<string, string> values, string key, float defaultValue)
        {
            if (values.TryGetValue(key, out var raw) && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return value;
            return defaultValue;
        }

        private static int ReadInt(Dictionary<string, string> values, string key, int defaultValue)
        {
            if (values.TryGetValue(key, out var raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return value;
            return defaultValue;
        }

        private static bool ReadBool(Dictionary<string, string> values, string key, bool defaultValue)
        {
            if (!values.TryGetValue(key, out var raw))
                return defaultValue;
            if (bool.TryParse(raw, out var b))
                return b;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                return i != 0;
            return defaultValue;
        }
    }
}


