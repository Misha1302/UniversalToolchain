using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

/// <summary>
/// Contract for module descriptors (UT-A005 / ADR-008).
/// <para>
/// The load-bearing property is negative: a module must never be able to change what the planner
/// selects. These tests pin that property directly rather than relying on review.
/// </para>
/// </summary>
[TestFixture]
public sealed class ModuleDescriptorContractTests
{
    private static readonly LanguageContributionId Alpha = new("test.module.alpha");
    private static readonly LanguageContributionId Beta = new("test.module.beta");

    [Test]
    public void Descriptor_SnapshotsContributions_SoCallerMutationCannotChangeIt()
    {
        var contributions = new List<LanguageContributionId> { Beta, Alpha };
        var descriptor = new LanguageModuleDescriptor(
            new LanguageModuleId("test.module"),
            [LanguageModulePhase.Lowering],
            contributions);
        contributions.Clear();

        Assert.That(descriptor.Contributions, Is.EqualTo(new[] { Alpha, Beta }),
            "contributions must be snapshotted and ordered canonically, not aliased from the caller");
    }

    [Test]
    public void Descriptor_RejectsEmptyContributionSet()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new LanguageModuleDescriptor(new LanguageModuleId("test.module"), [LanguageModulePhase.Lowering], []));

        Assert.That(exception!.ParamName, Is.EqualTo("contributions"));
    }

    [Test]
    public void Descriptor_RejectsEmptyPhaseSet()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new LanguageModuleDescriptor(new LanguageModuleId("test.module"), [], [Alpha]));

        Assert.That(exception!.ParamName, Is.EqualTo("phases"));
    }

    [Test]
    public void Descriptor_RejectsSelfRequirement()
    {
        var id = new LanguageModuleId("test.module");
        var exception = Assert.Throws<ArgumentException>(() =>
            new LanguageModuleDescriptor(
                id,
                [LanguageModulePhase.Lowering],
                [Alpha],
                requiresModules: [id]));

        Assert.That(exception!.ParamName, Is.EqualTo("requiresModules"));
    }

    [Test]
    public void Descriptor_RejectsUndefinedPhase()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LanguageModuleDescriptor(
                new LanguageModuleId("test.module"),
                [(LanguageModulePhase)99],
                [Alpha]));

        Assert.That(exception!.ParamName, Is.EqualTo("phases"));
    }

    [Test]
    public void Registry_ResolvesOneOwnerPerPhase_ForTheSameContribution()
    {
        var registry = new LanguageModuleRegistry()
            .Add(new LanguageModuleDescriptor(new LanguageModuleId("test.first"), [LanguageModulePhase.Binding], [Alpha]))
            .Add(new LanguageModuleDescriptor(new LanguageModuleId("test.second"), [LanguageModulePhase.Lowering], [Alpha]));

        Assert.Multiple(() =>
        {
            Assert.That(registry.TryGetOwner(LanguageModulePhase.Binding, Alpha, out var binding), Is.True);
            Assert.That(binding!.Id.Value, Is.EqualTo("test.first"));
            Assert.That(registry.TryGetOwner(LanguageModulePhase.Lowering, Alpha, out var lowering), Is.True);
            Assert.That(lowering!.Id.Value, Is.EqualTo("test.second"));
            Assert.That(registry.TryGetOwner(LanguageModulePhase.Emission, Alpha, out _), Is.False);
            Assert.That(registry.OwnedContributions, Is.EqualTo(new[] { Alpha }));
        });
    }

    [Test]
    public void Registry_DuplicatePhaseOwnership_IsExplicitError_NotTieBreak()
    {
        var registry = new LanguageModuleRegistry()
            .Add(new LanguageModuleDescriptor(new LanguageModuleId("test.first"), [LanguageModulePhase.Lowering], [Alpha]));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            registry.Add(new LanguageModuleDescriptor(new LanguageModuleId("test.second"), [LanguageModulePhase.Lowering], [Alpha])));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("test.first"));
            Assert.That(exception.Message, Does.Contain("test.second"));
            Assert.That(exception.Message, Does.Contain(Alpha.Value));
        });
    }

    [Test]
    public void Registry_DuplicateOwnershipInsideOneBatch_IsRejectedAndLeavesRegistryUnchanged()
    {
        var registry = new LanguageModuleRegistry();
        var exception = Assert.Throws<InvalidOperationException>(() => registry.AddRange([
            new LanguageModuleDescriptor(new LanguageModuleId("test.first"), [LanguageModulePhase.Lowering], [Alpha]),
            new LanguageModuleDescriptor(new LanguageModuleId("test.second"), [LanguageModulePhase.Lowering], [Alpha])
        ]));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("same batch"));
            Assert.That(registry.Modules, Is.Empty, "a rejected batch must not partially register");
        });
    }

    [Test]
    public void Registry_DuplicateModuleId_IsRejected()
    {
        var registry = new LanguageModuleRegistry()
            .Add(new LanguageModuleDescriptor(new LanguageModuleId("test.module"), [LanguageModulePhase.Binding], [Alpha]));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            registry.Add(new LanguageModuleDescriptor(new LanguageModuleId("test.module"), [LanguageModulePhase.Lowering], [Beta])));

        Assert.That(exception!.Message, Does.Contain("already registered"));
    }

    [Test]
    public void Registry_ModuleOrder_IsCanonicalNotInsertionOrder()
    {
        var forward = new LanguageModuleRegistry()
            .Add(new LanguageModuleDescriptor(new LanguageModuleId("test.a"), [LanguageModulePhase.Binding], [Alpha]))
            .Add(new LanguageModuleDescriptor(new LanguageModuleId("test.b"), [LanguageModulePhase.Binding], [Beta]));
        var reversed = new LanguageModuleRegistry()
            .Add(new LanguageModuleDescriptor(new LanguageModuleId("test.b"), [LanguageModulePhase.Binding], [Beta]))
            .Add(new LanguageModuleDescriptor(new LanguageModuleId("test.a"), [LanguageModulePhase.Binding], [Alpha]));

        Assert.That(
            forward.Modules.Select(static module => module.Id.Value),
            Is.EqualTo(reversed.Modules.Select(static module => module.Id.Value)));
    }

    [Test]
    public void WistModuleDescriptors_DeclareEveryCatalogComponentExactlyOnce()
    {
        var modules = WistModuleDescriptors.Create();
        var expected = WistRuntimeComponentCatalog.ContributionsForTest.Count;

        Assert.Multiple(() =>
        {
            Assert.That(modules, Has.Count.EqualTo(expected));
            Assert.That(
                modules.Select(static module => module.Id.Value).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(modules.Count),
                "module identities must be unique");
        });
    }

    [Test]
    public void WistModuleRegistry_ResolvesLoweringOwnerExactlyWhenLowererSupportsTheContribution()
    {
        // The registry and the hardcoded lowering predicate must agree, otherwise the two answers to
        // "can this module lower?" would drift apart during the UT-A046 migration.
        var registry = new LanguageModuleRegistry().AddRange(WistModuleDescriptors.Create());
        var catalog = WistRuntimeComponentCatalog.ContributionsForTest;
        var mismatches = new List<string>();

        foreach (var contribution in catalog)
        {
            var supported = WistSemanticBytecodeLowerer.SupportsModuleContribution(contribution);
            var owned = registry.TryGetOwner(LanguageModulePhase.Lowering, contribution, out _);
            if (supported != owned)
                mismatches.Add($"{contribution.Value}: switch={supported} registry={owned}");
        }

        Assert.That(mismatches, Is.Empty, string.Join(Environment.NewLine, mismatches));
    }

    [Test]
    public void ModuleDescriptors_CannotChangeAnyWistPlanHash()
    {
        // The planner's inputs are features and contributions. Modules describe those contributions,
        // so expressing them must be indistinguishable to the planner. This is the regression that
        // keeps a future "let modules select contributions" change out.
        var baseline = GoldenPlanHashes.Load();
        var actual = new Dictionary<string, string>(StringComparer.Ordinal);
        var compiler = new LanguageCompiler(
            new LanguagePackageRegistry().AddPackage(new WistLanguageFeaturePackage()));

        foreach (var presetId in WistLanguageDefinitions.PresetIds.OrderBy(static id => id, StringComparer.Ordinal))
        {
            foreach (var backend in WistLanguageDefinitions.Create(presetId).Backends
                         .OrderBy(static b => b.Value, StringComparer.Ordinal))
            {
                var definition = WistFacadeLanguageDefinitionFactory.FromPreset(
                    presetId, backend.Value, WistFacadeSsaPolicy.Disabled);
                var result = compiler.Compile(definition);
                Assert.That(result.IsSuccess, Is.True,
                    $"{presetId}/{backend.Value}: " + string.Join(
                        Environment.NewLine,
                        result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
                actual[$"{presetId}|{backend.Value}"] = result.Plan!.PlanHash;
            }
        }

        Assert.That(actual, Is.EqualTo(baseline),
            "module descriptors must not participate in planning: every preset/backend plan hash must be unchanged");
    }

    [Test]
    public void ModuleDescriptors_CannotChangeTheWistPackageManifestHash()
    {
        var baseline = GoldenPlanHashes.LoadManifestHash();
        var actual = LanguageFeatureManifestSerializer.ComputeSha256(new WistLanguageFeaturePackage().Descriptor);

        Assert.That(actual, Is.EqualTo(baseline));
    }
}
