using UniversalToolchain.Ir.Abstractions;

namespace Tests.Ir;

[TestFixture]
public sealed class IrRoutingAbstractionsTests
{
    [Test]
    public void IrKind_WhenValueIsEmpty_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => _ = new IrKind(" "));

        Assert.That(exception!.Message, Does.Contain("IR kind identifier must not be empty."));
    }

    [Test]
    public void IrArtifactExtensions_As_WhenArtifactTypeDoesNotMatch_ThrowsActionableException()
    {
        IIrArtifact artifact = new FakeArtifact(new IrKind("fake"));

        var exception = Assert.Throws<InvalidOperationException>(() => artifact.As<OtherArtifact>());

        Assert.That(exception!.Message, Does.Contain("fake"));
        Assert.That(exception.Message, Does.Contain(nameof(OtherArtifact)));
    }

    [Test]
    public void CapabilitySet_ShouldExposeDeterministicDistinctCapabilities()
    {
        var alpha = new CapabilityId("alpha");
        var beta = new CapabilityId("beta");
        var set = new CapabilitySet([beta, alpha, beta]);

        Assert.That(set.Values, Is.EqualTo(new[] { alpha, beta }));
        Assert.That(set.Supports(alpha), Is.True);
        Assert.That(set.Supports(new CapabilityId("missing")), Is.False);
    }

    [Test]
    public void IntermediateLayerRequest_ShouldStoreGenericLayerPolicy()
    {
        var request = new IntermediateLayerRequest(new IrKind("ssa"), IntermediateLayerPolicy.Prefer);

        Assert.That(request.IrKind, Is.EqualTo(new IrKind("ssa")));
        Assert.That(request.Policy, Is.EqualTo(IntermediateLayerPolicy.Prefer));
    }

    [Test]
    public void IrStageContract_ShouldSnapshotFactsAndCapabilities()
    {
        var requiredFact = new FactId("dominance");
        var requiredCapability = new CapabilityId("pure-effects");
        var contract = new IrStageContract(
            requiresFacts: [requiredFact],
            requiresCapabilities: [requiredCapability]);

        Assert.That(contract.RequiresFacts.Contains(requiredFact), Is.True);
        Assert.That(contract.RequiresCapabilities.Supports(requiredCapability), Is.True);
        Assert.That(contract.ProducesFacts.Values, Is.Empty);
    }

    private sealed class FakeArtifact(IrKind kind) : IIrArtifact
    {
        public IrKind Kind { get; } = kind;
    }

    private sealed class OtherArtifact : IIrArtifact
    {
        public IrKind Kind { get; } = new("other");
    }
}
