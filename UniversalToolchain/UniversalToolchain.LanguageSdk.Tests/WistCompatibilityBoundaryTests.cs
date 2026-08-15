using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistCompatibilityBoundaryTests
{
    [Test]
    public void LegacyCrossPhaseAdapter_IsExplicitAndExcludesCanonicalAddPilot()
    {
        var contributions = new WistLanguageFeaturePackage().Descriptor.Contributions
            .Where(static contribution => contribution.Slot == LanguageSlots.FrontendSyntax)
            .ToDictionary(static contribution => contribution.Id);

        Assert.Multiple(() =>
        {
            Assert.That(contributions[WistContributionIds.ArithmeticModule].Metadata["wist.compatibility"], Is.EqualTo("none"));
            Assert.That(contributions[WistContributionIds.TextualAdditionModule].Metadata["wist.compatibility"], Is.EqualTo("none"));
        });

        foreach (var contribution in contributions.Values.Where(contribution =>
                     contribution.Id != WistContributionIds.ArithmeticModule &&
                     contribution.Id != WistContributionIds.TextualAdditionModule))
        {
            Assert.That(
                contribution.Metadata.TryGetValue("wist.compatibility", out var compatibility) &&
                compatibility == "legacy-cross-phase-lowering-adapter",
                Is.True,
                $"{contribution.Id.Value} must remain explicitly marked until migrated to semantic/lowering ownership.");
        }
    }

    [Test]
    public void HiddenProgramStructureFrontendModule_IsRemoved()
    {
        var assembly = typeof(WistLanguageFeaturePackage).Assembly;
        Assert.That(
            assembly.GetType("UniversalToolchain.Wist.LanguagePack.WistProgramStructureFrontendModule", throwOnError: false),
            Is.Null);
    }
}
