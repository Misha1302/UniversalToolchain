using BasicCore.Contracts;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.ModuleContracts;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistCompatibilityBoundaryTests
{
    [Test]
    public void BuiltInSyntaxModules_NoLongerDeclareCrossPhaseCompatibility()
    {
        var syntaxContributions = new WistLanguageFeaturePackage().Descriptor.Contributions
            .Where(static contribution => contribution.Slot == LanguageSlots.FrontendSyntax)
            .ToArray();

        Assert.That(syntaxContributions, Is.Not.Empty);
        foreach (var contribution in syntaxContributions)
        {
            Assert.Multiple(() =>
            {
                Assert.That(contribution.Metadata["wist.phase"], Is.EqualTo("syntax"));
                Assert.That(contribution.Metadata["wist.compatibility"], Is.EqualTo("none"));
                Assert.That(contribution.Metadata["wist.owner"], Is.EqualTo("language-plan"));
            });
        }
    }

    [Test]
    public void VariablesFeature_DeclaresSyntaxSemanticAndLoweringOwners()
    {
        var package = new WistLanguageFeaturePackage().Descriptor;
        var feature = package.Features.Single(feature => feature.Id == WistFeatureIds.Variables);
        var semantic = WistModulePhaseOwnership.SemanticContributionId(WistContributionIds.VariablesModule);
        var lowering = WistModulePhaseOwnership.LoweringContributionId(WistContributionIds.VariablesModule);

        Assert.Multiple(() =>
        {
            Assert.That(feature.Contributions, Does.Contain(WistContributionIds.VariablesModule));
            Assert.That(feature.Contributions, Does.Contain(semantic));
            Assert.That(feature.Contributions, Does.Contain(lowering));
            Assert.That(package.Contributions.Single(item => item.Id == semantic).Slot, Is.EqualTo(WistModulePhaseSlots.Semantics));
            Assert.That(package.Contributions.Single(item => item.Id == lowering).Slot, Is.EqualTo(WistModulePhaseSlots.Lowering));
        });
    }

    [Test]
    public void SyntaxOnlyTextualAddition_DoesNotAcquireFakeModuleLowerer()
    {
        var package = new WistLanguageFeaturePackage().Descriptor;
        var feature = package.Features.Single(feature => feature.Id == WistFeatureIds.TextualAddition);
        var fakeModuleLowering = WistModulePhaseOwnership.LoweringContributionId(WistContributionIds.TextualAdditionModule);

        Assert.Multiple(() =>
        {
            Assert.That(feature.Contributions, Does.Not.Contain(fakeModuleLowering));
            Assert.That(feature.Contributions, Does.Contain(WistContributionIds.CanonicalAddSemantics));
            Assert.That(feature.Contributions, Does.Contain(WistContributionIds.CanonicalAddLowering));
        });
    }

    [Test]
    public void LegacyCrossPhaseFrontendAdapter_IsRemoved()
    {
        var assembly = typeof(WistLanguageFeaturePackage).Assembly;
        Assert.Multiple(() =>
        {
            Assert.That(
                assembly.GetType("UniversalToolchain.Wist.LanguagePack.WistLegacyFrontendModuleCompatibility", throwOnError: false),
                Is.Null);
            Assert.That(
                assembly.GetType("UniversalToolchain.Wist.LanguagePack.WistProgramStructureFrontendModule", throwOnError: false),
                Is.Null);
            Assert.That(typeof(IAirOptimizer).IsAssignableFrom(typeof(WistOptimizerContractSnapshot)), Is.False);
            Assert.That(typeof(IFrontendCoreModule).IsAssignableFrom(typeof(WistOptimizerContractSnapshot)), Is.False);
            Assert.That(typeof(IModuleContractDescriptorProvider).IsAssignableFrom(typeof(WistOptimizerContractSnapshot)), Is.True);
            Assert.That(
                typeof(WistAirArtifact).GetProperty(nameof(WistAirArtifact.AppliedOptimizerContracts))?.PropertyType,
                Is.EqualTo(typeof(IReadOnlyList<WistOptimizerContractSnapshot>)));
        });
    }
}
