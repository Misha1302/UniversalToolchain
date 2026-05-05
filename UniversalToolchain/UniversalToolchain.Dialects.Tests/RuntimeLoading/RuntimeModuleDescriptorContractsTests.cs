using BasicCore.Contracts;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class RuntimeModuleDescriptorContractsTests
{
    [Test]
    public void Ctor_RecognizesFrontendAndIrModuleFlags()
    {
        var frontendDescriptor = new RuntimeModuleDescriptor("frontend.sample", typeof(TestFrontendModule));
        var irDescriptor = new RuntimeModuleDescriptor("ir.sample", typeof(TestIrModule));

        Assert.Multiple(() =>
        {
            Assert.That(frontendDescriptor.IsFrontendModule, Is.True);
            Assert.That(frontendDescriptor.IsIrProcessingModule, Is.False);
            Assert.That(irDescriptor.IsFrontendModule, Is.False);
            Assert.That(irDescriptor.IsIrProcessingModule, Is.True);
        });
    }

    [Test]
    public void Ctor_InvalidImplementationType_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => { _ = new RuntimeModuleDescriptor("invalid.sample", typeof(InvalidModuleType)); });
    }

    [Test]
    public void Ctor_Aliases_AreSortedDeduplicatedAndRejectBlankValues()
    {
        var descriptor = new RuntimeModuleDescriptor(
            "module.sample",
            typeof(TestFrontendModule),
            ["beta", "alpha", "beta", "module.sample"]);

        Assert.That(descriptor.Aliases, Is.EqualTo(new[] { "alpha", "beta" }));

        Assert.Throws<ArgumentException>(() => { _ = new RuntimeModuleDescriptor("module.sample", typeof(TestFrontendModule), [""]); });
    }

    [Test]
    public void Ctor_BlankCanonicalId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => { _ = new RuntimeModuleDescriptor(" ", typeof(TestFrontendModule)); });
    }

    [Test]
    public void MetadataOwnerType_MatchesImplementationType()
    {
        var descriptor = new RuntimeModuleDescriptor("module.sample", typeof(TestFrontendModule));

        Assert.That(descriptor.MetadataOwnerType, Is.SameAs(descriptor.ImplementationType));
    }

    private sealed class TestFrontendModule : IFrontendCoreModule;

    private sealed class TestIrModule : IIRProcessingModule;

    private sealed class InvalidModuleType;
}