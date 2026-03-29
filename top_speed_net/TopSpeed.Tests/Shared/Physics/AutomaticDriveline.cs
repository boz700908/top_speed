using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests.Physics
{
    [Trait("Category", "SharedPhysics")]
    public sealed class AutomaticDrivelineTests
    {
        [Fact]
        public void Step_AtcIdle_ProducesCreepAcceleration()
        {
            var output = AutomaticDrivelineModel.Step(
                TransmissionType.Atc,
                AutomaticDrivelineTuning.Default,
                new AutomaticDrivelineInput(
                    elapsedSeconds: 0.016f,
                    speedMps: 0f,
                    throttle: 0f,
                    brake: 0f,
                    shifting: false,
                    wheelCircumferenceM: 2.0f,
                    finalDriveRatio: 3.5f,
                    idleRpm: 700f,
                    revLimiter: 6000f),
                new AutomaticDrivelineState(couplingFactor: 1f, cvtRatio: 0f));

            Assert.True(output.CreepAccelerationMps2 > 0f);
            Assert.True(output.CouplingFactor >= 0f && output.CouplingFactor <= 1f);
        }

        [Fact]
        public void Step_AtcBelowLockSpeed_DoesNotHardLockCoupling()
        {
            var output = AutomaticDrivelineModel.Step(
                TransmissionType.Atc,
                AutomaticDrivelineTuning.Default,
                new AutomaticDrivelineInput(
                    elapsedSeconds: 1.0f,
                    speedMps: 20f / 3.6f,
                    throttle: 1.0f,
                    brake: 0f,
                    shifting: false,
                    wheelCircumferenceM: 2.0f,
                    finalDriveRatio: 3.5f,
                    idleRpm: 700f,
                    revLimiter: 6000f),
                new AutomaticDrivelineState(couplingFactor: 0.50f, cvtRatio: 0f));

            Assert.True(output.CouplingFactor < 0.98f);
            Assert.True(output.CouplingFactor > 0.90f);
        }

        [Fact]
        public void Step_AtcAtLockSpeed_AllowsFullCoupling()
        {
            var output = AutomaticDrivelineModel.Step(
                TransmissionType.Atc,
                AutomaticDrivelineTuning.Default,
                new AutomaticDrivelineInput(
                    elapsedSeconds: 1.0f,
                    speedMps: 40f / 3.6f,
                    throttle: 1.0f,
                    brake: 0f,
                    shifting: false,
                    wheelCircumferenceM: 2.0f,
                    finalDriveRatio: 3.5f,
                    idleRpm: 700f,
                    revLimiter: 6000f),
                new AutomaticDrivelineState(couplingFactor: 0.50f, cvtRatio: 0f));

            Assert.Equal(1f, output.CouplingFactor, 3);
        }

        [Fact]
        public void Step_AtcLaunchTransition_AvoidsAbruptCouplingJump()
        {
            var tuning = AutomaticDrivelineTuning.Default;

            var launchSpeedOutput = AutomaticDrivelineModel.Step(
                TransmissionType.Atc,
                tuning,
                new AutomaticDrivelineInput(
                    elapsedSeconds: 1.0f,
                    speedMps: 2.4f / 3.6f,
                    throttle: 1.0f,
                    brake: 0f,
                    shifting: false,
                    wheelCircumferenceM: 2.0f,
                    finalDriveRatio: 3.5f,
                    idleRpm: 700f,
                    revLimiter: 6000f),
                new AutomaticDrivelineState(couplingFactor: 0f, cvtRatio: 0f));

            var justAboveLaunchOutput = AutomaticDrivelineModel.Step(
                TransmissionType.Atc,
                tuning,
                new AutomaticDrivelineInput(
                    elapsedSeconds: 1.0f,
                    speedMps: 2.6f / 3.6f,
                    throttle: 1.0f,
                    brake: 0f,
                    shifting: false,
                    wheelCircumferenceM: 2.0f,
                    finalDriveRatio: 3.5f,
                    idleRpm: 700f,
                    revLimiter: 6000f),
                new AutomaticDrivelineState(couplingFactor: 0f, cvtRatio: 0f));

            Assert.True(justAboveLaunchOutput.CouplingFactor >= launchSpeedOutput.CouplingFactor);
            Assert.True(justAboveLaunchOutput.CouplingFactor - launchSpeedOutput.CouplingFactor < 0.05f);
        }

        [Fact]
        public void Step_AtcLaunchFeedback_LowersCouplingWhenRpmBelowLaunchTarget()
        {
            var tuning = AutomaticDrivelineTuning.Default;
            var output = AutomaticDrivelineModel.Step(
                TransmissionType.Atc,
                tuning,
                new AutomaticDrivelineInput(
                    elapsedSeconds: 1.0f,
                    speedMps: 4f / 3.6f,
                    throttle: 1.0f,
                    brake: 0f,
                    shifting: false,
                    wheelCircumferenceM: 2.0f,
                    finalDriveRatio: 3.5f,
                    idleRpm: 700f,
                    revLimiter: 6000f,
                    launchRpm: 2000f,
                    currentEngineRpm: 900f),
                new AutomaticDrivelineState(couplingFactor: 1f, cvtRatio: 0f));

            Assert.True(output.CouplingFactor < tuning.Atc.LaunchCouplingMax);
        }

        [Fact]
        public void Step_AtcLaunchFeedback_RaisesCouplingWhenRpmAboveLaunchTarget()
        {
            var tuning = AutomaticDrivelineTuning.Default;
            var output = AutomaticDrivelineModel.Step(
                TransmissionType.Atc,
                tuning,
                new AutomaticDrivelineInput(
                    elapsedSeconds: 1.0f,
                    speedMps: 4f / 3.6f,
                    throttle: 1.0f,
                    brake: 0f,
                    shifting: false,
                    wheelCircumferenceM: 2.0f,
                    finalDriveRatio: 3.5f,
                    idleRpm: 700f,
                    revLimiter: 6000f,
                    launchRpm: 2000f,
                    currentEngineRpm: 2800f),
                new AutomaticDrivelineState(couplingFactor: 0f, cvtRatio: 0f));

            Assert.True(output.CouplingFactor > tuning.Atc.LaunchCouplingMax);
        }

        [Fact]
        public void Step_DctShift_DropsCouplingWithoutCreep()
        {
            var output = AutomaticDrivelineModel.Step(
                TransmissionType.Dct,
                AutomaticDrivelineTuning.Default,
                new AutomaticDrivelineInput(
                    elapsedSeconds: 0.016f,
                    speedMps: 20f,
                    throttle: 0.7f,
                    brake: 0f,
                    shifting: true,
                    wheelCircumferenceM: 2.0f,
                    finalDriveRatio: 3.5f,
                    idleRpm: 900f,
                    revLimiter: 8000f),
                new AutomaticDrivelineState(couplingFactor: 1f, cvtRatio: 0f));

            Assert.True(output.CouplingFactor < 1f);
            Assert.Equal(0f, output.CreepAccelerationMps2);
            Assert.Equal(0f, output.EffectiveDriveRatio);
        }

        [Fact]
        public void Step_DctStandstillClosedThrottle_ReleasesCoupling()
        {
            var output = AutomaticDrivelineModel.Step(
                TransmissionType.Dct,
                AutomaticDrivelineTuning.Default,
                new AutomaticDrivelineInput(
                    elapsedSeconds: 1.0f,
                    speedMps: 0f,
                    throttle: 0f,
                    brake: 0f,
                    shifting: false,
                    wheelCircumferenceM: 2.0f,
                    finalDriveRatio: 3.5f,
                    idleRpm: 900f,
                    revLimiter: 8000f),
                new AutomaticDrivelineState(couplingFactor: 1f, cvtRatio: 0f));

            Assert.True(output.CouplingFactor <= 0.01f);
        }

        [Fact]
        public void Step_DctLowThrottleStandstill_KeepsLowCoupling()
        {
            var output = AutomaticDrivelineModel.Step(
                TransmissionType.Dct,
                AutomaticDrivelineTuning.Default,
                new AutomaticDrivelineInput(
                    elapsedSeconds: 1.0f,
                    speedMps: 0f,
                    throttle: 0.10f,
                    brake: 0f,
                    shifting: false,
                    wheelCircumferenceM: 2.0f,
                    finalDriveRatio: 3.5f,
                    idleRpm: 900f,
                    revLimiter: 8000f,
                    launchRpm: 2400f,
                    currentEngineRpm: 850f),
                new AutomaticDrivelineState(couplingFactor: 0f, cvtRatio: 0f));

            Assert.True(output.CouplingFactor < 0.15f);
        }

        [Fact]
        public void Step_DctLaunchFeedback_AdjustsCouplingByRpmError()
        {
            var tuning = AutomaticDrivelineTuning.Default;
            var lowRpmOutput = AutomaticDrivelineModel.Step(
                TransmissionType.Dct,
                tuning,
                new AutomaticDrivelineInput(
                    elapsedSeconds: 1.0f,
                    speedMps: 4f / 3.6f,
                    throttle: 1.0f,
                    brake: 0f,
                    shifting: false,
                    wheelCircumferenceM: 2.0f,
                    finalDriveRatio: 3.5f,
                    idleRpm: 900f,
                    revLimiter: 8000f,
                    launchRpm: 2400f,
                    currentEngineRpm: 1200f),
                new AutomaticDrivelineState(couplingFactor: 0f, cvtRatio: 0f));

            var highRpmOutput = AutomaticDrivelineModel.Step(
                TransmissionType.Dct,
                tuning,
                new AutomaticDrivelineInput(
                    elapsedSeconds: 1.0f,
                    speedMps: 4f / 3.6f,
                    throttle: 1.0f,
                    brake: 0f,
                    shifting: false,
                    wheelCircumferenceM: 2.0f,
                    finalDriveRatio: 3.5f,
                    idleRpm: 900f,
                    revLimiter: 8000f,
                    launchRpm: 2400f,
                    currentEngineRpm: 3400f),
                new AutomaticDrivelineState(couplingFactor: 0f, cvtRatio: 0f));

            Assert.True(highRpmOutput.CouplingFactor > lowRpmOutput.CouplingFactor);
        }

        [Fact]
        public void Step_Cvt_AdjustsRatioWithinConfiguredBounds()
        {
            var tuning = AutomaticDrivelineTuning.Default;
            var state = new AutomaticDrivelineState(couplingFactor: 0.6f, cvtRatio: tuning.Cvt.RatioMax);
            AutomaticDrivelineOutput output = default;
            for (var i = 0; i < 12; i++)
            {
                output = AutomaticDrivelineModel.Step(
                    TransmissionType.Cvt,
                    tuning,
                    new AutomaticDrivelineInput(
                        elapsedSeconds: 0.02f,
                        speedMps: 18f,
                        throttle: 0.65f,
                        brake: 0f,
                        shifting: false,
                        wheelCircumferenceM: 2.0f,
                        finalDriveRatio: 3.2f,
                        idleRpm: 700f,
                        revLimiter: 5800f),
                    state);
                state = new AutomaticDrivelineState(output.CouplingFactor, output.CvtRatio);
            }

            Assert.True(output.EffectiveDriveRatio >= tuning.Cvt.RatioMin);
            Assert.True(output.EffectiveDriveRatio <= tuning.Cvt.RatioMax);
            Assert.True(output.CouplingFactor > 0f);
        }
    }
}


