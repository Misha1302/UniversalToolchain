using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.ModuleContracts;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistArchitectureRepairRegressionTests
{
    [Test]
    public void OptimizerContractSnapshot_DeepCopiesFacetAndNestedCollections()
    {
        var emittedPatterns = new List<AirPatternId>
        {
            new("test.air.before")
        };
        var emissions = new List<AirEmissionContract>
        {
            new(
                new BytecodePatternId("test.bytecode.source"),
                emittedPatterns,
                Array.Empty<IntrinsicSymbolId>(),
                Array.Empty<BackendCapabilityId>())
        };
        var facet = new MutableAirContractFacet(new ModuleId("test.optimizer"), emissions);
        var owners = new List<ContractNamespaceOwner>
        {
            ContractNamespaceOwner.External("test-owner")
        };
        var provider = new MutableContractProvider(owners, [facet]);

        var snapshot = WistOptimizerContractSnapshot.Capture(
            new LanguageContributionId("test.optimizer.pass"),
            provider);

        emittedPatterns.Clear();
        emissions.Clear();
        owners.Clear();

        var capturedFacet = snapshot.GetFacets().Single();
        Assert.Multiple(() =>
        {
            Assert.That(capturedFacet, Is.Not.SameAs(facet));
            Assert.That(capturedFacet, Is.InstanceOf<IAirContractFacet>());
            Assert.That(snapshot.NamespaceOwners, Has.Count.EqualTo(1));
            Assert.That(snapshot.NamespaceOwners[0], Is.Not.SameAs(ContractNamespaceOwner.ThirdParty));
        });

        var capturedAir = (IAirContractFacet)capturedFacet;
        Assert.Multiple(() =>
        {
            Assert.That(capturedAir.AirEmissions, Has.Count.EqualTo(1));
            Assert.That(capturedAir.AirEmissions[0].MayEmitPatterns, Has.Count.EqualTo(1));
            Assert.That(capturedAir.AirEmissions[0].MayEmitPatterns[0], Is.EqualTo(new AirPatternId("test.air.before")));
        });
    }

    [Test]
    public void ModulePhaseOwnership_IsDerivedFromExecutablePhaseImplementations()
    {
        var backend = new BackendId("interpreter");

        foreach (var component in WistRuntimeComponentCatalog.Modules)
        {
            var expanded = WistModulePhaseOwnership.ExpandFeatureContributions([component.ContributionId]);
            var declared = WistModulePhaseOwnership.CreatePhaseContributions(component, [backend]).ToArray();
            var ownsSemantics = component.SemanticBindingRulesFactory != null;
            var ownsLowering = WistSemanticBytecodeLowerer.SupportsModuleContribution(component.ContributionId);
            var semanticId = WistModulePhaseOwnership.SemanticContributionId(component.ContributionId);
            var loweringId = WistModulePhaseOwnership.LoweringContributionId(component.ContributionId);

            Assert.Multiple(() =>
            {
                Assert.That(expanded, Does.Contain(component.ContributionId));
                Assert.That(expanded.Contains(semanticId), Is.EqualTo(ownsSemantics),
                    $"Semantic expansion drifted for '{component.ContributionId.Value}'.");
                Assert.That(expanded.Contains(loweringId), Is.EqualTo(ownsLowering),
                    $"Lowering expansion drifted for '{component.ContributionId.Value}'.");
                Assert.That(declared.Any(item => item.Id == semanticId), Is.EqualTo(ownsSemantics),
                    $"Semantic declaration drifted for '{component.ContributionId.Value}'.");
                Assert.That(declared.Any(item => item.Id == loweringId), Is.EqualTo(ownsLowering),
                    $"Lowering declaration drifted for '{component.ContributionId.Value}'.");
                Assert.That(
                    WistModulePhaseOwnership.TryGetSemanticComponent(semanticId, out var semanticOwner),
                    Is.EqualTo(ownsSemantics));
                Assert.That(semanticOwner == component, Is.EqualTo(ownsSemantics));
                Assert.That(
                    WistModulePhaseOwnership.TryGetLoweringComponent(loweringId, out var loweringOwner),
                    Is.EqualTo(ownsLowering));
                Assert.That(loweringOwner == component, Is.EqualTo(ownsLowering));
            });
        }
    }

    private sealed class MutableContractProvider(
        IReadOnlyList<ContractNamespaceOwner> namespaceOwners,
        IReadOnlyList<IModuleContractFacet> facets) : IModuleContractDescriptorProvider
    {
        public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners { get; } = namespaceOwners;

        public IReadOnlyList<IModuleContractFacet> GetFacets() => facets;
    }

    private sealed class MutableAirContractFacet(
        ModuleId moduleId,
        IReadOnlyList<AirEmissionContract> emissions) : IAirContractFacet
    {
        public ModuleId ModuleId { get; } = moduleId;
        public ContractFacetKind Kind => ContractFacetKind.Air;
        public ContractSchemaVersion SchemaVersion => ModuleContractSchemaVersions.Current;
        public IReadOnlyList<AirEmissionContract> AirEmissions { get; } = emissions;
    }
}
