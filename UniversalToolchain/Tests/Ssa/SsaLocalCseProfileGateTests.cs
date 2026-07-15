using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaLocalCseProfileGateTests
{
    [Test]
    public void PreviewAirRoundtripProfile_DoesNotEnableLocalCseWithoutSchedulingCapability()
    {
        var passIds = SsaRouteProfiles
            .Create(SsaRoutePolicy.Debug)
            .CreateOptimizationPasses()
            .Select(static pass => pass.Id.Value)
            .ToArray();

        Assert.That(passIds, Does.Not.Contain("ssa.optimization.cse.local"));
    }
}
