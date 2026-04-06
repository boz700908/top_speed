using System.Reflection;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class WheelPedalTests
    {
        [Fact]
        public void WheelPedals_AutoInvert_FromRestEndpoint_ForThrottleBrakeAndClutch()
        {
            var settings = new RaceSettings
            {
                DeviceMode = InputDeviceMode.Controller
            };
            var input = new RaceInput(settings);

            input.Run(new InputState(), new State { Z = 100, Rz = 100, Slider1 = 100 }, 0f, controllerIsRacingWheel: true);
            input.Run(new InputState(), new State { Z = -100, Rz = -100, Slider1 = -100 }, 0f, controllerIsRacingWheel: true);

            Assert.Equal(100, input.GetThrottle());
            Assert.Equal(-100, input.GetBrake());
            Assert.Equal(100, input.GetClutch());
        }

        [Fact]
        public void WheelPedals_RefineRestEndpoint_ToUseFullTravel()
        {
            var settings = new RaceSettings
            {
                DeviceMode = InputDeviceMode.Controller
            };
            var input = new RaceInput(settings);

            input.Run(new InputState(), new State { Rz = 60 }, 0f, controllerIsRacingWheel: true);
            input.Run(new InputState(), new State { Rz = 100 }, 0f, controllerIsRacingWheel: true);
            input.Run(new InputState(), new State { Rz = -100 }, 0f, controllerIsRacingWheel: true);
            input.Run(new InputState(), new State { Rz = 0 }, 0f, controllerIsRacingWheel: true);

            Assert.InRange(input.GetThrottle(), 45, 55);
        }

        [Theory]
        [InlineData(69, false)]
        [InlineData(70, true)]
        [InlineData(100, true)]
        public void ManualShift_UsesRelaxedClutchThreshold(int clutch, bool expected)
        {
            var method = typeof(Car).GetMethod("CanShiftManual", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.NotNull(method);
            Assert.Equal(expected, (bool)method!.Invoke(null, new object[] { clutch })!);
        }
    }
}
