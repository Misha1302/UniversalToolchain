using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistCanonicalConcurrencyTests
{
    private static readonly BackendId Interpreter = new("interpreter");

    [Test]
    public async Task ParallelDslPlanning_DoesNotMixSelectedFeaturesOrBackends()
    {
        var cases = new[]
        {
            new DialectCase(
                "dialect ArithmeticOnly\nuse Arithmetic,Numbers,Scopes,Whitespaces\nbackend interpreter\nsecurity restricted",
                new[] { "interpreter" }),
            new DialectCase(
                "dialect Variables\nuse Arithmetic,Numbers,Scopes,Variables,Whitespaces\nbackend interpreter,cil\nsecurity restricted",
                new[] { "cil", "interpreter" }),
            new DialectCase(
                "dialect Conditions\nuse Arithmetic,Conditions,Numbers,Scopes,Whitespaces\nbackend cil\nsecurity restricted",
                new[] { "cil" })
        };
        var expected = cases
            .Select(testCase => CreateSemanticProjection(
                Compile(testCase.Source, "canonical-oracle.wistdialect", testCase.Backends)))
            .ToArray();

        var results = await Task.WhenAll(Enumerable.Range(0, 48).Select(index => Task.Run(() =>
        {
            var caseIndex = index % cases.Length;
            var testCase = cases[caseIndex];
            return new ParallelPlanProjection(
                caseIndex,
                CreateSemanticProjection(
                    Compile(testCase.Source, $"parallel-{index}.wistdialect", testCase.Backends)));
        })));

        foreach (var result in results)
        {
            Assert.That(
                result.Projection.Features,
                Is.EqualTo(expected[result.CaseIndex].Features),
                cases[result.CaseIndex].Source);
            Assert.That(
                result.Projection.Backends,
                Is.EqualTo(expected[result.CaseIndex].Backends),
                cases[result.CaseIndex].Source);
        }
    }

    [Test]
    public void RepeatedDslPlanning_WithSameSourceIdentity_ProducesOneStablePlanHash()
    {
        const string source = "dialect Stable\nuse Arithmetic,Numbers,Scopes,Whitespaces\nbackend interpreter\nsecurity restricted";

        var hashes = Enumerable.Range(0, 64)
            .Select(_ => Compile(source, "stable.wistdialect").PlanHash)
            .ToArray();

        Assert.That(hashes.Distinct(StringComparer.Ordinal).ToArray(), Has.Length.EqualTo(1));
    }

    [Test]
    public void EquivalentDsl_WithDifferentSourceNames_PreservesSemanticProjection()
    {
        const string source = "dialect StableSemantic\nuse Arithmetic,Numbers,Scopes,Whitespaces\nbackend interpreter\nsecurity restricted";
        var first = Compile(source, "first.wistdialect");
        var second = Compile(source, "second.wistdialect");

        Assert.Multiple(() =>
        {
            Assert.That(first.Definition.SelectedFeatures, Is.EqualTo(second.Definition.SelectedFeatures));
            Assert.That(first.Definition.Backends, Is.EqualTo(second.Definition.Backends));
            Assert.That(
                first.Contributions.Select(static contribution => contribution.Contribution.Id),
                Is.EqualTo(second.Contributions.Select(static contribution => contribution.Contribution.Id)));
            Assert.That(
                first.Routes.Select(static route => (route.Key, route.Value.TargetContract)),
                Is.EqualTo(second.Routes.Select(static route => (route.Key, route.Value.TargetContract))));
        });
    }

    [Test]
    public void FailedDslPlanning_DoesNotPoisonNextSuccessfulPlan()
    {
        const string invalid = "dialect Broken\nuse MissingModule\nbackend interpreter\nsecurity restricted";
        const string valid = "dialect Good\nuse Arithmetic,Numbers,Scopes,Whitespaces\nbackend interpreter\nsecurity restricted";

        var error = Assert.Throws<InvalidOperationException>(() => Compile(invalid, "broken.wistdialect"));
        Assert.That(error!.Message, Does.Contain("MissingModule").And.Contain("not a canonical module component"));

        var first = Compile(valid, "good.wistdialect");
        var second = Compile(valid, "good.wistdialect");

        Assert.Multiple(() =>
        {
            Assert.That(first.PlanHash, Is.EqualTo(second.PlanHash));
            Assert.That(first.Definition.SelectedFeatures, Is.EqualTo(second.Definition.SelectedFeatures));
            Assert.That(first.Definition.Backends, Is.EqualTo(second.Definition.Backends));
        });
    }

    [Test]
    public async Task ParallelExactRuntimeSessions_RemainIndependentAndDeterministic()
    {
        const string source = "dialect RuntimeStable\nuse Arithmetic,Numbers,Scopes,Whitespaces\nbackend interpreter\nsecurity restricted";
        var package = new WistLanguageFeaturePackage();
        var definition = WistFacadeLanguageDefinitionFactory.FromDialectText(
            source,
            "runtime-stable.wistdialect",
            Interpreter.Value,
            WistFacadeSsaPolicy.Disabled);
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();

        var results = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
        {
            using var runtime = LanguageRuntime.Create(
                plan,
                new ILanguageRouteComponentSource[] { package });
            return runtime.Run(new LanguageExecutionRequest("2 + 3", Interpreter)).Value?.ToString();
        })));

        Assert.That(results, Is.All.EqualTo("5"));
    }

    private static LanguagePlan Compile(
        string source,
        string sourceName,
        IReadOnlyList<string>? backendNames = null)
    {
        backendNames ??= [Interpreter.Value];
        if (backendNames.Count == 0)
            throw new ArgumentException("At least one backend is required.", nameof(backendNames));

        var backends = backendNames
            .Select(static backend => new BackendId(backend))
            .Distinct()
            .ToArray();
        var definitions = backends
            .Select(backend => WistFacadeLanguageDefinitionFactory.FromDialectText(
                source,
                sourceName,
                backend.Value,
                WistFacadeSsaPolicy.Disabled))
            .ToArray();
        var baseline = definitions[0];

        foreach (var candidate in definitions.Skip(1))
            EnsureBackendIndependentSemantics(baseline, candidate);

        var definition = new LanguageDefinition(
            baseline.Id,
            baseline.Version,
            baseline.ToolchainApiVersion,
            baseline.SelectedFeatures,
            backends,
            baseline.RuntimeProvider,
            baseline.RuntimePolicy,
            baseline.Metadata,
            baseline.SlotOverrides,
            baseline.CapabilityProviders,
            baseline.ExcludedContributions,
            baseline.EntryArtifact,
            baseline.ContributionOrderConstraints,
            baseline.IntrinsicPolicy);
        var package = new WistLanguageFeaturePackage();
        return new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();
    }

    private static void EnsureBackendIndependentSemantics(
        LanguageDefinition baseline,
        LanguageDefinition candidate)
    {
        if (candidate.Id != baseline.Id ||
            candidate.Version != baseline.Version ||
            !candidate.SelectedFeatures.SequenceEqual(baseline.SelectedFeatures) ||
            candidate.RuntimeProvider != baseline.RuntimeProvider ||
            candidate.RuntimePolicy != baseline.RuntimePolicy ||
            !candidate.ContributionOrderConstraints.SequenceEqual(baseline.ContributionOrderConstraints) ||
            !candidate.IntrinsicPolicy.SequenceEqual(baseline.IntrinsicPolicy))
        {
            throw new InvalidOperationException(
                "Wist concurrency fixture produced backend-dependent semantics before canonical planning.");
        }
    }

    private static CanonicalCaseProjection CreateSemanticProjection(LanguagePlan plan) => new(
        plan.Definition.SelectedFeatures.OrderBy(static feature => feature.Value, StringComparer.Ordinal).ToArray(),
        plan.Definition.Backends.Select(static backend => backend.Value).OrderBy(static value => value, StringComparer.Ordinal).ToArray());

    private sealed record DialectCase(
        string Source,
        IReadOnlyList<string> Backends);

    private sealed record CanonicalCaseProjection(
        IReadOnlyList<LanguageFeatureId> Features,
        IReadOnlyList<string> Backends);

    private sealed record ParallelPlanProjection(
        int CaseIndex,
        CanonicalCaseProjection Projection);
}