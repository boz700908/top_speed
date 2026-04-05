using System;
using TopSpeed.Input;
using TopSpeed.Input.Backends.Sdl;
using TS.Sdl.Input;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class ControllerDisplayTests
    {
        [Fact]
        public void FormatAxis_UsesXboxLabels_ForXboxGamepadProfile()
        {
            var profile = new ControllerDisplayProfile(ControllerDeviceType.Gamepad, ControllerGamepadFamily.Xbox);

            Assert.Equal("A", InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button1, profile));
            Assert.Equal("View", InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button7, profile));
            Assert.Equal("Left trigger", InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.AxisZPos, profile));
        }

        [Fact]
        public void FormatAxis_UsesPlayStationLabels_ForPlayStationGamepadProfile()
        {
            var profile = new ControllerDisplayProfile(ControllerDeviceType.Gamepad, ControllerGamepadFamily.PlayStation);

            Assert.Equal("Cross", InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button1, profile));
            Assert.Equal("Circle", InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button2, profile));
            Assert.Equal("PS button", InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button11, profile));
        }

        [Fact]
        public void FormatAxis_UsesNintendoLabels_ForNintendoGamepadProfile()
        {
            var profile = new ControllerDisplayProfile(ControllerDeviceType.Gamepad, ControllerGamepadFamily.Nintendo);

            Assert.Equal("B", InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button1, profile));
            Assert.Equal("A", InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button2, profile));
            Assert.Equal("Plus", InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button8, profile));
        }

        [Fact]
        public void FormatAxis_UsesSemanticLabels_ForNeutralGamepadProfile()
        {
            Assert.Equal(
                "South",
                InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button1, ControllerDisplayProfile.SemanticGamepad));
            Assert.Equal(
                "Start",
                InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button8, ControllerDisplayProfile.SemanticGamepad));
        }

        [Fact]
        public void FormatAxis_UsesGenericLabels_ForJoystickProfile()
        {
            Assert.Equal(
                "Button 1",
                InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button1, ControllerDisplayProfile.Joystick));
            Assert.Equal(
                "POV 1 up",
                InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Pov1, ControllerDisplayProfile.Joystick));
        }

        [Fact]
        public void BuildChoiceLabel_IsAlwaysDetailed()
        {
            var metadata = CreateMetadata(
                42,
                isGamepad: false,
                name: "Wheel Pro",
                joystickType: JoystickType.Wheel,
                vendorId: 0x046D,
                productId: 0xC29B);

            var label = Display.BuildChoiceLabel(metadata, isRacingWheel: true);

            Assert.Equal("Wheel Pro (Racing wheel, 046D:C29B)", label);
        }

        [Fact]
        public void CreateProfile_UsesNameHeuristics_ForUnknownGamepadTypes()
        {
            var metadata = CreateMetadata(
                7,
                isGamepad: true,
                name: "DualSense Wireless Controller",
                joystickType: JoystickType.Gamepad,
                gamepadType: GamepadType.Unknown);

            var profile = Display.CreateProfile(metadata, isRacingWheel: false);

            Assert.Equal(ControllerDeviceType.Gamepad, profile.DeviceType);
            Assert.Equal(ControllerGamepadFamily.PlayStation, profile.GamepadFamily);
        }

        private static DeviceMetadata CreateMetadata(
            uint instanceId,
            bool isGamepad,
            string name,
            JoystickType joystickType,
            GamepadType gamepadType = GamepadType.Unknown,
            ushort vendorId = 0,
            ushort productId = 0)
        {
            return new DeviceMetadata(
                instanceId,
                isGamepad,
                name,
                path: string.Empty,
                guid: Guid.Empty,
                joystickType,
                gamepadType,
                playerIndex: -1,
                vendorId,
                productId,
                productVersion: 0,
                firmwareVersion: 0,
                serial: string.Empty);
        }
    }
}
