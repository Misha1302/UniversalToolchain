using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Wist.LanguagePack;

namespace Tests.Architecture;

/// <summary>
/// Determinism gate (UT-A054).
/// <para>
/// Every plan-shaped artifact is expected to be a pure function of the <em>set</em> of selected
/// features, backends and packages, never of the order in which they were declared. That property is
/// currently true, but it is true for the fragile reason that each phase happens to sort. The day one
/// sort is replaced by "just iterate the list" — a small, plausible, compiling edit — the plan becomes
/// order-dependent, and the observable symptom is not a crash but a different route or lock file for
/// the same language. Nothing in the suite would notice.
/// </para>
/// <para>
/// So the property is pinned by measurement over a shuffled corpus rather than by reading the sort
/// calls. The corpus is every shipped Wist preset, every backend each one declares, and several
/// declaration permutations per preset with fixed seeds, because a real <see cref="HashSet{T}"/>
/// below the abstraction would surface as an order-sensitive plan.
/// </para>
/// <para>
/// <see cref="GateCanFail"/> is the part that makes this a gate and not a decoration: it runs a
/// deliberately order-sensitive stub through the same comparison and requires that comparison to
/// report a difference. Without it, a refactor that silently stopped comparing anything would leave
/// this fixture green.
/// </para>
/// </summary>
[TestFixture]
public sealed class PlanDeterminismGateTests
{
    private static readonly BackendId Interpreter = new("interpreter");
    private static readonly BackendId Cil = new("cil");

    [Test]
    public void EveryShippedPreset_ProducesTheSamePlanRegardlessOfDeclarationOrder()
    {
        var failures = new List<string>();

        foreach (var presetId in WistLanguageDefinitions.PresetIds)
        {
            var definition = WistLanguageDefinitions.Create(presetId);
            var package = new WistLanguageFeaturePackage();
            var baseline = Compile(package, definition);

            // Eight fixed seeds: enough permutations to hit the wrong side of a small hash bucket,
            // deterministic so a failure is reproducible from the test name alone.
            foreach (var seed in Seeds)
            {
                var shuffled = Shuffle(definition, seed);
                var candidate = Compile(package, shuffled);

                Compare(failures, $"{presetId}/seed{seed}", baseline, candidate);
            }
        }

        Assert.That(failures, Is.Empty, Format(failures));
    }

    [Test]
    public void EveryShippedBackend_ProducesTheSameRouteRegardlessOfDeclarationOrder()
    {
        var failures = new List<string>();

        foreach (var presetId in WistLanguageDefinitions.PresetIds)
        {
            var definition = WistLanguageDefinitions.Create(presetId);
            var package = new WistLanguageFeaturePackage();

            foreach (var backend in definition.Backends)
            {
                var baseline = Compile(package, definition);

                foreach (var seed in Seeds)
                {
                    var candidate = Compile(package, Shuffle(definition, seed));
                    if (!baseline.Routes.TryGetValue(backend, out var expectedRoute))
                        continue;
                    if (!candidate.Routes.TryGetValue(backend, out var actualRoute))
                    {
                        failures.Add($"{presetId}/{backend.Value}/seed{seed}: backend route disappeared");
                        continue;
                    }

                    var expected = string.Join(
                        " -> ",
                        expectedRoute.Steps.Select(static step => step.ContributionId.Value));
                    var actual = string.Join(
                        " -> ",
                        actualRoute.Steps.Select(static step => step.ContributionId.Value));
                    if (!StringComparer.Ordinal.Equals(expected, actual))
                    {
                        failures.Add(
                            $"{presetId}/{backend.Value}/seed{seed}: route differs{Environment.NewLine}" +
                            $"    expected {expected}{Environment.NewLine}" +
                            $"    actual   {actual}");
                    }
                }
            }
        }

        Assert.That(failures, Is.Empty, Format(failures));
    }

    [Test]
    public void BackendDeclarationOrder_DoesNotChangeTheLockFileOrPlanHash()
    {
        // Backends are the axis most likely to leak ordering: resolution iterates them, and the lock
        // file serialises per-backend data. A HashSet of BackendId would show up exactly here.
        var definition = WistLanguageDefinitions.Create(WistLanguageDefinitions.FullDefaultId);
        var package = new WistLanguageFeaturePackage();
        var baseline = Compile(package, definition);

        var reordered = new LanguageDefinition(
            definition.Id,
            definition.Version,
            definition.ToolchainApiVersion,
            definition.SelectedFeatures,
            definition.Backends.Reverse().ToArray(),
            definition.RuntimeProvider,
            definition.RuntimePolicy,
            definition.Metadata,
            definition.SlotOverrides,
            definition.CapabilityProviders,
            definition.ExcludedContributions,
            definition.EntryArtifact,
            definition.ContributionOrderConstraints,
            definition.IntrinsicPolicy);

        var candidate = Compile(package, reordered);

        Assert.Multiple(() =>
        {
            Assert.That(
                Convert.ToHexString(LanguageLockFile.SerializeCanonical(candidate)),
                Is.EqualTo(Convert.ToHexString(LanguageLockFile.SerializeCanonical(baseline))),
                "reversing backend declaration order changed the canonical lock bytes");
            Assert.That(candidate.PlanHash, Is.EqualTo(baseline.PlanHash));
        });
    }

    [Test]
    public void DiagnosticSequence_IsStableRegardlessOfDeclarationOrder()
    {
        // Diagnostics are user-facing output: an error list that reorders between runs makes bug
        // reports non-comparable and makes "the same failure" look like two different failures.
        // A definition that fails for several independent reasons exercises the ordering.
        var failing = new LanguageDefinition(
            new LanguageId("determinism.diagnostics"),
            new LanguageVersion("1"),
            new ToolchainApiVersion(1),
            [new LanguageFeatureId("determinism.missing.feature.a"), new LanguageFeatureId("determinism.missing.feature.b")],
            [Interpreter],
            runtimeProvider: null,
            runtimePolicy: null,
            metadata: null);

        var package = new WistLanguageFeaturePackage();
        var baseline = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(failing);
        var reversed = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(new LanguageDefinition(
                failing.Id,
                failing.Version,
                failing.ToolchainApiVersion,
                failing.SelectedFeatures.Reverse().ToArray(),
                failing.Backends,
                failing.RuntimeProvider,
                failing.RuntimePolicy,
                failing.Metadata,
                failing.SlotOverrides,
                failing.CapabilityProviders,
                failing.ExcludedContributions,
                failing.EntryArtifact,
                failing.ContributionOrderConstraints,
                failing.IntrinsicPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(baseline.Plan, Is.Null, "the probe definition must actually fail, otherwise ordering is untested");
            Assert.That(
                reversed.Diagnostics.Select(static d => $"{d.Code}|{d.Message}"),
                Is.EqualTo(baseline.Diagnostics.Select(static d => $"{d.Code}|{d.Message}")));
        });
    }

    [Test]
    public void RepeatedCompilationOfTheSameDefinition_IsByteIdentical()
    {
        var package = new WistLanguageFeaturePackage();
        var definition = WistLanguageDefinitions.Create(WistLanguageDefinitions.FullDefaultId);

        var first = Compile(package, definition);
        var second = Compile(package, definition);
        var third = Compile(package, definition);

        var expected = Convert.ToHexString(LanguageLockFile.SerializeCanonical(first));
        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(LanguageLockFile.SerializeCanonical(second)), Is.EqualTo(expected));
            Assert.That(Convert.ToHexString(LanguageLockFile.SerializeCanonical(third)), Is.EqualTo(expected));
        });
    }

    [Test]
    public void GateCanFail()
    {
        // The mutant self-test. An order-sensitive "plan" compared against its shuffled twin must be
        // reported as different. If this passes while the real corpus also passes, the corpus is
        // vacuous and the gate proves nothing.
        var ordered = Enumerable.Range(0, 8).Select(static value => $"{value}").ToArray();
        var shuffled = ordered.Reverse().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                string.Join("|", shuffled),
                Is.Not.EqualTo(string.Join("|", ordered)),
                "the mutant comparison must detect a difference, otherwise the gate cannot fail");

            var failures = new List<string>();
            Compare(failures, "hashset-stub", ordered, shuffled);
            Assert.That(failures, Is.Not.Empty,
                "the comparison used by the corpus reported no difference for an order-sensitive input; " +
                "every green result in this fixture would be meaningless");
        });
    }

    [Test]
    public void Corpus_CoversEveryPresetAndBothShippedBackends()
    {
        // A gate over an empty or narrowed corpus passes for the wrong reason. This is the coverage
        // assertion that keeps the loop above honest.
        var presets = WistLanguageDefinitions.PresetIds;
        var backends = presets
            .Select(WistLanguageDefinitions.Create)
            .SelectMany(static definition => definition.Backends)
            .Select(static backend => backend.Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(presets, Has.Count.EqualTo(9), "the shipped Wist preset set changed");
            Assert.That(backends, Is.EquivalentTo(new[] { Interpreter.Value, Cil.Value }),
                "the corpus must exercise both shipped backends");
        });
    }

    private static readonly int[] Seeds = [1, 2, 3, 5, 8, 13, 21, 34];

    private static void Compare(
        List<string> failures,
        string label,
        LanguagePlan expected,
        LanguagePlan actual)
    {
        Compare(
            failures,
            label,
            Convert.ToHexString(LanguageLockFile.SerializeCanonical(expected)),
            Convert.ToHexString(LanguageLockFile.SerializeCanonical(actual)));

        if (!StringComparer.Ordinal.Equals(expected.PlanHash, actual.PlanHash))
            failures.Add($"{label}: plan hash {expected.PlanHash} != {actual.PlanHash}");

        // Canonical bytes are sorted by `LanguagePlanCanonicalizer`, so they cannot observe a planner
        // that emitted contributions in a different order: the serializer would hide it. The raw
        // sequence can observe it, and the raw sequence is what runtime dispatch iterates.
        Compare(
            failures,
            $"{label}/raw-contributions",
            expected.Contributions.Select(static c => c.Contribution.Id.Value).ToArray(),
            actual.Contributions.Select(static c => c.Contribution.Id.Value).ToArray());

        foreach (var backend in expected.Routes.Keys.OrderBy(static key => key.Value, StringComparer.Ordinal))
        {
            if (!actual.Routes.TryGetValue(backend, out var actualRoute))
                continue;
            Compare(
                failures,
                $"{label}/raw-route/{backend.Value}",
                expected.Routes[backend].Steps.Select(static step => step.ContributionId.Value).ToArray(),
                actualRoute.Steps.Select(static step => step.ContributionId.Value).ToArray());
        }
    }

    private static void Compare(List<string> failures, string label, string expected, string actual)
    {
        if (StringComparer.Ordinal.Equals(expected, actual))
            return;
        failures.Add(
            $"{label}: canonical bytes differ{Environment.NewLine}" +
            $"    expected {expected}{Environment.NewLine}" +
            $"    actual   {actual}");
    }

    private static void Compare(
        List<string> failures,
        string label,
        IReadOnlyList<string> expected,
        IReadOnlyList<string> actual) =>
        Compare(failures, label, string.Join("|", expected), string.Join("|", actual));

    private static LanguagePlan Compile(WistLanguageFeaturePackage package, LanguageDefinition definition)
    {
        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition);
        Assert.That(result.Plan, Is.Not.Null,
            $"preset '{definition.Id.Value}' did not compile: {string.Join("; ", result.Diagnostics.Select(static d => d.Code))}");
        return result.Plan!;
    }

    /// <summary>
    /// Deterministically permutes the declaration order of a definition without changing its content.
    /// </summary>
    private static LanguageDefinition Shuffle(LanguageDefinition definition, int seed) =>
        new(
            definition.Id,
            definition.Version,
            definition.ToolchainApiVersion,
            Permute(definition.SelectedFeatures, seed),
            Permute(definition.Backends, seed + 1),
            definition.RuntimeProvider,
            definition.RuntimePolicy,
            definition.Metadata,
            definition.SlotOverrides,
            definition.CapabilityProviders,
            definition.ExcludedContributions,
            definition.EntryArtifact,
            definition.ContributionOrderConstraints,
            definition.IntrinsicPolicy);

    private static IReadOnlyList<T> Permute<T>(IReadOnlyList<T> items, int seed)
    {
        var copy = items.ToArray();
        // xorshift32: deterministic, no dependency on the runtime's Random implementation, which has
        // changed shape between .NET versions and would make a failure non-reproducible later.
        var state = (uint)(seed * 2654435761u) | 1u;
        for (var index = copy.Length - 1; index > 0; index--)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            var swap = (int)(state % (uint)(index + 1));
            (copy[index], copy[swap]) = (copy[swap], copy[index]);
        }

        return copy;
    }

    private static string Format(List<string> failures) =>
        failures.Count == 0
            ? string.Empty
            : $"{failures.Count} determinism violation(s):{Environment.NewLine}" +
              string.Join(Environment.NewLine, failures);
}
