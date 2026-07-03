using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class CoreContractDescriptorTests
{
    [Test]
    public void ModuleContractTableBuilder_DoesNotInjectCoreFacetsImplicitly()
    {
        var table = new ModuleContractTableBuilder().Build();

        Assert.That(table.Facets, Is.Empty);
    }

    [Test]
    public void KnownCoreContractDescriptorProvider_ExposesCoreFactsAndCapabilitiesExplicitly()
    {
        var facets = new KnownCoreContractDescriptorProvider().GetFacets();

        Assert.Multiple(() =>
        {
            Assert.That(
                facets.OfType<ICompilerFactOwnershipFacet>().Single().Facts.Select(static fact => fact.FactId),
                Does.Contain(KnownCoreCompilerFacts.AirVerified));
            Assert.That(
                facets.OfType<IBackendCapabilityFacet>().Single().Capabilities.Select(static capability => capability.CapabilityId),
                Does.Contain(KnownCoreBackendCapabilities.LocalVariables));
        });
    }
}
