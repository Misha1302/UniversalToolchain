using BasicCore.Contracts;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class RuntimeOptimizerDescriptorContractsTests
{
    [Test]
    public void Ctor_ValidOptimizerType_IsAccepted()
    {
        var descriptor = new RuntimeOptimizerDescriptor("optimizer.sample", typeof(TestOptimizerModule));

        Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(TestOptimizerModule)));
    }

    [Test]
    public void Ctor_InvalidImplementationType_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => { _ = new RuntimeOptimizerDescriptor("optimizer.sample", typeof(InvalidOptimizerType)); });
    }

    [Test]
    public void Ctor_Aliases_AreSortedDeduplicatedAndRejectBlankValues()
    {
        var descriptor = new RuntimeOptimizerDescriptor(
            "optimizer.sample",
            typeof(TestOptimizerModule),
            ["beta", "alpha", "beta", "optimizer.sample"]);

        Assert.That(descriptor.Aliases, Is.EqualTo(new[] { "alpha", "beta" }));

        Assert.Throws<ArgumentException>(() => { _ = new RuntimeOptimizerDescriptor("optimizer.sample", typeof(TestOptimizerModule), ["  "]); });
    }

    [Test]
    public void Ctor_BlankCanonicalId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => { _ = new RuntimeOptimizerDescriptor(" ", typeof(TestOptimizerModule)); });
    }

    private sealed class TestOptimizerModule : IIRProcessingModule;

    private sealed class InvalidOptimizerType;
}