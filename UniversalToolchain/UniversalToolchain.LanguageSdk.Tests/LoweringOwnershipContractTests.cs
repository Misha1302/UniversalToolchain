using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

/// <summary>
/// Contract for the lowering-ownership migration (UT-A046).
/// <para>
/// The refactor moved "which modules lower, and who owns which phase" out of a thirteen-term
/// <c>||</c> chain and into one declarative list. The properties worth pinning are the ones a later
/// edit could quietly break: that the list is the only source, that nothing derives a second
/// divergent answer, and that the dispatch still fails closed for a contribution nobody owns.
/// </para>
/// </summary>
[TestFixture]
public sealed class LoweringOwnershipContractTests
{
    [Test]
    public void LoweringOwners_AreCanonicalDistinctAndNonEmpty()
    {
        var contributions = WistLoweringOwners.Contributions;

        Assert.Multiple(() =>
        {
            Assert.That(contributions, Is.Not.Empty);
            Assert.That(contributions.Distinct().Count(), Is.EqualTo(contributions.Count),
                "a duplicated owner would make the registry reject its own batch");
            Assert.That(
                contributions.Select(static id => id.Value),
                Is.Ordered.Using<string>(StringComparer.Ordinal),
                "the list is data and must read canonically, so a diff shows additions in place");
        });
    }

    [Test]
    public void LoweringOwners_AllExistInTheComponentCatalog()
    {
        // A typo in the list must not silently disable a module's lowering: it must name a contribution
        // the catalog actually knows about.
        var catalog = WistRuntimeComponentCatalog.ContributionsForTest.ToHashSet();

        Assert.That(
            WistLoweringOwners.Contributions.Where(contribution => !catalog.Contains(contribution)),
            Is.Empty,
            "every lowering owner must be a contribution the component catalog declares");
    }

    [Test]
    public void ModuleRegistry_OwnsLoweringExactlyForTheDeclaredOwners()
    {
        var registry = WistLoweringOwners.ModuleRegistry;
        var mismatches = new List<string>();
        var catalog = WistRuntimeComponentCatalog.ContributionsForTest;

        foreach (var contribution in catalog)
        {
            var declared = WistLoweringOwners.OwnsLowering(contribution);
            var registered = registry.TryGetOwner(LanguageModulePhase.Lowering, contribution, out _);
            if (declared != registered)
                mismatches.Add($"{contribution.Value}: declared={declared} registry={registered}");
            if (declared != WistSemanticBytecodeLowerer.SupportsModuleContribution(contribution))
                mismatches.Add($"{contribution.Value}: declared={declared} predicate={!declared}");
        }

        Assert.That(mismatches, Is.Empty,
            "the declarative list, the ownership registry and the lowering predicate must be one answer; " +
            "divergence is how a dispatch bug becomes a runtime failure: " +
            string.Join("; ", mismatches));
    }

    [Test]
    public void LoweringOwners_ExcludeModulesThatDoNotLower()
    {
        // Comments and other frontend-only modules are in the catalog but have no lowering
        // implementation. If one were added to the list, the lowerer would claim to support a node it
        // cannot emit.
        var frontendOnly = WistRuntimeComponentCatalog.Modules
            .Where(static component => component.SemanticBindingRulesFactory == null)
            .Select(static component => component.ContributionId)
            .Where(static contribution => !WistLoweringOwners.OwnsLowering(contribution))
            .ToArray();

        Assert.That(frontendOnly, Is.Not.Empty,
            "the corpus must contain at least one frontend-only module, otherwise this test proves nothing");

        var wronglyOwned = frontendOnly
            .Where(static contribution => WistLoweringOwners.ModuleRegistry
                .TryGetOwner(LanguageModulePhase.Lowering, contribution, out _))
            .ToArray();

        Assert.That(wronglyOwned, Is.Empty,
            "a frontend-only module must not own a lowering phase: " +
            string.Join(", ", wronglyOwned.Select(static id => id.Value)));
    }

    [Test]
    public void OwnerLookup_IsIndependentOfCatalogIterationOrder()
    {
        // The registry is a Lazy singleton; building a second one with the descriptors supplied in a
        // different order must produce the same answers, otherwise ownership depends on load order.
        var forward = new LanguageModuleRegistry().AddRange(WistModuleDescriptors.Create());
        var reversed = new LanguageModuleRegistry().AddRange(WistModuleDescriptors.Create().Reverse());

        var mismatches = WistLoweringOwners.Contributions
            .Where(contribution =>
            {
                forward.TryGetOwner(LanguageModulePhase.Lowering, contribution, out var left);
                reversed.TryGetOwner(LanguageModulePhase.Lowering, contribution, out var right);
                return !string.Equals(left?.Id.Value, right?.Id.Value, StringComparison.Ordinal);
            })
            .ToArray();

        Assert.That(mismatches, Is.Empty,
            "ownership must not depend on registration order: " +
            string.Join(", ", mismatches.Select(static id => id.Value)));
    }

    [Test]
    public void LoweringDispatch_FailsClosedForAContributionNoModuleOwns()
    {
        // `wist.module.comments` exists in the catalog but has no lowering implementation, which makes
        // it the honest probe for the fail-closed path.
        var unowned = WistContributionIds.CommentsModule;

        Assert.Multiple(() =>
        {
            Assert.That(WistLoweringOwners.OwnsLowering(unowned), Is.False);
            Assert.That(
                WistLoweringOwners.TryGetLoweringOwner(unowned, out var descriptor),
                Is.False);
            Assert.That(descriptor, Is.Null);
        });
    }

    [Test]
    public void LoweringDispatch_ResolvesTheOwningModuleForEverySupportedContribution()
    {
        var unresolved = WistLoweringOwners.Contributions
            .Where(static contribution => !WistLoweringOwners.TryGetLoweringOwner(contribution, out _))
            .ToArray();

        Assert.That(unresolved, Is.Empty,
            "a contribution declared as lowered must resolve to its owning module: " +
            string.Join(", ", unresolved.Select(static id => id.Value)));
    }
}
