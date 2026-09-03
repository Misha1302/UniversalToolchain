using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class PlannerCompositionAdversarialCountertests
{
    private static readonly LanguageRuntimeComponentTraits SafeTraits =
        LanguageRuntimeComponentTraits.DeterministicNoHostInterop;

    [Test]
    public void Planner_ShouldNotPreferOverflowedRoute_WhenCostsExceedIntRange()
    {
        var middle = new LanguageArtifactKind<int>("counter.overflow.middle");
        var target = new LanguageArtifactKind<long>("counter.overflow.target");
        var backend = new BackendId("counter.overflow.backend");
        var directId = new LanguageContributionId("counter.overflow.direct");
        var package = LanguagePackageBuilder.Create("Counter.Overflow", "1")
            .AddFeature("counter.overflow.core", feature => feature
                .AddTransformer(
                    directId.Value,
                    new LanguageSlotId("counter.overflow.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    target,
                    static (_, _) => 1L,
                    SafeTraits,
                    cost: 100)
                .AddTransformer(
                    "counter.overflow.huge.first",
                    new LanguageSlotId("counter.overflow.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    middle,
                    static (_, _) => 1,
                    SafeTraits,
                    cost: int.MaxValue)
                .AddTransformer(
                    "counter.overflow.huge.second",
                    new LanguageSlotId("counter.overflow.routes"),
                    middle,
                    target,
                    static (value, _) => value,
                    SafeTraits,
                    cost: int.MaxValue)
                .AddBackend(
                    backend,
                    new LanguageContributionId("counter.overflow.executor"),
                    target,
                    static (value, _) => value,
                    SafeTraits))
            .UseRouteRuntime("counter.overflow.runtime", "1")
            .Build();

        LanguageBuildResult? result = null;
        Assert.DoesNotThrow(() => result = Compile(package, "counter.overflow.core", backend));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsSuccess, Is.True, FormatDiagnostics(result));
        Assert.That(
            result.Plan!.Routes[backend].Steps.Select(static step => step.ContributionId),
            Does.Contain(directId));
    }

    [Test]
    public void Planner_ShouldNotRejectGloballyValidRoute_WhenSelectedPassRequiresAlternativePath()
    {
        var middle = new LanguageArtifactKind<int>("counter.global.middle");
        var target = new LanguageArtifactKind<long>("counter.global.target");
        var backend = new BackendId("counter.global.backend");
        var passId = new LanguageContributionId("counter.global.required-pass");
        var package = LanguagePackageBuilder.Create("Counter.GlobalRoute", "1")
            .AddFeature("counter.global.core", feature => feature
                .AddTransformer(
                    "counter.global.cheap-direct",
                    new LanguageSlotId("counter.global.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    target,
                    static (_, _) => 1L,
                    SafeTraits,
                    cost: 1)
                .AddTransformer(
                    "counter.global.via-middle.first",
                    new LanguageSlotId("counter.global.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    middle,
                    static (_, _) => 1,
                    SafeTraits,
                    cost: 2)
                .AddTransformer(
                    "counter.global.via-middle.second",
                    new LanguageSlotId("counter.global.routes"),
                    middle,
                    target,
                    static (value, _) => value,
                    SafeTraits,
                    cost: 2)
                .AddPass(
                    passId.Value,
                    LanguageSlots.Optimizers,
                    middle,
                    static (value, _) => value + 1,
                    SafeTraits)
                .AddBackend(
                    backend,
                    new LanguageContributionId("counter.global.executor"),
                    target,
                    static (value, _) => value,
                    SafeTraits))
            .UseRouteRuntime("counter.global.runtime", "1")
            .Build();

        var result = Compile(package, "counter.global.core", backend);

        Assert.That(result.IsSuccess, Is.True, FormatDiagnostics(result));
        Assert.That(
            result.Plan!.Routes[backend].Steps.Select(static step => step.ContributionId),
            Does.Contain(passId),
            "A complete route exists through the middle artifact, so the selected pass should be placeable.");
    }

    [Test]
    public void Planner_ShouldNotResolveSemanticRouteAmbiguityByContributionIdAlone()
    {
        var target = new LanguageArtifactKind<int>("counter.ambiguity.target");
        var backend = new BackendId("counter.ambiguity.backend");
        var package = LanguagePackageBuilder.Create("Counter.Ambiguity", "1")
            .AddFeature("counter.ambiguity.core", feature => feature
                .AddTransformer(
                    "counter.ambiguity.a-route",
                    new LanguageSlotId("counter.ambiguity.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    target,
                    static (_, _) => 1,
                    SafeTraits,
                    cost: 1)
                .AddTransformer(
                    "counter.ambiguity.z-route",
                    new LanguageSlotId("counter.ambiguity.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    target,
                    static (_, _) => 2,
                    SafeTraits,
                    cost: 1)
                .AddBackend(
                    backend,
                    new LanguageContributionId("counter.ambiguity.executor"),
                    target,
                    static (value, _) => value,
                    SafeTraits))
            .UseRouteRuntime("counter.ambiguity.runtime", "1")
            .Build();

        var result = Compile(package, "counter.ambiguity.core", backend);

        Assert.That(
            result.Plan,
            Is.Null,
            "Proposed safety invariant: equal-cost semantically different routes require an explicit policy instead of lexical ContributionId selection.");
    }

    [Test]
    public void Planner_ShouldReportImpossibleCrossContractPassOrder_AsPlanningFailure()
    {
        var earlyArtifact = new LanguageArtifactKind<int>("counter.order.early");
        var lateArtifact = new LanguageArtifactKind<long>("counter.order.late");
        var backend = new BackendId("counter.order.backend");
        var earlyPass = new LanguageContributionId("counter.order.early-pass");
        var latePass = new LanguageContributionId("counter.order.late-pass");
        var package = LanguagePackageBuilder.Create("Counter.CrossContractOrder", "1")
            .AddFeature("counter.order.core", feature => feature
                .AddTransformer(
                    "counter.order.parse",
                    new LanguageSlotId("counter.order.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    earlyArtifact,
                    static (_, _) => 1,
                    SafeTraits,
                    cost: 1)
                .AddPass(
                    earlyPass.Value,
                    LanguageSlots.Optimizers,
                    earlyArtifact,
                    static (value, _) => value + 1,
                    SafeTraits,
                    configure: contribution => contribution.After(latePass))
                .AddTransformer(
                    "counter.order.lower",
                    new LanguageSlotId("counter.order.routes"),
                    earlyArtifact,
                    lateArtifact,
                    static (value, _) => value,
                    SafeTraits,
                    cost: 1)
                .AddPass(
                    latePass.Value,
                    LanguageSlots.Optimizers,
                    lateArtifact,
                    static (value, _) => value + 1,
                    SafeTraits)
                .AddBackend(
                    backend,
                    new LanguageContributionId("counter.order.executor"),
                    lateArtifact,
                    static (value, _) => value,
                    SafeTraits))
            .UseRouteRuntime("counter.order.runtime", "1")
            .Build();

        LanguageBuildResult? result = null;
        Assert.DoesNotThrow(() => result = Compile(package, "counter.order.core", backend));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsSuccess, Is.False);
        Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Severity == LanguageDiagnosticSeverity.Error), Is.True);
    }

    [Test]
    public void Planner_ShouldNotUseContributionIdAsSemanticOrderForEqualOrderPasses()
    {
        var artifact = new LanguageArtifactKind<int>("counter.pass-tie.artifact");
        var backend = new BackendId("counter.pass-tie.backend");
        var package = LanguagePackageBuilder.Create("Counter.PassTie", "1")
            .AddFeature("counter.pass-tie.core", feature => feature
                .AddTransformer(
                    "counter.pass-tie.parse",
                    new LanguageSlotId("counter.pass-tie.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    artifact,
                    static (_, _) => 1,
                    SafeTraits,
                    cost: 1)
                .AddPass(
                    "counter.pass-tie.a-add",
                    LanguageSlots.Optimizers,
                    artifact,
                    static (value, _) => value + 1,
                    SafeTraits,
                    order: 0)
                .AddPass(
                    "counter.pass-tie.z-multiply",
                    LanguageSlots.Optimizers,
                    artifact,
                    static (value, _) => value * 2,
                    SafeTraits,
                    order: 0)
                .AddBackend(
                    backend,
                    new LanguageContributionId("counter.pass-tie.executor"),
                    artifact,
                    static (value, _) => value,
                    SafeTraits))
            .UseRouteRuntime("counter.pass-tie.runtime", "1")
            .Build();

        var result = Compile(package, "counter.pass-tie.core", backend);

        Assert.That(
            result.Plan,
            Is.Null,
            "Proposed safety invariant: equal-order non-commutative passes need explicit ordering/equivalence rather than lexical ContributionId priority.");
    }

    [Test]
    public void Planner_ShouldPreservePlan_WhenPackageRegistrationOrderChanges()
    {
        var artifact = new LanguageArtifactKind<int>("counter.permutation.artifact");
        var backend = new BackendId("counter.permutation.backend");
        var frontend = LanguagePackageBuilder.Create("Counter.Permutation.Frontend", "1")
            .AddFeature("counter.permutation.core", feature => feature.AddTransformer(
                "counter.permutation.parse",
                new LanguageSlotId("counter.permutation.routes"),
                StandardLanguageArtifactKinds.SourceText,
                artifact,
                static (source, _) => source.Length,
                SafeTraits,
                cost: 1))
            .Build();
        var execution = LanguagePackageBuilder.Create("Counter.Permutation.Execution", "1")
            .AddBackend(
                backend.Value,
                "counter.permutation.executor",
                artifact,
                static (value, _) => value,
                SafeTraits)
            .UseRouteRuntime("counter.permutation.runtime", "1")
            .Build();
        var definition = LanguageDefinitionBuilder.Create("Counter.Permutation.Language", "1")
            .UseFeature("counter.permutation.core")
            .EnableBackend(backend)
            .UseRuntimeProvider("counter.permutation.runtime", "1")
            .Build();

        var first = new LanguageCompiler(new LanguagePackageRegistry()
                .AddPackage(frontend)
                .AddPackage(execution))
            .Compile(definition)
            .GetRequiredPlan();
        var second = new LanguageCompiler(new LanguagePackageRegistry()
                .AddPackage(execution)
                .AddPackage(frontend))
            .Compile(definition)
            .GetRequiredPlan();

        Assert.Multiple(() =>
        {
            Assert.That(second.PlanHash, Is.EqualTo(first.PlanHash));
            Assert.That(
                second.Routes[backend].Steps.Select(static step => step.ContributionId),
                Is.EqualTo(first.Routes[backend].Steps.Select(static step => step.ContributionId)));
        });
    }

    private static LanguageBuildResult Compile(
        ILanguageFeaturePackage package,
        string featureId,
        BackendId backend)
    {
        var definition = LanguageDefinitionBuilder.Create($"{featureId}.language", "1")
            .UseFeature(featureId)
            .EnableBackend(backend)
            .Build();
        return new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(definition);
    }

    private static string FormatDiagnostics(LanguageBuildResult result) => string.Join(
        Environment.NewLine,
        result.Diagnostics.Select(static diagnostic => $"[{diagnostic.Code}] {diagnostic.Message}"));
}
