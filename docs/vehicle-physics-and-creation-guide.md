# Top Speed Vehicle Creation and Physics Guide

## Table of Contents

[1. Introduction](#sec-1-introduction)
[2. How Vehicle Physics Works in Top Speed](#sec-2-how-vehicle-physics-works-in-top-speed)
[2.1 The Big Picture](#sec-2-1-the-big-picture)
[2.2 Units Used by the Game](#sec-2-2-units-used-by-the-game)
[2.3 The Main Acceleration Flow (Plain Language)](#sec-2-3-the-main-acceleration-flow-plain-language)
[2.4 RPM, Torque, and Why Some Gears Feel Stronger Than Others](#sec-2-4-rpm-torque-and-why-some-gears-feel-stronger-than-others)
[2.4.1 RPM](#sec-2-4-1-rpm)
[2.4.2 Torque](#sec-2-4-2-torque)
[2.5 Torque Curve Shape and Shift Recovery](#sec-2-5-torque-curve-shape-and-shift-recovery)
[2.6 Gears, Final Drive, and Effective Ratio](#sec-2-6-gears-final-drive-and-effective-ratio)
[2.7 Drag and Rolling Resistance](#sec-2-7-drag-and-rolling-resistance)
[2.8 Braking and Coasting](#sec-2-8-braking-and-coasting)
[2.9 Steering, Grip, and Stability](#sec-2-9-steering-grip-and-stability)
[2.10 Surface Behavior](#sec-2-10-surface-behavior)
[2.11 Manual vs Automatic Transmission](#sec-2-11-manual-vs-automatic-transmission)
[3. Creating a Custom Vehicle Package](#sec-3-creating-a-custom-vehicle-package)
[3.1 Folder Layout and Discovery](#sec-3-1-folder-layout-and-discovery)
[3.2 Strict File Format Rules (Very Important)](#sec-3-2-strict-file-format-rules-very-important)
[3.3 Required and Optional Sections](#sec-3-3-required-and-optional-sections)
[3.4 Example `.tsv` Vehicle File (Full Sectioned Format)](#sec-3-4-example-tsv-vehicle-file-full-sectioned-format)
[3.5 Sound Path Rules and Safety Rules](#sec-3-5-sound-path-rules-and-safety-rules)
[3.6 Validation Behavior and Error Messages](#sec-3-6-validation-behavior-and-error-messages)
[3.7 Practical Tuning Workflow for Beginners](#sec-3-7-practical-tuning-workflow-for-beginners)
[4. Parameter Reference (Grouped by Section)](#sec-4-parameter-reference-grouped-by-section)
[4.1 `[meta]` Section](#sec-4-1-meta-section)
[4.2 `[sounds]` Section](#sec-4-2-sounds-section)
[4.3 `[general]` Section](#sec-4-3-general-section)
[4.4 `[engine]` Section](#sec-4-4-engine-section)
[4.4.1 `[torque]` Section](#sec-4-4-1-torque-section)
[4.4.2 `[torque_curve]` Section](#sec-4-4-2-torque-curve-section)
[4.4.3 `[transmission]` Section](#sec-4-4-3-transmission-section)
[4.4.4 `[transmission_atc]` Section](#sec-4-4-4-transmission-atc-section)
[4.4.5 `[transmission_dct]` Section](#sec-4-4-5-transmission-dct-section)
[4.4.6 `[transmission_cvt]` Section](#sec-4-4-6-transmission-cvt-section)
[4.5 `[drivetrain]` Section](#sec-4-5-drivetrain-section)
[4.6 `[gears]` Section](#sec-4-6-gears-section)
[4.7 `[steering]` Section](#sec-4-7-steering-section)
[4.8 `[tire_model]` Section](#sec-4-8-tire-model-section)
[4.9 `[dynamics]` Section](#sec-4-9-dynamics-section)
[4.10 `[dimensions]` Section](#sec-4-10-dimensions-section)
[4.11 `[tires]` Section](#sec-4-11-tires-section)
[4.12 `[policy]` Section (Optional, Automatic Transmission Only)](#sec-4-12-policy-section)
[5. Class Baseline Presets (Steering, Tire Model, Dynamics)](#sec-5-class-baseline-presets)
[6. What Was Removed From the Old Format](#sec-6-what-was-removed-from-the-old-format)
[7. Tuning Advice and Common Problems](#sec-7-tuning-advice-and-common-problems)
[8. Final Notes for Authors](#sec-8-final-notes-for-authors)

<a id="sec-1-introduction"></a>
## 1. Introduction

This guide explains how vehicles work in the current Top Speed rewrite and how to create custom vehicles using the new strict `.tsv` vehicle package format (TopSpeedVehicle). It is written for beginners, especially blind players and modders using screen readers, and it assumes only basic knowledge of acceleration, braking, and gears.

The main goal is practical understanding. You should be able to read this document, understand what the game is simulating, create a valid vehicle file, and tune the vehicle so it feels good in gameplay without needing advanced math or real-world engineering training.

Top Speed now uses a force-based driving model. The game calculates acceleration from engine torque, gearing, drivetrain efficiency, tire circumference, traction, drag, rolling resistance, and braking forces. Some parameter names are inherited from older versions of the game, but the custom vehicle format itself is now fully redesigned and strict. There is no backward compatibility with old vehicle files.

That means three important things for authors. First, all values are entered directly as real numbers, not encoded legacy numbers that get divided by 100. Second, every parameter must be inside a supported section. Third, invalid or extreme values are rejected with line-aware error messages instead of being silently accepted.

This guide is split into five parts. It starts with the physics model used by the game, then explains how to create a custom vehicle package and file, then explains the parser and validation rules, then gives a detailed parameter reference grouped by section with allowed value ranges and tuning advice, and finally provides class baseline presets for the modern steering and tire dynamics model.

<a id="sec-2-how-vehicle-physics-works-in-top-speed"></a>
## 2. How Vehicle Physics Works in Top Speed

<a id="sec-2-1-the-big-picture"></a>
## 2.1 The Big Picture

When a vehicle accelerates in Top Speed, the game is not simply adding a fixed speed amount. It builds acceleration from forces. The engine produces torque, the torque is multiplied by the current gear and final drive, that torque becomes wheel force, and the wheel force is limited by traction. After that, the game subtracts forces that resist motion, mainly rolling resistance and aerodynamic drag. The remaining force is converted into acceleration by dividing by vehicle mass.

This is why two vehicles with similar top speed targets can feel very different. If one vehicle is heavy, tall-geared, and has low torque at the RPM where it lands after a shift, it can feel lazy even if its `max_speed` value is high. Another vehicle can feel very quick with a lower `max_speed` reference if it has short gearing, stronger midrange torque, and lower mass.

The game also runs a separate steering and lateral movement model. Steering is influenced by `steering_response`, `max_steer_deg`, `wheelbase`, and high-speed window values, while actual sideways response is limited and shaped by `[tire_model]` and `[dynamics]` values. At high speed, the shared steering model now applies built-in attenuation first, then applies `high_speed_steer_gain` only as a bounded boost to that attenuated baseline. This keeps high-speed steering stable while still allowing class tuning.

Transmission behavior is now split into two layers. The first layer is vehicle definition (`[transmission]` plus type-specific `[transmission_*]` sections), which defines what transmission family the vehicle supports and how that family couples engine RPM to wheel RPM. The second layer is automatic shift policy (`[policy]`), which decides *when* automatic mode shifts. Policy improves decisions, but it cannot create engine power that is not there.

Manual and automatic families now follow different driveline physics. Manual mode uses clutch state and can stall at low speed/high load when clutch is engaged in an unsuitable gear. Automatic families use type-specific coupling models (ATC, DCT, CVT), including launch coupling behavior, lock thresholds, and in ATC/CVT the low-speed creep behavior.

<a id="sec-2-2-units-used-by-the-game"></a>
## 2.2 Units Used by the Game

Most gameplay speed values are expressed in kilometers per hour. Physics calculations internally use SI-style units in many places, especially meters, seconds, kilograms, and Newton-meters.

As a vehicle author, you do not need to convert everything manually, but it is important to enter values in the expected units. Examples include `mass_kg` in kilograms, `peak_torque` in Newton-meters, `wheelbase` in meters, `max_steer_deg` in degrees, and `frontal_area` in square meters.

The new custom vehicle format does not use encoded legacy values. You write the actual value directly. For example, `max_speed=170` means 170 km/h. `steering_response=1.8` means 1.8, not 180. `surface_traction_factor=0.10` means 0.10, not 10.

<a id="sec-2-3-the-main-acceleration-flow-plain-language"></a>
## 2.3 The Main Acceleration Flow (Plain Language)

The game uses a sequence of calculations each frame while throttle is applied.

The first step is determining the engine RPM for the current vehicle speed and gear. RPM depends on road speed, tire circumference, current gear ratio, final drive ratio, and driveline coupling mode (locked, blended, or disengaged). At low speed under throttle, a launch RPM floor can help prevent the engine from dropping too low and feeling weak right off the line.

The next step is reading engine torque from the engine torque curve. The torque curve can be built from a preset shape plus overrides in `[torque_curve]`, or from direct per-RPM points. It still works with the core torque values (`idle_torque`, `peak_torque`, `peak_torque_rpm`, and `redline_torque`) and the RPM range between `idle_rpm` and `rev_limiter`.

That engine torque is multiplied by the current gear ratio and the `final_drive` ratio, then reduced by `drivetrain_efficiency`. This gives wheel torque. Wheel torque is then converted into wheel force using tire radius derived from tire circumference.

The wheel force is capped by grip. Even if the engine could theoretically push harder, the tires can only transmit so much force to the road. That is where `tire_grip`, surface behavior, and related handling values matter.

Finally, the game subtracts resistance forces. Rolling resistance acts all the time and is noticeable at lower speeds. Aerodynamic drag grows rapidly with speed and becomes dominant near top speed. The remaining force becomes acceleration after dividing by `mass_kg`.

The result is a system where low-speed acceleration, mid-speed pull, and high-speed pull can all be tuned differently using different parameters.

<a id="sec-2-4-rpm-torque-and-why-some-gears-feel-stronger-than-others"></a>
## 2.4 RPM, Torque, and Why Some Gears Feel Stronger Than Others

<a id="sec-2-4-1-rpm"></a>
## 2.4.1 RPM

RPM is engine speed in revolutions per minute. RPM is not power by itself, but it controls where the engine is on the torque curve. If a shift drops RPM too far below the strong part of the curve, the next gear can feel weak even if the vehicle has a high torque peak on paper.

This is why RPM drop after an upshift is normal but must be usable. A good upshift causes RPM to drop and then recover. A bad upshift drops RPM into a weak zone and the vehicle feels flat or stalls in that gear.

<a id="sec-2-4-2-torque"></a>
## 2.4.2 Torque

Torque is twisting force at the engine. In the game, torque becomes acceleration only after gearing and drivetrain efficiency are applied, and then after traction and resistance are considered.

If a vehicle feels too strong everywhere, reducing `peak_torque` can help, but it is not the only option. `power_factor`, gear ratios, final drive, and mass are often better gameplay-balance controls because they can shape the feel without destroying the engine character.

If a vehicle is fine in lower gears but too strong at high speed, lowering `redline_torque` or increasing drag is usually more targeted than lowering `peak_torque`.

The runtime also distinguishes gross and net engine output. Gross output is engine-produced torque/power before internal losses. Net output subtracts friction and engine-braking losses. Near idle, net horsepower can be low or zero while gross torque still exists to hold idle behavior.

<a id="sec-2-5-torque-curve-shape-and-shift-recovery"></a>
## 2.5 Torque Curve Shape and Shift Recovery

`idle_torque`, `peak_torque`, `peak_torque_rpm`, and `redline_torque` together define the shape of the engine curve.

The new split between `[torque]` and `[torque_curve]` gives two authoring paths:

- Fast path: choose a named preset in `[torque_curve]` and optionally override only a few RPM points.
- Full-control path: provide your own RPM points directly (for example `2000rpm=220`).

If both are present, preset points are loaded first and your explicit RPM lines override matching RPMs or add new ones. This is useful when you want a stable baseline shape but still need exact behavior near shift landing RPM.

Lower `peak_torque_rpm` generally makes a vehicle easier to pull in taller gears because the strong torque band starts earlier. Higher `peak_torque_rpm` makes the vehicle feel more high-rev and can punish early upshifts if the next gear lands too low.

Higher `redline_torque` makes the vehicle continue pulling strongly near the top of each gear. Lower `redline_torque` creates a more obvious fade near the rev limiter and can be used to calm high-gear acceleration without heavily changing launch.

<a id="sec-2-6-gears-final-drive-and-effective-ratio"></a>
## 2.6 Gears, Final Drive, and Effective Ratio

Each forward gear has its own ratio. The game multiplies that ratio by `final_drive` to get the effective ratio for wheel torque and RPM mapping.

A higher effective ratio means stronger torque multiplication but more RPM at the same road speed. A lower effective ratio means lower RPM at the same road speed and less wheel torque.

Changing `final_drive` affects every forward gear at once. This makes it one of the strongest tuning controls in the game. A small `final_drive` increase can fix weak high gears, but it can also make lower gears too aggressive and cause the vehicle to reach the rev limiter earlier. A `final_drive` decrease can calm acceleration, but it can also make upper gears feel dead if the engine does not have enough torque at the resulting RPM.

Because of this, high-gear tuning is often a combination of final drive, torque curve shape, drag, and transmission policy.

<a id="sec-2-7-drag-and-rolling-resistance"></a>
## 2.7 Drag and Rolling Resistance

Rolling resistance is a constant-like force that always fights motion. It is influenced by `rolling_resistance` and affects low, medium, and high speed, though it is usually most noticeable before aerodynamic drag becomes large.

For beginners, the easiest way to think about rolling resistance is this: it is the \"always-on drag\" from tires and road contact. Even at moderate speed, the vehicle has to spend some engine force just to keep moving. If you raise `rolling_resistance`, the vehicle may feel heavier and more reluctant to gain speed in almost every gear, not just near top speed. If you lower it too much, the vehicle may coast for too long and feel unrealistically free-rolling.

Aerodynamic drag depends on `drag_coefficient` and `frontal_area`, and it grows much more rapidly with speed. This is why a vehicle may accelerate quickly up to a point and then slowly crawl toward top speed.

This difference matters when tuning. If a vehicle feels weak from launch, through mid speed, and also near top speed, the problem is usually not aerodynamic drag alone. It is more often a combination of `power_factor`, torque curve shape, `mass_kg`, gearing, and possibly rolling resistance. If a vehicle feels fine in lower gears but loses pull mainly at high speed, then drag-related parameters (`drag_coefficient`, `frontal_area`) and high-RPM torque (`redline_torque`) become the first places to inspect.

In other words, rolling resistance mostly shapes the \"general heaviness\" of motion, while aerodynamic drag mostly shapes the \"top-speed wall\" feeling. You usually get better tuning results by deciding which of those two feelings is wrong before changing values.

This behavior is often exactly what you want for game balance. If a vehicle is too strong near top speed, increasing drag or lowering `redline_torque` is usually a clean fix. If it feels weak at all speeds, look first at `power_factor`, `mass_kg`, and gearing before blaming drag.

<a id="sec-2-8-braking-and-coasting"></a>
## 2.8 Braking and Coasting

Top Speed separates active braking from lift-off slowing.

Active braking happens when the player presses the brake. The result depends mainly on `brake_strength`, grip, and the current surface.

Lift-off slowing happens when the player releases throttle. That is engine braking, controlled by `engine_braking` and `engine_braking_torque`, along with RPM, gearing, and drivetrain efficiency.

If a vehicle feels like it slows too hard when the player stops accelerating, reduce engine braking values rather than brake strength. If the actual brake button feels weak, tune `brake_strength` instead.

<a id="sec-2-9-steering-grip-and-stability"></a>
## 2.9 Steering, Grip, and Stability

Steering response in Top Speed is built from several parameters working together. `steering_response` acts like steering strength. `max_steer_deg` limits absolute steering angle. `wheelbase` changes how steering angle maps to path curvature. High-speed window values (`high_speed_steer_gain`, `high_speed_steer_start_kph`, `high_speed_steer_full_kph`) shape how much attenuation is applied as speed rises. Grip and dynamics parameters then decide how much of the request can actually become lateral motion.

This means handling balance should not be tuned by one value alone. If a vehicle is unrealistically agile at high speed, lowering `high_speed_steer_gain`, lowering `max_steer_deg`, or increasing `high_speed_stability` is usually the first step. If it still corners too well, lower `lateral_grip` or reduce `corner_stiffness_front` and `corner_stiffness_rear`. If it loses traction too easily under power or braking, inspect `tire_grip` and `combined_grip_penalty`.

<a id="sec-2-10-surface-behavior"></a>
## 2.10 Surface Behavior

The game applies surface-specific modifiers for asphalt, gravel, water, sand, and snow. Those modifiers interact with the vehicle baseline values.

The `surface_traction_factor` and `deceleration` parameters still exist in the format because they affect baseline behavior and some surface-related calculations, but they are not your main modern tuning tools for a vehicle's overall handling quality. In most cases, the most meaningful tuning for traction and cornering feel comes from `tire_grip`, `lateral_grip`, `brake_strength`, `engine_braking`, and the engine/drivetrain setup.

For beginners, this is an important trap to avoid: if a vehicle slips too much on asphalt, do not immediately start changing `surface_traction_factor`. That parameter sounds like the obvious fix, but in the current model it is more of a baseline reference than a simple \"more grip\" slider. Start with `tire_grip` for forward traction and braking grip, and use `lateral_grip` for cornering feel.

Also remember that surfaces amplify or expose weaknesses in your tune. A vehicle with too much torque and weak traction may feel acceptable on asphalt but become almost impossible to manage on gravel or wet surfaces. A vehicle with very strong engine braking may feel responsive on asphalt but feel too abrupt on low-grip surfaces. This is why surface testing is useful after the basic asphalt tune is finished.

<a id="sec-2-11-manual-vs-automatic-transmission"></a>
## 2.11 Manual vs Automatic Transmission

Transmission type now has explicit families:

- `manual`: player clutch + player shifts.
- `atc`: automatic torque-converter style family.
- `dct`: automatic dual-clutch style family.
- `cvt`: automatic continuously variable ratio family.

Each vehicle declares a `primary_type` and `supported_types` in `[transmission]`. The parser allows either one type, or two types where one is `manual` and the other is exactly one automatic family (`atc` or `dct` or `cvt`).

Manual mode allows the player to shift whenever they choose, but the clutch and stall logic now matter. With clutch released (high driveline coupling), the engine is mechanically tied to wheel speed. With clutch pressed, coupling drops and engine RPM can move more freely. At low speed with high load and insufficient throttle in manual, the engine can stall.

Automatic mode uses the selected family's coupling model and the policy system. The family model controls how coupling behaves (launch slip, lock behavior, creep, CVT ratio logic). The policy controls shift timing, delays, top-speed gear intent, and anti-hunting behavior.

ATC behavior emphasizes smoother launch and creep. It uses launch coupling bounds at very low speed, can release coupling during shifts, and can hard-lock only when lock criteria are met (speed, throttle, and low enough RPM slip). DCT behaves more directly with fast coupling and no creep, plus shift overlap coupling during shifts. CVT keeps ratio inside a configured ratio window and targets an RPM band (`target_rpm_low` to `target_rpm_high`) based on throttle.

Policy improves shift decisions, but it does not fix weak physics. If a gear cannot physically pull because torque, drag, or gearing are wrong, policy can only avoid that gear or delay entry into it.

For tuning work, manual mode is still the best diagnostic tool because it exposes raw powertrain behavior. If manual feels healthy but automatic shifts too early, hunts, or enters overdrive too soon, tune `[policy]` and the relevant `[transmission_*]` section for that automatic family.

<a id="sec-3-creating-a-custom-vehicle-package"></a>
## 3. Creating a Custom Vehicle Package

<a id="sec-3-1-folder-layout-and-discovery"></a>
## 3.1 Folder Layout and Discovery

Custom vehicles are discovered from subfolders under the `Vehicles` folder, similar to track discovery. The game searches recursively and picks the first `.tsv` file it finds in each vehicle folder (sorted by file name). The file does not need to be named `vehicle.tsv`; any `.tsv` filename is valid.

A simple example structure might look like this:

```text
Vehicles/
  TouringSedan/
    touring_sedan.tsv
    engine.wav
    start.wav
    horn.wav
    crash1.wav
    crash2.wav
    brake.wav
    backfire1.wav
    backfire2.wav
```

The vehicle selection menu uses the custom vehicle metadata name from the file (`[meta] name`) for display. The `version` and `description` values are loaded and stored, but the menu item text is based on the name.

<a id="sec-3-2-strict-file-format-rules-very-important"></a>
## 3.2 Strict File Format Rules (Very Important)

The new custom vehicle format is strict by design. There is no backward compatibility mode for legacy vehicle files.

Every parameter must be inside a supported section. Top-level parameters are rejected. Unknown sections are rejected. Unknown keys are rejected. Duplicate sections are rejected. Duplicate keys inside a section are rejected.

Errors are line-aware and explain what is wrong, which makes debugging much easier for screen-reader users than silent fallback behavior.

The format uses `key=value` lines inside sections, and comments start with `;` or `#`.

<a id="sec-3-3-required-and-optional-sections"></a>
## 3.3 Required and Optional Sections

The following sections are supported by the parser.

`[meta]`, `[sounds]`, `[general]`, `[engine]`, `[torque]`, `[torque_curve]`, `[transmission]`, `[drivetrain]`, `[gears]`, `[steering]`, `[tire_model]`, `[dynamics]`, `[dimensions]`, and `[tires]` are always required. `[policy]` is optional.

`[transmission_atc]`, `[transmission_dct]`, and `[transmission_cvt]` are conditionally required based on `supported_types`:

- If `supported_types` contains `atc`, `[transmission_atc]` is required.
- If `supported_types` contains `dct`, `[transmission_dct]` is required.
- If `supported_types` contains `cvt`, `[transmission_cvt]` is required.

If one of these sections exists but the matching type is not in `supported_types`, the parser reports a warning that the section is unused.

If a required section is missing, the file fails to load and the vehicle is skipped from custom vehicle discovery.

<a id="sec-3-4-example-tsv-vehicle-file-full-sectioned-format"></a>
## 3.4 Example `.tsv` Vehicle File (Full Sectioned Format)

This example uses direct values, grouped sections, and an optional policy. It also demonstrates multi-sound lists for `crash` and `backfire`.

```ini
; Example custom vehicle package file for Top Speed
; File extension: .tsv (TopSpeedVehicle)

[meta]
name=Example Touring Sedan
version=1.0
description=Balanced front-engine touring sedan with usable 8-speed overdrive gears.

[sounds]
engine=builtin6
start=builtin1
horn=builtin4
throttle=
crash=builtin3,crash1.wav,crash2.wav
brake=brake.wav
backfire=backfire1.wav,backfire2.wav
idle_freq=9000
top_freq=42000
shift_freq=30000
pitch_curve_exponent=0.85

[general]
surface_traction_factor=0.10
deceleration=0.40
max_speed=170
has_wipers=1

[engine]
idle_rpm=700
max_rpm=5600
rev_limiter=5000
auto_shift_rpm=4600
engine_braking=0.35
mass_kg=1500
drivetrain_efficiency=0.88
drag_coefficient=0.27
frontal_area=2.20
rolling_resistance=0.014
launch_rpm=1800

[torque]
engine_braking_torque=220
peak_torque=260
peak_torque_rpm=3800
idle_torque=110
redline_torque=220
power_factor=0.64
engine_inertia_kgm2=0.24
engine_friction_torque_nm=20
driveline_coupling_rate=12

[torque_curve]
preset=diesel_suv
1800rpm=240
3800rpm=260
5000rpm=220

[transmission]
primary_type=atc
supported_types=atc,manual

[transmission_atc]
creep_accel_kphps=0.70
launch_coupling_min=0.22
launch_coupling_max=0.80
lock_speed_kph=38
lock_throttle_min=0.30
shift_release_coupling=0.38
engage_rate=3.5
disengage_rate=6.0

[drivetrain]
final_drive=3.20
reverse_max_speed=35
reverse_power_factor=0.55
reverse_gear_ratio=3.20
brake_strength=1.00

[gears]
number_of_gears=8
gear_ratios=5.20,3.00,1.95,1.45,1.20,1.00,0.95,0.90

[steering]
steering_response=1.80
wheelbase=2.80
max_steer_deg=32
high_speed_stability=0.28
high_speed_steer_gain=0.92
high_speed_steer_start_kph=150
high_speed_steer_full_kph=240

[tire_model]
tire_grip=0.92
lateral_grip=1.00
combined_grip_penalty=0.72
slip_angle_peak_deg=8.0
slip_angle_falloff=1.25
turn_response=1.05
mass_sensitivity=0.75
downforce_grip_gain=0.10

[dynamics]
corner_stiffness_front=1.05
corner_stiffness_rear=0.98
yaw_inertia_scale=1.05
steering_curve=1.00
transient_damping=1.10

[dimensions]
vehicle_width=1.84
vehicle_length=4.85

[tires]
tire_width=215
tire_aspect=55
tire_rim=17
; Or provide tire_circumference directly instead of width/aspect/rim.

[policy]
top_speed_gear=6
allow_overdrive_above_game_top_speed=true
auto_upshift_rpm_fraction=0.88
auto_downshift_rpm_fraction=0.35
base_auto_shift_cooldown=0.15
upshift_delay_default=0.15
upshift_delay_5_6=0.18
upshift_delay_6_7=0.24
upshift_delay_7_8=0.30
upshift_hysteresis=0.05
min_upshift_net_accel_mps2=-0.15
top_speed_pursuit_speed_fraction=0.97
prefer_intended_top_speed_gear_near_limit=true
```

<a id="sec-3-5-sound-path-rules-and-safety-rules"></a>
## 3.5 Sound Path Rules and Safety Rules

Custom vehicle sound paths are sandboxed to the custom vehicle folder. That means normal sound file paths must be relative paths inside the same folder as the `.tsv` file (or a subfolder under it). Paths that try to escape the folder, such as `..\\outside.wav`, are rejected. Absolute paths are also rejected for custom sound files.

This rule is important for both safety and portability. A vehicle package should be self-contained so it can be shared with other players and still work. If a sound path points to a random file elsewhere on your computer, the vehicle may work only on your machine and fail for everyone else. The folder sandbox prevents that class of problem.

The exception is built-in sound references using `builtinN`, such as `builtin1`, `builtin6`, and so on. Built-in references are allowed because they do not bypass the sandbox or access user file paths. They are useful for rapid prototyping and for authors who want to focus on physics first.

In practice, a very good beginner workflow is to build the vehicle with `builtinN` sounds first, tune the physics until it feels correct, and only then replace the sounds with custom files. This reduces the number of things you are debugging at the same time.

`crash` and `backfire` support comma-separated lists. All listed sounds are initialized when the vehicle loads, and the game randomizes among them at runtime when a crash or backfire event is played. This gives better variety without changing any physics behavior.

<a id="sec-3-6-validation-behavior-and-error-messages"></a>
## 3.6 Validation Behavior and Error Messages

The custom vehicle parser is intentionally strict because earlier versions of the game allowed unrealistic and extreme values that made vehicles impossible to balance. The new parser validates both structure and value ranges.

If a file has a mistake, the parser produces line-aware errors. For example, it can report that a key is unknown, that `gear_ratios` does not match `number_of_gears`, or that `max_speed` is outside the allowed range. This makes it much easier to fix configuration problems without guessing.

\"Line-aware\" means the error points at the line where the problem was found, not just a generic failure message. This is especially helpful when a file is large or when a screen reader user wants to move directly to the problem area and correct one value at a time.

The parser also validates cross-parameter relationships. For example, `rev_limiter` must be between `idle_rpm` and `max_rpm`, and `peak_torque_rpm` must be between `idle_rpm` and `rev_limiter`. `shift_freq` must stay between `idle_freq` and `top_freq`. `gear_ratios` must be non-increasing from gear 1 to the last gear.

Transmission relationships are also validated. `primary_type` must exist in `supported_types`. `supported_types` must contain either one type, or exactly two types where one is manual and one is automatic family. Only one automatic family is allowed in a vehicle file. Required type-specific sections must exist for supported automatic types, while unused type-specific sections generate warnings.

Inside type-specific sections, additional consistency checks are enforced. For example, `launch_coupling_max` must be greater than or equal to `launch_coupling_min`; in CVT, `ratio_max` must be greater than or equal to `ratio_min`, and `target_rpm_high` must be greater than or equal to `target_rpm_low`.

For `[torque_curve]`, the parser also validates section-specific rules. You must provide at least two RPM points overall, either directly or through preset plus overrides. RPM keys must use the `NNNNrpm` format (for example `2500rpm=210`), and both RPM and torque values are range-checked.

This is important because many configuration errors are not \"bad numbers\" by themselves. A value can look valid in isolation and still be invalid when compared to another value. For example, a `peak_torque_rpm` of `7000` is not wrong by itself, but it becomes wrong if the `rev_limiter` is `6500`.

This strict behavior is not a restriction for its own sake. It exists to protect creators from invalid setups and to keep the game physics within reasonable, testable ranges.

<a id="sec-3-7-practical-tuning-workflow-for-beginners"></a>
## 3.7 Practical Tuning Workflow for Beginners

The easiest way to build a good custom vehicle is to start from a clear gameplay role. Decide whether the vehicle should be a slow beginner car, a balanced sedan, a fast but hard-to-turn supercar, a van, or a bike-like high-rev vehicle. This decision helps you avoid creating a vehicle that is accidentally strong at everything.

Start by making the vehicle load and drive. Use built-in sounds if needed. Confirm it starts, moves, brakes, and shifts. After that, tune overall acceleration and top-speed behavior with `power_factor`, torque, mass, drag, gearing, and `max_speed` as the reference target. Then tune the torque curve for how it feels before and after shifts. After that, tune handling in three passes: `[steering]` for command and speed window, `[tire_model]` for grip budget and slip shape, and `[dynamics]` for transient rotation behavior. After the physics feels correct, use `[policy]` to improve automatic shifting.

Always test manual and automatic modes separately. Manual testing tells you whether the powertrain can physically pull the gears. Automatic testing tells you whether the policy is making good choices.

When something feels wrong, change one major parameter at a time. If you change power, torque, gears, drag, and steering at once, it becomes very hard to know which change actually solved the problem.

<a id="sec-4-parameter-reference-grouped-by-section"></a>
## 4. Parameter Reference (Grouped by Section)

<a id="sec-4-1-meta-section"></a>
## 4.1 `[meta]` Section

The `[meta]` section describes the vehicle package for discovery and display. All three keys are required and must be non-empty text.

### `name`

This is the display name shown in the vehicle menu for the custom vehicle. Use a clear and human-friendly name because this is what players will hear through the menu system.

There is no numeric range because this is text, but it must not be empty.

### `version`

This is the package version string. It is not currently shown in the vehicle menu item text, but it is stored and available for future display or tooling. It is useful for managing updates to your vehicle package.

There is no numeric range because this is text, but it must not be empty.

### `description`

This is a longer text description of the vehicle package. It is not currently the menu label, but it is stored and is useful for documentation, future UI improvements, and collaboration.

There is no numeric range because this is text, but it must not be empty.

<a id="sec-4-2-sounds-section"></a>
## 4.2 `[sounds]` Section

This section controls vehicle audio assets and audio pitch behavior. It does not directly change acceleration, braking, or handling. However, good sound setup is important in Top Speed because the game is audio-heavy and relies on sound feedback for gameplay clarity.

### `engine`

Main engine sound. This is required. The value may be a relative path inside the vehicle folder or a built-in reference such as `builtin6`.

There is no numeric range because this is a sound reference. The path must resolve safely inside the vehicle folder unless it is a `builtinN` reference.

This sound usually carries most of the vehicle identity for blind players, so it is worth choosing carefully. If the physics feels right but the vehicle still feels \"wrong\" in gameplay, the engine sound pitch behavior (`idle_freq`, `top_freq`, `shift_freq`) is often the missing part.

### `start`

Engine start sound. Required. Used when the vehicle is started.

There is no numeric range because this is a sound reference. The same path safety rules apply as `engine`.

### `horn`

Horn sound. Required. Used when the horn is triggered.

There is no numeric range because this is a sound reference. The same path safety rules apply as `engine`.

### `throttle`

Optional additional throttle layer sound. This can be left empty. When present, it adds audio richness but does not change physics.

There is no numeric range because this is a sound reference. If you provide a non-empty value, it must resolve successfully.

This is often useful for vehicles that need a clearer sense of throttle application, turbo-like texture, or intake-style character. If your vehicle already sounds busy, leaving this empty is fine.

### `crash`

Crash sound or crash sound list. This key is required. You may provide a single sound or a comma-separated list. If you provide multiple sounds, all are initialized and one is chosen randomly at runtime for each crash event.

There is no numeric range because this is a sound reference or list of references. Each entry must be valid and must follow the same path safety rules. `builtinN` entries are allowed.

### `brake`

Brake sound. Required. Used for brake feedback and related tire/braking audio cues.

There is no numeric range because this is a sound reference. The same path safety rules apply as `engine`.

### `backfire`

Optional backfire sound or backfire sound list. If multiple entries are provided as a comma-separated list, the game randomizes among them when a backfire event is played.

There is no numeric range because this is a sound reference or list of references. If you provide entries, each must resolve successfully and follow path safety rules.

### `idle_freq`

Low engine audio pitch frequency anchor. This affects how the engine sound is pitched in low-speed and idle-like conditions.

Allowed range is 100 to 200000.

It does not change physics, but it strongly changes how slow or relaxed the vehicle sounds. If this is set too high, the vehicle may sound unnaturally \"busy\" even when moving slowly. If it is set too low, the engine may sound dull or too deep.

### `top_freq`

High engine audio pitch frequency anchor. This affects how the engine sound is pitched near the upper part of the RPM/speed range.

Allowed range is 100 to 200000, and it must be greater than or equal to `idle_freq`.

It does not change physics, but it strongly affects the perceived character of the engine. A very high `top_freq` can make a vehicle sound sharp, high-rev, or aggressive. A lower `top_freq` can make it sound heavier or less sporty.

### `shift_freq`

Intermediate audio frequency anchor used in engine pitch/shift-related sound behavior.

Allowed range is 100 to 200000, and it must be between `idle_freq` and `top_freq`.

It does not change acceleration or top speed, but poor values can make the vehicle sound inconsistent or unnatural. As a beginner rule, keep `shift_freq` somewhere between the other two values in a way that matches the vehicle character, then adjust by listening during real shifts.

### `pitch_curve_exponent`

RPM-to-pitch curve shape exponent for engine sound pitch mapping.

Allowed range is 0.5 to 1.5. If omitted, default is `0.85`.

`1.0` gives linear mapping over normalized RPM. Lower than `1.0` raises pitch earlier at lower RPM (more aggressive early rise). Higher than `1.0` delays pitch rise until later RPM.

<a id="sec-4-3-general-section"></a>
## 4.3 `[general]` Section

The `[general]` section contains general gameplay-facing values that do not fit cleanly into the engine or handling groups.

### `surface_traction_factor`

Baseline surface traction factor used by parts of the surface interaction logic.

Allowed range is 0.0 to 5.0.

This value is entered directly. Do not multiply by 100. For example, use `0.10`, not `10`.

In the current physics model this is not usually the strongest tuning lever for everyday grip feel. For meaningful traction and handling changes, `tire_grip` and `lateral_grip` are usually more important.

### `deceleration`

Baseline deceleration factor used by some surface-related behavior and legacy-style deceleration baselines.

Allowed range is 0.0 to 5.0.

This value is entered directly. Do not multiply by 100. For example, use `0.40`, not `40`.

If you need to change braking feel, tune `brake_strength`. If you need to change lift-off slowing, tune `engine_braking` and `engine_braking_torque`.

### `max_speed`

Forward reference speed in km/h for gameplay and automatic top-speed policy behavior.

Allowed range is 10 to 500.

This is not a strict hard cap in the current model. It is used as the main reference for top-speed behavior and transmission policy, while forward speed safety is enforced by a separate internal ceiling (`min(max_speed * 1.5, 550)` km/h).

For beginners, this should be treated as a gameplay target, not a realism promise. A real vehicle may be capable of more speed, but you can intentionally set a lower `max_speed` to protect game balance while preserving the vehicle's identity through torque, gearing, sound, and handling.

### `has_wipers`

Boolean-like flag for wiper behavior and related weather audio behavior.

This key is parsed as a boolean integer (`0` or `1`).

There is no numeric tuning range beyond that. It does not affect physics.

This is still worth setting correctly because weather audio feedback is important in an audio-based game. A road car or van will usually use `1`. Many bikes and open or non-wiper vehicles may use `0`.

<a id="sec-4-4-engine-section"></a>
## 4.4 `[engine]` Section

The `[engine]` section contains RPM limits, braking and mass values, and resistance values used in acceleration calculations. Torque-shape and engine rotational dynamics are defined in `[torque]` and `[torque_curve]`.

### `idle_rpm`

Engine idle RPM baseline.

Allowed range is 300 to 3000.

This value affects the low end of the RPM range, torque curve calculations, and how RPM-based policy fractions convert into absolute RPM. Raising it changes the meaning of some policy thresholds and can change low-speed feel.

### `max_rpm`

Maximum RPM ceiling used by the engine model for RPM handling and reporting.

Allowed range is 1000 to 20000, and it must be greater than or equal to `idle_rpm`.

This is not the same as `rev_limiter`. `max_rpm` is the overall ceiling, while `rev_limiter` is the main usable limit for power and shifting behavior.

### `rev_limiter`

Usable upper RPM limit for engine power and gear pulling.

Allowed range is 800 to 18000, and it must be between `idle_rpm` and `max_rpm`.

Lower values shorten each gear and can make high gears easier to reach but may reduce flexibility. Higher values extend each gear, but only help if the torque curve stays strong at high RPM.

### `auto_shift_rpm`

Preferred automatic shift RPM anchor.

Allowed range is 0 to 18000. It must be `0` or between `idle_rpm` and `rev_limiter`.

A value of `0` means policy/default logic derives behavior from other values. A real value gives a direct automatic shift target and is also used when policy derives defaults.

### `engine_braking`

Engine braking strength multiplier.

Allowed range is 0.0 to 1.5.

This affects lift-off deceleration, not active brake-button stopping. Too high can make the vehicle feel like it drags unnaturally when the player releases throttle.

### `mass_kg`

Vehicle mass in kilograms.

Allowed range is 20 to 10000.

Higher mass reduces acceleration for the same net force and usually makes the vehicle feel calmer but heavier. Lower mass increases responsiveness and acceleration. Mass also influences some handling feel indirectly.

### `drivetrain_efficiency`

Drivetrain efficiency multiplier representing power loss through the drivetrain.

Allowed range is 0.1 to 1.0.

Higher values send more torque to the wheels. Lower values reduce acceleration and engine braking transfer. This is useful but usually not the first tuning lever for gameplay balance.

### `drag_coefficient`

Aerodynamic drag coefficient used in the drag force calculation.

Allowed range is 0.01 to 1.5.

Lower values improve high-speed pull and top-speed reach. Higher values reduce high-speed acceleration and can be used to balance fast vehicles while keeping low-speed behavior more intact than a large torque reduction would.

### `frontal_area`

Frontal area in square meters used in the drag calculation.

Allowed range is 0.05 to 10.0.

This works together with `drag_coefficient`. Larger values increase aerodynamic drag, especially at higher speeds.

### `rolling_resistance`

Rolling resistance coefficient.

Allowed range is 0.001 to 0.1.

This affects resistance across the speed range and is especially noticeable at low and medium speed. If a vehicle feels weak everywhere, inspect this along with mass and power settings.

For road vehicles, realistic values are usually much lower than the top end of the allowed range. In practice, many normal road-vehicle tunes will sit roughly in the low hundredths. The `0.1` maximum is an intentionally generous upper limit for gameplay safety and experimentation, not a recommended target for normal cars or motorcycles.

A useful beginner test is coasting behavior. If the vehicle loses speed too quickly even when the engine and brakes are not the main issue, `rolling_resistance` may be too high. If the vehicle seems to glide too easily and never feels like it settles, it may be too low.

### `launch_rpm`

Launch RPM assist floor under throttle at low speed.

Allowed range is 0 to 18000, and it must not exceed `rev_limiter`.

Higher values can make launch feel stronger and reduce bogging. Lower values can calm launches.

<a id="sec-4-4-1-torque-section"></a>
## 4.4.1 `[torque]` Section

The `[torque]` section contains engine torque shape controls and rotational dynamics controls. This section is required.

### `engine_braking_torque`

Base engine braking torque in Newton-meters.

Allowed range is 0 to 3000.

This works together with `engine_braking` to determine lift-off slowing. If off-throttle slowing is wrong, tune these two together.

### `peak_torque`

Peak engine torque in Newton-meters.

Allowed range is 10 to 3000.

This is one of the main acceleration parameters. Larger values usually increase acceleration across much of the speed range, especially where the engine spends time near `peak_torque_rpm`.

### `peak_torque_rpm`

RPM where peak torque occurs.

Allowed range is 500 to 18000, and it must be between `idle_rpm` and `rev_limiter`.

Lower values improve midrange and recovery after upshifts. Higher values create a more high-rev character and can make tall gears harder to pull if shifts land too low.

### `idle_torque`

Torque near idle RPM.

Allowed range is 0 to 3000.

Higher values improve launch and low-RPM response. Too high can make the vehicle unrealistically strong in tall gears at low RPM.

### `redline_torque`

Torque near the rev limiter.

Allowed range is 0 to 3000.

This is one of the best targeted controls for high-gear pull. Lower it to calm upper-gear acceleration without heavily affecting low-speed launch. Raise it to let the engine continue pulling harder at high RPM.

### `power_factor`

Global power scaling multiplier for throttle-driven acceleration calculations.

Allowed range is 0.05 to 2.0.

This is one of the best gameplay-balance controls in the entire format. It lets you adjust acceleration without fully rebuilding the torque curve. If a vehicle is too dominant, lowering `power_factor` is often the cleanest first step.

### `engine_inertia_kgm2`

Engine rotational inertia in kg*m^2.

Allowed range is 0.01 to 5.0.

Higher values make RPM change more slowly when torque changes, so the engine feels heavier and calmer. Lower values make RPM rise and fall faster, which feels more reactive but can also feel jumpy if coupled with aggressive gearing.

### `engine_friction_torque_nm`

Parasitic friction torque inside the engine in Newton-meters.

Allowed range is 0 to 1000.

This contributes to negative net torque and influences low-throttle RPM behavior. Raising it increases internal loss and can reduce free-rev feel. If set too high, it can make off-throttle behavior feel harsh unless compensated by other values.

### `driveline_coupling_rate`

Blend rate between free engine RPM integration and wheel-coupled RPM.

Allowed range is 0.1 to 80.0.

Higher values make engine RPM lock to wheel speed faster through the driveline. Lower values allow more free engine behavior before RPM fully couples to wheel speed.

<a id="sec-4-4-2-torque-curve-section"></a>
## 4.4.2 `[torque_curve]` Section

The `[torque_curve]` section defines explicit torque points by RPM and optionally a base preset. This section is required.

### `preset`

Optional named base shape loaded before any explicit RPM points.

Allowed values are:

- `city_compact`
- `family_sedan`
- `sport_sedan`
- `sport_coupe`
- `grand_tourer`
- `hot_hatch`
- `muscle_v8`
- `supercar_na`
- `supercar_turbo`
- `rally_turbo`
- `diesel_suv`
- `diesel_truck`
- `supersport_bike`
- `naked_bike`

Use this when you want a stable baseline quickly, then override only a few RPM points for vehicle-specific character.

### `NNNNrpm`

Per-RPM torque points using keys like `2000rpm=220`.

RPM key range is 300 to 25000. Torque value range is 0 to 5000 Nm.

You must end with at least two total points in `[torque_curve]`. If you only use `preset`, that requirement is satisfied by preset points. If you do not use a preset, you must provide at least two explicit RPM lines.

When preset and explicit points are both present, explicit points win at matching RPM and add new points where no preset point exists.

<a id="sec-4-4-3-transmission-section"></a>
## 4.4.3 `[transmission]` Section

The `[transmission]` section defines transmission families available to the vehicle and which one starts as default.

### `primary_type`

Default active transmission type for the vehicle.

Allowed values are `atc`, `cvt`, `dct`, `manual`.

This value must also appear inside `supported_types`.

### `supported_types`

Comma-separated list of supported transmission types.

Allowed tokens are `atc`, `cvt`, `dct`, `manual`.

Validation rules:

- At least one type is required.
- At most two types are allowed.
- If two types are provided, they must be exactly one manual type plus exactly one automatic family type.
- Only one automatic family is allowed per vehicle.
- Duplicate types are rejected.

If `supported_types` includes an automatic family, the matching type-specific section becomes required:

- `atc` requires `[transmission_atc]`.
- `dct` requires `[transmission_dct]`.
- `cvt` requires `[transmission_cvt]`.

<a id="sec-4-4-4-transmission-atc-section"></a>
## 4.4.4 `[transmission_atc]` Section

ATC (automatic torque-converter style) driveline coupling parameters. This section is required when `supported_types` includes `atc`.

### `creep_accel_kphps`

Low-speed creep acceleration in km/h per second when throttle is near zero and brake is not pressed.

Allowed range is 0.0 to 12.0.

### `launch_coupling_min`

Minimum coupling factor used at launch-region speed with zero throttle.

Allowed range is 0.0 to 1.0.

### `launch_coupling_max`

Maximum coupling factor used at launch-region speed with full throttle.

Allowed range is 0.0 to 1.0 and must be greater than or equal to `launch_coupling_min`.

### `lock_speed_kph`

Speed threshold where ATC may request full lock coupling.

Allowed range is 2.0 to 300.0.

### `lock_throttle_min`

Minimum throttle needed for ATC full-lock request above lock speed.

Allowed range is 0.0 to 1.0.

### `shift_release_coupling`

Coupling target while a shift is in progress.

Allowed range is 0.0 to 1.0.

### `engage_rate`

Rate limit for coupling increase toward target.

Allowed range is 0.1 to 80.0.

### `disengage_rate`

Rate limit for coupling decrease toward target.

Allowed range is 0.1 to 80.0.

ATC notes: ATC can creep, and it uses controlled pre-lock coupling before hard lock criteria are satisfied. In runtime, hard lock is still gated by speed/throttle/coupling/slip conditions to avoid abrupt RPM behavior.

<a id="sec-4-4-5-transmission-dct-section"></a>
## 4.4.5 `[transmission_dct]` Section

DCT (dual-clutch automatic) driveline coupling parameters. This section is required when `supported_types` includes `dct`.

### `launch_coupling_min`

Minimum coupling factor used at launch-region speed with zero throttle.

Allowed range is 0.0 to 1.0.

### `launch_coupling_max`

Maximum coupling factor used at launch-region speed with full throttle.

Allowed range is 0.0 to 1.0 and must be greater than or equal to `launch_coupling_min`.

### `lock_speed_kph`

Speed threshold where DCT may request full lock coupling.

Allowed range is 2.0 to 300.0.

### `lock_throttle_min`

Minimum throttle needed for DCT full-lock request above lock speed.

Allowed range is 0.0 to 1.0.

### `shift_overlap_coupling`

Coupling target while shifting to model overlap behavior.

Allowed range is 0.0 to 1.0.

### `engage_rate`

Rate limit for coupling increase toward target.

Allowed range is 0.1 to 80.0.

### `disengage_rate`

Rate limit for coupling decrease toward target.

Allowed range is 0.1 to 80.0.

DCT notes: DCT has no automatic creep in this model and generally behaves more directly than ATC at low speed.

<a id="sec-4-4-6-transmission-cvt-section"></a>
## 4.4.6 `[transmission_cvt]` Section

CVT driveline and ratio-control parameters. This section is required when `supported_types` includes `cvt`.

### `ratio_min`

Minimum effective CVT ratio.

Allowed range is 0.1 to 8.0.

### `ratio_max`

Maximum effective CVT ratio.

Allowed range is 0.2 to 10.0 and must be greater than or equal to `ratio_min`.

### `target_rpm_low`

Low end of the CVT RPM target band.

Allowed range is `idle_rpm` to `rev_limiter`.

### `target_rpm_high`

High end of the CVT RPM target band.

Allowed range is `idle_rpm` to `rev_limiter` and must be greater than or equal to `target_rpm_low`.

### `ratio_change_rate`

How quickly CVT ratio moves toward target ratio.

Allowed range is 0.1 to 20.0.

### `launch_coupling_min`

Minimum coupling factor used at launch-region speed with zero throttle.

Allowed range is 0.0 to 1.0.

### `launch_coupling_max`

Maximum coupling factor used at launch-region speed with full throttle.

Allowed range is 0.0 to 1.0 and must be greater than or equal to `launch_coupling_min`.

### `lock_speed_kph`

Speed threshold where CVT may request full lock coupling.

Allowed range is 2.0 to 300.0.

### `lock_throttle_min`

Minimum throttle needed for CVT full-lock request above lock speed.

Allowed range is 0.0 to 1.0.

### `creep_accel_kphps`

Low-speed creep acceleration in km/h per second when throttle is near zero and brake is not pressed.

Allowed range is 0.0 to 12.0.

### `shift_hold_coupling`

Coupling target used while shifting/transitioning.

Allowed range is 0.0 to 1.0.

### `engage_rate`

Rate limit for coupling increase toward target.

Allowed range is 0.1 to 80.0.

### `disengage_rate`

Rate limit for coupling decrease toward target.

Allowed range is 0.1 to 80.0.

CVT notes: CVT dynamically selects ratio inside `[ratio_min, ratio_max]` to hold engine RPM in the target band according to throttle demand.

<a id="sec-4-5-drivetrain-section"></a>
## 4.5 `[drivetrain]` Section

The `[drivetrain]` section contains gearing and braking controls that are not part of the engine torque curve itself.

### `final_drive`

Final drive ratio applied to all forward gears.

Allowed range is 0.5 to 8.0.

This is one of the strongest tuning controls because it changes effective gearing in every forward gear. Increasing it makes all gears shorter and usually improves pull. Decreasing it makes all gears taller and can reduce acceleration or make upper gears harder to use.

Because it affects every gear, `final_drive` is often the fastest way to move a vehicle in the right direction, but it can also create side effects very quickly. A change that fixes weak 7th gear might make 1st and 2nd gear too aggressive. A change that calms launch might make top-speed gears impossible to pull.

For beginners, a good rule is to use `final_drive` for broad changes and `gear_ratios` for fine shaping. If the whole vehicle feels too short or too tall, change `final_drive`. If only one or two gears feel wrong, adjust the gear ratio list.

### `reverse_max_speed`

Maximum reverse speed in km/h.

Allowed range is 1 to 100.

The game clamps reverse speed separately from forward speed. This gives you direct control over how fast the vehicle can back up.

### `reverse_power_factor`

Reverse acceleration scaling multiplier.

Allowed range is 0.05 to 2.0.

This is a gameplay tuning value. Most vehicles should use a lower reverse power value than forward power to keep reverse behavior controllable.

### `reverse_gear_ratio`

Reverse gear ratio.

Allowed range is 0.5 to 8.0.

This affects reverse RPM and reverse torque multiplication together with `final_drive` and `reverse_power_factor`.

### `brake_strength`

Active braking strength multiplier used when the brake input is pressed.

Allowed range is 0.1 to 5.0.

This works with grip and surface behavior. If brake input feels weak, increase this. If braking is too harsh or difficult to control, reduce it.

<a id="sec-4-6-gears-section"></a>
## 4.6 `[gears]` Section

This section defines the number of forward gears and the exact gear ratio list. Both keys are required.

### `number_of_gears`

Number of forward gears.

Allowed range is 1 to 10.

This value must match the number of entries in `gear_ratios`. The parser hard-fails if they do not match.

### `gear_ratios`

Comma-separated list of forward gear ratios, one value per gear from 1st to last.

This key is required. There is no fallback or auto-generation in the custom vehicle format.

Each individual ratio must be between 0.20 and 8.00. The list must be non-increasing, which means each later gear must be the same or lower ratio than the previous one.

Higher values make shorter gears with stronger torque multiplication. Lower values make taller gears with less pull and lower RPM at the same speed. Gear ratios always interact with `final_drive`, so tune them together.

For beginner tuning, think of the gear list as the shape of the acceleration experience. Early gears control launch and low-speed response. Middle gears control the most common acceleration feel during normal driving and racing. Late gears control high-speed pull and overdrive behavior.

If an upshift causes the engine to drop into a weak RPM zone, the next gear may be too tall, the previous gear may be too short, the final drive may be too low, or the torque curve may not support that RPM. The fix is not always \"change that one ratio only.\" Always test the effect on the gears before and after it.

<a id="sec-4-7-steering-section"></a>
## 4.7 `[steering]` Section

The `[steering]` section controls steering command generation and speed-window behavior before tire-force and yaw dynamics are applied.

### `steering_response`

Steering strength multiplier.

Allowed range is 0.1 to 5.0.

This is one of the main first-pass handling controls. Raising it increases steering authority in the full speed range. Lowering it makes steering calmer and less immediate.

If steering feels weak at low and medium speed, this is usually the first value to increase. If steering feels nervous at all speeds, reduce this before touching advanced parameters.

### `high_speed_stability`

High-speed stability damping factor for lateral response.

Allowed range is 0.0 to 1.0.

Higher values calm the vehicle at speed and reduce twitchiness, especially when combined with high grip and short wheelbase setups. Too high can make the vehicle feel reluctant to rotate in fast corners.

This value should not be used as a complete substitute for proper grip and dynamics tuning. It is best used as a speed-domain stabilizer, not as a universal "fix steering" knob.

### `high_speed_steer_gain`

High-speed steering boost factor applied on top of the built-in speed attenuation.

Allowed range is 0.7 to 1.6.

In the current shared model, steering is attenuated as speed rises. `high_speed_steer_gain` does not override that attenuation. It only boosts or trims the attenuated result.

Practically, this means even high values are controlled. Most realistic tunes should stay near `0.85` to `0.98` and use `high_speed_stability`, tire grip, and dynamics values for additional control.

### `high_speed_steer_start_kph`

Speed where high-speed steering attenuation and gain blending starts.

Allowed range is 60 to 260.

Below this speed, high-speed attenuation is not active. Lower start values make high-speed behavior begin earlier in normal driving.

### `high_speed_steer_full_kph`

Speed where high-speed attenuation and gain blending is fully applied.

Allowed range is 100 to 350, and must be greater than `high_speed_steer_start_kph`.

Between start and full speed, the game blends smoothly from base steering to the high-speed steering scale. A narrow window creates faster transition. A wide window creates a more gradual transition.

### `wheelbase`

Wheelbase in meters.

Allowed range is 0.3 to 8.0.

Shorter wheelbase usually feels more responsive and more willing to rotate. Longer wheelbase usually feels calmer and less agile. It interacts strongly with `steering_response`, `max_steer_deg`, and corner stiffness balance.

For beginners, wheelbase is easy to misunderstand because it does not feel like a direct \"more turning\" or \"less turning\" knob. Instead, it changes how strongly the same steering angle bends the vehicle path. That means it changes the character of steering, not only the amount.

### `max_steer_deg`

Maximum steering angle in degrees.

Allowed range is 5 to 60.

This is one of the most direct handling controls. Lower values are useful for limiting the agility of high-speed vehicles. Higher values improve maneuverability but can make the vehicle unstable if grip and stability are too high.

<a id="sec-4-8-tire-model-section"></a>
## 4.8 `[tire_model]` Section

The `[tire_model]` section controls grip budget sharing, slip-angle shape, and mass/aero influence. These values define steady-state cornering behavior and how much traction remains while turning.

### `tire_grip`

Base tire grip coefficient for traction and braking, with influence on overall grip behavior.

Allowed range is 0.1 to 3.0.

This primarily affects acceleration traction and braking authority, but it also contributes to total grip behavior under combined load. If vehicles spin wheels too easily or feel weak under braking, this is a primary parameter.

### `lateral_grip`

Additional lateral grip tuning for turning behavior.

Allowed range is 0.1 to 3.0.

This is the main cornering-grip scalar. Raise for stronger corner hold, lower for more drift/slip character. If turning feels too weak while traction feels fine, tune this before torque values.

### `combined_grip_penalty`

How strongly lateral demand reduces available longitudinal traction.

Allowed range is 0.0 to 1.0.

Higher values create stronger trade-off between turning and acceleration/braking. Lower values allow stronger acceleration while cornering. This is a core realism-versus-arcade balance control.

### `slip_angle_peak_deg`

Slip-angle peak reference in degrees for the simplified tire response.

Allowed range is 0.5 to 20.0.

Lower values reach the peak earlier and can feel sharp but unforgiving. Higher values make slip build more gradually and can feel easier to control.

### `slip_angle_falloff`

Falloff scale after the peak slip angle.

Allowed range is 0.01 to 5.0.

Higher falloff values drop force faster after the peak, which increases punishment for over-driving steering input. Lower values keep more force after peak and feel more forgiving.

### `turn_response`

Lateral response-time scaling.

Allowed range is 0.2 to 2.5.

This scales how quickly lateral output follows the steering/lateral state. Low values can feel delayed or heavy. High values feel immediate but may become twitchy if damping is too low.

### `mass_sensitivity`

How much vehicle mass influences turn agility.

Allowed range is 0.0 to 1.0.

At higher values, heavy vehicles lose agility more clearly while light vehicles preserve responsiveness. At lower values, classes feel closer together regardless of mass.

### `downforce_grip_gain`

Speed-dependent grip gain factor.

Allowed range is 0.0 to 1.0.

Higher values add more grip as speed increases. This is useful for high-performance classes but can make high-speed behavior too dominant if combined with high `high_speed_steer_gain`.

If high-speed cornering is too strong, reduce this or reduce high-speed steering gain before reducing low-speed steering values.

<a id="sec-4-9-dynamics-section"></a>
## 4.9 `[dynamics]` Section

The `[dynamics]` section controls transient behavior: how quickly yaw builds, how steering input is shaped, and how front/rear axle authority is balanced. These values are required.

### `corner_stiffness_front`

Front axle cornering stiffness scale.

Allowed range is 0.2 to 3.0.

Higher values increase front lateral force build-up and can improve turn-in authority. Too high can create front-end bite that feels abrupt.

Relative balance matters more than absolute value. Front greater than rear generally favors stable turn-in with mild understeer tendency at the limit.

### `corner_stiffness_rear`

Rear axle cornering stiffness scale.

Allowed range is 0.2 to 3.0.

Higher values increase rear lateral authority and can help rotation. Too high relative to front can create oversteer-like rotation that is hard to recover.

For safe baseline tuning, keep rear slightly below front. For more rotation-oriented setups, narrow the gap carefully.

### `yaw_inertia_scale`

Yaw inertia multiplier applied to rotational response.

Allowed range is 0.5 to 2.0.

Higher values make yaw build and change more slowly, which feels heavier and more stable. Lower values make yaw change faster, which feels agile but can become nervous.

This parameter is especially important for class identity. Bikes and lightweight vehicles usually need lower values than heavy trucks and buses.

### `steering_curve`

Input shaping exponent for steering command.

Allowed range is 0.5 to 2.0.

Values below `1.0` make early input stronger and feel more aggressive around small steering taps. Values above `1.0` make early input softer and reserve more authority for larger inputs.

If players report that light taps cause too much response, increase this value slightly. If small taps feel dead, reduce this value slightly.

### `transient_damping`

Damping factor for lateral/yaw transients.

Allowed range is 0.0 to 6.0.

Higher values suppress oscillation and reduce overshoot after steering changes. Lower values allow freer response and stronger immediate rotation.

If vehicles keep rotating after steering release, increase this value. If vehicles feel slow to rotate or "stuck," reduce it carefully and retest with `turn_response`.

<a id="sec-4-10-dimensions-section"></a>
## 4.10 `[dimensions]` Section

The `[dimensions]` section defines physical size values used for spatial behavior and representation.

### `vehicle_width`

Vehicle width in meters.

Allowed range is 0.2 to 5.0.

This affects spatial/audio placement and vehicle size behavior, not engine power or top speed directly.

Even though it is not a power parameter, it still matters for how the vehicle feels in an audio game. Width contributes to spatial presence and how wide the vehicle seems relative to lanes and nearby traffic or objects.

### `vehicle_length`

Vehicle length in meters.

Allowed range is 0.3 to 20.0.

This also affects spatial representation and presence rather than direct acceleration physics.

For beginners, this should generally reflect the actual body length of the vehicle type you are creating, even if you are not trying to be perfectly realistic. A value that is far too short or far too long can make the vehicle feel spatially \"wrong\" even when the engine and handling are tuned well.

<a id="sec-4-11-tires-section"></a>
## 4.11 `[tires]` Section

The `[tires]` section defines tire circumference directly or provides the size triplet needed to calculate it.

You must provide either a valid `tire_circumference` or all three of `tire_width`, `tire_aspect`, and `tire_rim`. The parser hard-fails if neither form is complete and valid.

### `tire_circumference`

Tire circumference in meters.

If provided and greater than zero, it is used directly. Allowed range is 0.2 to 5.0 meters.

This value affects the speed-to-RPM relationship. Incorrect tire circumference can make gearing and RPM behavior feel wrong even when other parameters are correct.

This is one of the easiest hidden causes of confusing tuning problems. If tire circumference is too small, the engine RPM will appear higher than expected at a given speed, which can make gears seem shorter than they really are. If it is too large, RPM may seem too low and gears may feel too tall.

### `tire_width`

Tire width in millimeters used for circumference calculation when direct circumference is not provided.

Allowed range is 20 to 450.

This value is only used when circumference is being calculated from tire size. It does not act as a direct grip parameter in the current custom format. Use `tire_grip` and `lateral_grip` for actual handling tuning.

### `tire_aspect`

Tire aspect ratio (sidewall height percentage of tire width) used for circumference calculation fallback.

Allowed range is 5 to 150.

This is also used only for circumference calculation fallback, not as a direct handling-physics grip knob.

### `tire_rim`

Rim diameter in inches used for circumference calculation fallback.

Allowed range is 4 to 30.

This is also used only for circumference calculation fallback. If you already know tire circumference, providing `tire_circumference` directly is usually simpler and reduces mistakes.

If you provide the size triplet, the game calculates tire circumference automatically after validation.

<a id="sec-4-12-policy-section"></a>
## 4.12 `[policy]` Section (Optional, Automatic Transmission Only)

The `[policy]` section controls automatic shifting behavior. It does not change engine power, grip, or drag directly. Policy should be used after the vehicle can physically pull the gears you intend it to use.

The parser accepts normal policy keys and also wildcard delay keys such as `upshift_delay_6_7` and `upshift_delay_g6`.

### `top_speed_gear`

Intended forward gear for reaching the game-world top speed.

Allowed range is 1 to `number_of_gears`.

This is especially important for 7-speed and 8-speed vehicles with overdrive gears. It tells automatic mode which gear should usually be treated as the main top-speed gear.

For example, an 8-speed vehicle may be designed to reach the game top speed in 6th gear, while 7th and 8th are calmer cruising or overdrive gears. Declaring that intention in policy helps automatic mode avoid entering those higher gears too early.

### `allow_overdrive_above_game_top_speed`

Boolean policy flag for allowing gears above `top_speed_gear` near or above the top-speed region.

This key has no numeric range because it is boolean. Use `true` or `false`.

If `true`, automatic mode can use higher gears as overdrives when appropriate. If `false`, it avoids them while pursuing top speed.

This setting does not make the overdrive gear physically usable by itself. It only changes whether automatic mode is allowed to choose it. If overdrive gears still feel dead in manual mode, fix torque, drag, or gearing first.

### `base_auto_shift_cooldown`

Base automatic shift cooldown in seconds.

Allowed range is 0.0 to 2.0.

Higher values make automatic mode calmer and less likely to shift rapidly. Too high can make the transmission feel lazy.

This is a good first control for automatic behavior because it improves stability without changing the underlying powertrain. If the automatic mode feels nervous or chatters between gears, try a small increase here before changing several other policy values.

### `upshift_delay_default`

Default automatic upshift delay in seconds.

Allowed range is 0.0 to 2.0.

This is a useful way to make upshifts slower in general before adding special delays for high gears.

### `upshift_delay_X_Y`

Per-transition upshift delay in seconds for a specific adjacent upshift.

Allowed range is 0.0 to 2.0.

Use keys such as `upshift_delay_5_6=0.18` or `upshift_delay_6_7=0.24`. This is the best way to make high-gear upshifts slower without affecting lower gears.

### `upshift_delay_gX`

Per-source-gear shorthand upshift delay in seconds.

Allowed range is 0.0 to 2.0.

For example, `upshift_delay_g6=0.24` applies to upshifts out of 6th gear. Explicit transition keys are more specific and should be preferred when you need exact behavior.

### `auto_upshift_rpm_fraction`

Automatic upshift threshold as a fraction of the RPM span from idle to rev limiter.

Allowed range is 0.05 to 1.0.

This is convenient because it stays meaningful even if you later retune `idle_rpm` or `rev_limiter`.

### `auto_upshift_rpm`

Automatic upshift threshold in absolute RPM.

Allowed range is 0 to the vehicle RPM limits used by validation. In practice it must be `0` or between `idle_rpm` and `rev_limiter`.

This overrides the fraction-style threshold when present.

### `auto_downshift_rpm_fraction`

Automatic downshift threshold as a fraction of the RPM span from idle to rev limiter.

Allowed range is 0.05 to 0.95.

Higher values make the transmission more eager to downshift. Lower values make it hold higher gears longer.

### `auto_downshift_rpm`

Automatic downshift threshold in absolute RPM.

Allowed range is 0 or a valid RPM between `idle_rpm` and `rev_limiter`.

This overrides the fraction-style downshift threshold when present.

### `top_speed_pursuit_speed_fraction`

Threshold for when the automatic logic considers the vehicle near the top-speed region, expressed as a fraction of game `max_speed` reference speed.

Allowed range is 0.50 to 1.20.

Values below `1.0` let the policy begin top-speed behavior before the vehicle reaches its `max_speed` reference target. This is useful for stable high-gear behavior near terminal speed.

### `upshift_hysteresis`

Extra hysteresis for automatic upshift decisions.

Allowed range is 0.0 to 2.0.

Higher values reduce gear hunting but can delay good shifts. Lower values make shifting more responsive but can increase oscillation.

### `min_upshift_net_accel_mps2`

Minimum acceptable net acceleration in the next gear before an upshift is allowed, unless top-speed pursuit logic decides otherwise.

Allowed range is -20.0 to 20.0.

This is an important anti-stall protection for high gears. It helps prevent automatic mode from shifting into a gear that would immediately decelerate or feel dead.

For beginners, the exact unit (`m/s²`) is less important than the effect: it is a \"do not upshift if the next gear is too weak\" rule. If automatic mode enters high gears too early and the vehicle falls flat, this parameter is one of the most useful policy controls to review.

### `prefer_intended_top_speed_gear_near_limit`

Boolean policy flag for preferring the intended top-speed gear near the speed limit region.

This key has no numeric range because it is boolean. Use `true` or `false`.

When enabled, automatic mode tries to stay in or below the intended top-speed gear until it is appropriate to use overdrives.

<a id="sec-5-class-baseline-presets"></a>
## 5. Class Baseline Presets (Steering, Tire Model, Dynamics)

This section provides practical starting presets by class for the modern handling model. These are not hard rules. They are baseline values that should produce a stable first drive and then be refined for each specific vehicle.

The values below intentionally use only `[steering]`, `[tire_model]`, and `[dynamics]` fields.

Recommended classes in this guide:

- Sports Car: fast and responsive, but with controlled high-speed steering authority.
- Sedan/Hatchback: balanced daily-driver behavior.
- SUV/Truck/Van: heavier and calmer directional behavior.
- Motorcycle: agile but less forgiving, especially at speed.

### Steering Baselines (`[steering]`)

| Class | steering_response | high_speed_stability | high_speed_steer_gain | high_speed_steer_start_kph | high_speed_steer_full_kph | wheelbase | max_steer_deg |
|---|---:|---:|---:|---:|---:|---:|---:|
| Sports Car | 1.08 to 1.30 | 0.22 to 0.30 | 0.92 to 0.98 | 145 to 165 | 235 to 260 | 2.50 to 2.90 | 30 to 35 |
| Sedan/Hatchback | 0.98 to 1.20 | 0.25 to 0.34 | 0.88 to 0.95 | 125 to 150 | 210 to 240 | 2.55 to 2.95 | 30 to 36 |
| SUV/Truck/Van | 0.84 to 1.08 | 0.34 to 0.50 | 0.84 to 0.91 | 85 to 120 | 150 to 200 | 2.80 to 3.50 | 28 to 36 |
| Motorcycle | 0.92 to 1.08 | 0.42 to 0.50 | 0.88 to 0.93 | 140 to 160 | 225 to 245 | 1.35 to 1.55 | 34 to 42 |

### Tire Model Baselines (`[tire_model]`)

| Class | tire_grip | lateral_grip | combined_grip_penalty | slip_angle_peak_deg | slip_angle_falloff | turn_response | mass_sensitivity | downforce_grip_gain |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Sports Car | 0.95 to 1.15 | 0.98 to 1.12 | 0.64 to 0.72 | 7.5 to 8.8 | 1.12 to 1.28 | 0.95 to 1.15 | 0.60 to 0.75 | 0.06 to 0.12 |
| Sedan/Hatchback | 0.88 to 1.02 | 0.92 to 1.02 | 0.66 to 0.74 | 8.3 to 9.6 | 1.22 to 1.40 | 0.90 to 1.08 | 0.68 to 0.82 | 0.03 to 0.08 |
| SUV/Truck/Van | 0.75 to 0.92 | 0.78 to 0.92 | 0.50 to 0.65 | 9.5 to 11.5 | 1.40 to 1.70 | 0.80 to 0.98 | 0.80 to 0.95 | 0.00 to 0.05 |
| Motorcycle | 1.04 to 1.20 | 0.76 to 0.90 | 0.74 to 0.84 | 6.0 to 7.2 | 0.90 to 1.08 | 1.05 to 1.28 | 0.40 to 0.62 | 0.05 to 0.11 |

### Dynamics Baselines (`[dynamics]`)

| Class | corner_stiffness_front | corner_stiffness_rear | yaw_inertia_scale | steering_curve | transient_damping |
|---|---:|---:|---:|---:|---:|
| Sports Car | 1.08 to 1.25 | 1.00 to 1.15 | 0.90 to 1.08 | 0.90 to 1.00 | 0.85 to 1.15 |
| Sedan/Hatchback | 0.94 to 1.08 | 0.90 to 1.02 | 1.05 to 1.25 | 1.02 to 1.18 | 1.30 to 1.90 |
| SUV/Truck/Van | 0.72 to 0.92 | 0.65 to 0.85 | 1.35 to 1.70 | 1.12 to 1.30 | 2.00 to 2.90 |
| Motorcycle | 1.30 to 1.55 | 0.90 to 1.05 | 0.52 to 0.70 | 0.72 to 0.88 | 0.80 to 1.05 |

### How to Apply Baselines Safely

1. Pick one class baseline and apply all three groups (`[steering]`, `[tire_model]`, `[dynamics]`) together.
2. Run low-speed checks first (30 to 80 km/h) to verify direction, turn-in, and release behavior.
3. Run high-speed checks (160 to 240 km/h) and adjust `high_speed_steer_gain`, `high_speed_stability`, and `downforce_grip_gain` first.
4. If response is too weak, increase `steering_response` or `turn_response` slightly before raising grip.
5. If response is too aggressive, raise `transient_damping` or `steering_curve` slightly before reducing grip.

### Quick Class Correction Guide

- Sports Car corners too hard at high speed: lower `high_speed_steer_gain`, reduce `downforce_grip_gain`, or slightly raise `high_speed_stability`.
- Sedan feels dead in normal turns: raise `steering_response` by a small amount, then raise `turn_response` only if needed.
- SUV rotates too quickly: raise `yaw_inertia_scale` and `transient_damping`, then reduce `corner_stiffness_rear`.
- Motorcycle feels too safe and planted: reduce `high_speed_stability` slightly and lower `yaw_inertia_scale` toward class baseline.

<a id="sec-6-what-was-removed-from-the-old-format"></a>
## 6. What Was Removed From the Old Format

The new custom format removes several legacy behaviors on purpose.

`mono_crash` sound support is removed and is not a valid parameter. `steering_factor` is also removed and is not supported. If either appears in a custom `.tsv` file, the parser will reject the file as unknown-key input.

The old divide-by-100 convention is also removed. Do not write encoded values such as `17000` for 170 km/h or `180` for `steering_response=1.8`. Use direct values everywhere.

Top-level parameters are no longer supported. Every key must be inside a valid section.

<a id="sec-7-tuning-advice-and-common-problems"></a>
## 7. Tuning Advice and Common Problems

If a vehicle is too fast in every gear, lowering only `max_speed` will not fix the real problem. The vehicle will still accelerate too hard and just reach the top-speed region earlier. In that case, reduce `power_factor`, reduce torque values, increase mass, adjust gearing, or increase drag depending on which part of the speed range is too strong.

If a vehicle feels fine at launch but weak after an upshift, inspect where the new gear lands on the torque curve. Lowering `peak_torque_rpm`, increasing `idle_torque`, shortening gearing, increasing final drive, or reducing drag can all improve shift recovery. Policy can help avoid bad automatic shifts, but it cannot make a weak gear physically stronger.

If a vehicle turns too well for its class, reduce `high_speed_steer_gain`, `max_steer_deg`, or `steering_response` first. If it is still too strong in corners, reduce `lateral_grip`, reduce corner stiffness values, or increase `high_speed_stability`. If it becomes hard to control under braking or acceleration, revisit `tire_grip`, `combined_grip_penalty`, and braking values.

If automatic mode feels worse than manual mode, the physics may already be good. In that case, tune `[policy]` instead of rebuilding the engine and gears. Use `top_speed_gear`, `upshift_delay_*`, and acceleration-protection policy values to stabilize behavior.

<a id="sec-8-final-notes-for-authors"></a>
## 8. Final Notes for Authors

The new `.tsv` format is strict because strictness improves quality, debugging, and fairness. It prevents accidental bad values, catches mistakes early, and makes custom vehicles much easier to maintain.

The best results come from clear goals and methodical tuning. Decide the vehicle role first, make sure the powertrain can physically pull the intended gears, then refine balance with `power_factor`, drag, handling limits, and automatic transmission policy. Test in manual and automatic modes, and pay close attention to what happens immediately after each shift. That moment usually reveals whether your tune is healthy.
