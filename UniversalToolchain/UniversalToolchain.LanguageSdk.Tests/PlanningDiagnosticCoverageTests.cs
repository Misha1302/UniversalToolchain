using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

/// <summary>
///     Pins planning diagnostics that had no test at all, so nobody had ever observed them firing.
/// </summary>
/// <remarks>
///     <para>
///         A diagnostic gate can only prove that a code is <em>mentioned</em>. Fourteen <c>UTL</c> planning codes
///         were emitted in production and referenced by no positive assertion, which means their message, severity
///         and trigger condition were all unverified claims. Each test below reaches the branch through
///         <see cref="LanguageCompiler" /> using a registry built for that one condition.
///     </para>
///     <para>
///         Every test asserts the specific code rather than "there are errors", because a fixture that trips a
///         different validation on the way still leaves the intended branch unobserved.
///     </para>
/// </remarks>
[TestFixture]
public sealed class PlanningDiagnosticCoverageTests
{
    private static readonly LanguageSlotId Tooling = new("planning.test.tooling");

    [Test]
    public void FeatureDependencyCycle_ReportsCycleDiagnostic()
    {
        var a = new LanguageFeatureId("planning.cycle.a");
        var b = new LanguageFeatureId("planning.cycle.b");
        var package = Package(
            new LanguageFeatureDescriptor(a, [b], contributions: []),
            new LanguageFeatureDescriptor(b, [a], contributions: []));

        var result = Compile(registry => registry.AddPackage(package), builder => builder.UseFeature(a));

        AssertCode(result, "UTL1003");
    }

    [Test]
    public void FeatureUnsupportedBySelectedBackend_ReportsBackendRejection()
    {
        var feature = new LanguageFeatureId("planning.backend.feature");
        var package = PackageWithContributions(
            new LanguageFeatureDescriptor(
                feature,
                requires: [],
                contributions: [new LanguageContributionId("planning.backend.contribution")],
                supportedBackends: [new BackendId("cil")]),
            [new LanguageContributionDescriptor(new LanguageContributionId("planning.backend.contribution"), Tooling)]);

        var result = Compile(
            registry => registry.AddPackage(package),
            builder => builder.UseFeature(feature).EnableBackend(new BackendId("interpreter")));

        AssertCode(result, "UTL1203");
    }

    [Test]
    public void FeaturePackageWithMismatchedToolchainApi_ReportsApiMismatch()
    {
        var feature = new LanguageFeatureId("planning.api.feature");
        var package = new TestPackage(new LanguagePackageDescriptor(
            new LanguagePackageId("Planning.Api"),
            new LanguageVersion("1"),
            new ToolchainApiVersion(ToolchainApi.Current.Major + 1),
            [new LanguageFeatureDescriptor(feature, requires: [], contributions: [])],
            contributions: []));

        var result = Compile(
            registry => registry.AddPackage(package),
            builder => builder.UseFeature(feature));

        AssertCode(result, "UTL1501");
    }

    [Test]
    public void RequiredContributionThatIsNotRegistered_ReportsUnregisteredContribution()
    {
        var feature = new LanguageFeatureId("planning.missing-contribution.feature");
        var package = PackageWithContributions(
            new LanguageFeatureDescriptor(
                feature,
                requires: [],
                contributions: [new LanguageContributionId("planning.known")]),
            contributions:
            [
                new LanguageContributionDescriptor(
                    new LanguageContributionId("planning.known"),
                    Tooling,
                    requiresContributions: [new LanguageContributionId("planning.absent")])
            ]);

        var result = Compile(
            registry => registry.AddPackage(package),
            builder => builder.UseFeature(feature));

        AssertCode(result, "UTL2001");
    }

    [Test]
    public void RequiredContributionThatWasExcluded_ReportsExclusion()
    {
        var feature = new LanguageFeatureId("planning.excluded-contribution.feature");
        var excluded = new LanguageContributionId("planning.excluded");
        var package = PackageWithContributions(
            new LanguageFeatureDescriptor(feature, requires: [], contributions: [excluded]),
            [new LanguageContributionDescriptor(excluded, Tooling)]);

        var result = Compile(
            registry => registry.AddPackage(package),
            builder => builder.UseFeature(feature).ExcludeContribution(excluded));

        AssertCode(result, "UTL2006");
    }

    [Test]
    public void PreferredCapabilityProviderThatDoesNotProvideTheCapability_IsRejected()
    {
        var feature = new LanguageFeatureId("planning.preferred.feature");
        var capability = new LanguageCapabilityId("planning.test.capability");
        var consumer = new LanguageContributionId("planning.consumer");
        var wrongProvider = new LanguageContributionId("planning.wrong-provider");
        var package = PackageWithContributions(
            new LanguageFeatureDescriptor(feature, requires: [], contributions: [consumer, wrongProvider]),
            [
                new LanguageContributionDescriptor(consumer, Tooling, requiresCapabilities: [capability]),
                new LanguageContributionDescriptor(wrongProvider, Tooling, providesCapabilities:
                    [new LanguageCapabilityId("planning.other.capability")])
            ]);

        var result = Compile(
            registry => registry.AddPackage(package),
            builder => builder.UseFeature(feature).PreferCapabilityProvider(capability, wrongProvider));

        AssertCode(result, "UTL2005");
    }

    [Test]
    public void ContributionOwnedByUnselectedFeature_IsRejected()
    {
        var ownerFeature = new LanguageFeatureId("planning.owner.feature");
        var otherFeature = new LanguageFeatureId("planning.other.feature");
        var owned = new LanguageContributionId("planning.owned.by.other");
        var consumer = new LanguageContributionId("planning.owner.consumer");
        // The owned contribution is registered and reachable, but belongs to a feature the definition did not
        // select. That is the condition the diagnostic exists for: registered is not the same as eligible.
        var package = new TestPackage(new LanguagePackageDescriptor(
            new LanguagePackageId("Planning.Tests"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [
                new LanguageFeatureDescriptor(ownerFeature, requires: [], contributions: [consumer]),
                new LanguageFeatureDescriptor(otherFeature, requires: [], contributions: [owned])
            ],
            contributions:
            [
                new LanguageContributionDescriptor(consumer, Tooling, requiresContributions: [owned]),
                new LanguageContributionDescriptor(owned, Tooling)
            ]));

        var result = Compile(
            registry => registry.AddPackage(package),
            builder => builder.UseFeature(ownerFeature));

        AssertCode(result, "UTL2014");
    }

    [Test]
    public void SlotReplacementPointingAtAnUnselectedContribution_IsRejected()
    {
        // The slot must have a resolved group before an override can fail against it; the override then names a
        // contribution that is not in that group. Without the first part the override has nothing to contradict.
        var feature = new LanguageFeatureId("planning.slot.feature");
        var resident = new LanguageContributionId("planning.slot.resident");
        var absent = new LanguageContributionId("planning.slot.absent");
        var slot = new LanguageSlotId("planning.slot.occupied");
        // The replacement is registered and resolved, but into a different slot. That is what makes the override
        // contradictory rather than merely unresolvable: the contribution exists, it is just not in this group.
        var otherSlot = new LanguageSlotId("planning.slot.elsewhere");
        var package = PackageWithContributions(
            new LanguageFeatureDescriptor(feature, requires: [], contributions: [resident, absent]),
            [
                new LanguageContributionDescriptor(resident, slot),
                new LanguageContributionDescriptor(absent, otherSlot)
            ]);

        var result = Compile(
            registry => registry.AddPackage(package),
            builder => builder.UseFeature(feature).ReplaceSlot(slot, absent));

        AssertCode(result, "UTL2102");
    }

    [Test]
    public void SlotReplacementNamingTheWrongCurrentOwner_IsRejected()
    {
        var feature = new LanguageFeatureId("planning.slot-owner.feature");
        var slot = new LanguageSlotId("planning.slot.owned");
        var original = new LanguageContributionId("planning.slot.original");
        var replacement = new LanguageContributionId("planning.slot.replacement");
        var phantomOwner = new LanguageContributionId("planning.slot.phantom");
        var package = PackageWithContributions(
            new LanguageFeatureDescriptor(feature, requires: [], contributions: [original, replacement]),
            [
                new LanguageContributionDescriptor(original, slot),
                new LanguageContributionDescriptor(replacement, slot)
            ]);

        var result = Compile(
            registry => registry.AddPackage(package),
            builder => builder.UseFeature(feature).ReplaceSlot(slot, replacement, phantomOwner));

        AssertCode(result, "UTL2103");
    }

    [Test]
    public void ContributionConflict_ReportsTheConflictingContribution()
    {
        var feature = new LanguageFeatureId("planning.conflict.feature");
        var first = new LanguageContributionId("planning.conflict.a");
        var second = new LanguageContributionId("planning.conflict.b");
        var package = PackageWithContributions(
            new LanguageFeatureDescriptor(feature, requires: [], contributions: [first, second]),
            [
                new LanguageContributionDescriptor(first, Tooling, conflicts: [second]),
                new LanguageContributionDescriptor(second, Tooling)
            ]);

        var result = Compile(
            registry => registry.AddPackage(package),
            builder => builder.UseFeature(feature));

        AssertCode(result, "UTL2010");
    }

    [Test]
    public void CapabilityConflict_ReportsTheConflictingCapability()
    {
        var feature = new LanguageFeatureId("planning.capability-conflict.feature");
        var first = new LanguageContributionId("planning.capability-conflict.a");
        var second = new LanguageContributionId("planning.capability-conflict.b");
        var capability = new LanguageCapabilityId("planning.conflicting.capability");
        var package = PackageWithContributions(
            new LanguageFeatureDescriptor(feature, requires: [], contributions: [first, second]),
            [
                new LanguageContributionDescriptor(first, Tooling, conflictsCapabilities: [capability]),
                new LanguageContributionDescriptor(second, Tooling, providesCapabilities: [capability])
            ]);

        var result = Compile(
            registry => registry.AddPackage(package),
            builder => builder.UseFeature(feature));

        AssertCode(result, "UTL2011");
    }

    [Test]
    public void OrderingAcrossTwoSlots_IsRejectedRatherThanSilentlyIgnored()
    {
        // The order constraint is definition-level and therefore has no slot of its own; contributions in
        // different slots cannot be ordered, and the diagnostic names both slots.
        var feature = new LanguageFeatureId("planning.cross-slot.feature");
        var first = new LanguageContributionId("planning.cross-slot.a");
        var second = new LanguageContributionId("planning.cross-slot.b");
        var slotA = new LanguageSlotId("planning.cross-slot.first");
        var slotB = new LanguageSlotId("planning.cross-slot.second");
        var package = PackageWithContributions(
            new LanguageFeatureDescriptor(feature, requires: [], contributions: [first, second]),
            [
                new LanguageContributionDescriptor(first, slotA),
                new LanguageContributionDescriptor(second, slotB)
            ]);

        var result = Compile(
            registry => registry.AddPackage(package),
            builder => builder.UseFeature(feature).RequireContributionOrder(first, second));

        AssertCode(result, "UTL2111");
    }

    [Test]
    public void SingleOwnerSlotWithoutAnOverride_ReportsTheSlotConflict()
    {
        // UTL2101 is documented, and this fixture is the smallest shape that reaches it: two contributions
        // declaring the same single-owner slot without a definition-level replacement.
        var feature = new LanguageFeatureId("planning.single-owner.feature");
        var slot = new LanguageSlotId("planning.single-owner.slot");
        var first = new LanguageContributionId("planning.single-owner.a");
        var second = new LanguageContributionId("planning.single-owner.b");
        var package = PackageWithContributions(
            new LanguageFeatureDescriptor(feature, requires: [], contributions: [first, second]),
            [
                new LanguageContributionDescriptor(
                    first, slot, LanguageSlotMultiplicity.Single, ContributionMergePolicy.RejectDuplicate),
                new LanguageContributionDescriptor(
                    second, slot, LanguageSlotMultiplicity.Single, ContributionMergePolicy.RejectDuplicate)
            ]);

        var result = Compile(
            registry => registry.AddPackage(package),
            builder => builder.UseFeature(feature));

        AssertCode(result, "UTL2101");
    }

    private static void AssertCode(LanguageBuildResult result, string code)
    {
        var codes = string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Code), Does.Contain(code),
                $"{code} was not reported; observed: [{codes}]");
            // The code must be reported as an error, and a failing compilation must not silently return a plan.
            Assert.That(
                result.Diagnostics.First(diagnostic => diagnostic.Code == code).Severity,
                Is.EqualTo(LanguageDiagnosticSeverity.Error));
            Assert.That(result.Plan, Is.Null);
        });
    }

    private static LanguageBuildResult Compile(
        Func<LanguagePackageRegistry, LanguagePackageRegistry> configureRegistry,
        Func<LanguageDefinitionBuilder, LanguageDefinitionBuilder> configureDefinition) =>
        new LanguageCompiler(configureRegistry(new LanguagePackageRegistry())).Compile(
            configureDefinition(LanguageDefinitionBuilder.Create("Planning.Tests", "1.0.0")).Build());

    /// <summary>A package whose features carry their own contribution lists.</summary>
    private static ILanguageExtensionPackage Package(params LanguageFeatureDescriptor[] features) =>
        new TestPackage(new LanguagePackageDescriptor(
            new LanguagePackageId("Planning.Tests"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            features));

    /// <summary>A package whose contributions are declared at package level.</summary>
    private static ILanguageExtensionPackage PackageWithContributions(
        LanguageFeatureDescriptor feature,
        LanguageContributionDescriptor[] contributions) =>
        new TestPackage(new LanguagePackageDescriptor(
            new LanguagePackageId("Planning.Tests"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [feature],
            contributions: contributions));

    private sealed class TestPackage(LanguagePackageDescriptor descriptor) : ILanguageExtensionPackage
    {
        public LanguagePackageDescriptor Descriptor { get; } = descriptor;
    }
}
