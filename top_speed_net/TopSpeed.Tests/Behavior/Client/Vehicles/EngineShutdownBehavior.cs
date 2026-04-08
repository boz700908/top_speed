using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class EngineShutdownBehaviorTests
{
    [Fact]
    public void CombustionOff_DisengagedFromDriveline_ShouldNotBeReseededFromWheelSpeed()
    {
        var engine = EngineHarness.BuildEngine();
        engine.StopEngine();

        engine.SyncFromSpeed(
            speedGameUnits: 90f,
            gear: 3,
            elapsed: 0.05f,
            throttleInput: 0,
            inReverse: false,
            couplingMode: EngineCouplingMode.Disengaged,
            couplingFactor: 0f,
            minimumCoupledRpm: 0f,
            combustionEnabled: false);

        engine.Rpm.Should().BeApproximately(0f, 0.0001f);
    }

    [Fact]
    public void CombustionOff_LockedToDriveline_ShouldStillBeBackDrivenFromWheelSpeed()
    {
        var engine = EngineHarness.BuildEngine();
        engine.StopEngine();

        engine.SyncFromSpeed(
            speedGameUnits: 90f,
            gear: 3,
            elapsed: 0.05f,
            throttleInput: 0,
            inReverse: false,
            couplingMode: EngineCouplingMode.Locked,
            couplingFactor: 1f,
            minimumCoupledRpm: 0f,
            combustionEnabled: false);

        engine.Rpm.Should().BeGreaterThan(engine.StallRpm);
    }
}
