using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class PlannerRouteMetamorphicTests
{
    private static readonly LanguageRuntimeComponentTraits Traits =
        LanguageRuntimeComponentTraits.DeterministicNoHostInterop;

    [Test]
    public void Planner_ShouldPreserveChosenRoute_WhenStrictlyDominatedAlternativeIsAdded()
    {
        var target = new LanguageArtifactKind<int>("meta.dominated.target");
        var middle = new LanguageArtifactKind<long>("meta.dominated.middle");
        var backend = new BackendId("meta.dominated.backend");
        var direct = new LanguageContributionId("meta.dominated.direct");

        var baseline = LanguagePackageBuilder.Create("Meta.Dominated.Baseline", "1")
            .AddFeature("meta.dominated.core", feature => feature
                .AddTransformer(
                    direct.Value,
                    new LanguageSlotId("meta.dominated.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    target,
                    static (_, _) => 1,
                    Traits,
                    cost: 2)
                .AddBackend(
                    backend,
                    new LanguageContributionId("meta.dominated.executor"),
                    target,
                    static (value, _) => value,
                    Traits))
            .UseRouteRuntime("meta.dominated.runtime", "1")
            .Build();
        var augmented = LanguagePackageBuilder.Create("Meta.Dominated.Augmented", "1")
            .AddFeature("meta.dominated.core", feature => feature
                .AddTransformer(
                    direct.Value,
                    new LanguageSlotId("meta.dominated.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    target,
                    static (_, _) => 1,
                    Traits,
                    cost: 2)
                .AddTransformer(
                    "meta.dominated.slow.first",
                    new LanguageSlotId("meta.dominated.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    middle,
                    static (_, _) => 1L,
                    Traits,
                    cost: 10)
                .AddTransformer(
                    "meta.dominated.slow.second",
                    new LanguageSlotId("meta.dominated.routes"),
                    middle,
                    target,
                    static (_, _) => 1,
                    Traits,
                    cost: 10)
                .AddBackend(
                    backend,
                    new LanguageContributionId("meta.dominated.executor"),
                    target,
                    static (value, _) => value,
                    Traits))
            .UseRouteRuntime("meta.dominated.runtime", "1")
            .Build();

        Assert.That(RouteIds(Compile(baseline, backend)), Is.EqualTo(RouteIds(Compile(augmented, backend))));
        Assert.That(RouteIds(Compile(augmented, backend)), Does.Contain(direct));
    }

    [Test]
    public void Planner_ShouldPreserveChosenRoute_WhenUnreachableAlternativeIsAdded()
    {
        var target = new LanguageArtifactKind<int>("meta.unreachable.target");
        var isolated = new LanguageArtifactKind<long>("meta.unreachable.isolated");
        var isolatedTarget = new LanguageArtifactKind<double>("meta.unreachable.isolated-target");
        var backend = new BackendId("meta.unreachable.backend");
        var direct = new LanguageContributionId("meta.unreachable.direct");
        var package = LanguagePackageBuilder.Create("Meta.Unreachable", "1")
            .AddFeature("meta.unreachable.core", feature => feature
                .AddTransformer(
                    direct.Value,
                    new LanguageSlotId("meta.unreachable.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    target,
                    static (_, _) => 1,
                    Traits,
                    cost: 3)
                .AddTransformer(
                    "meta.unreachable.edge",
                    new LanguageSlotId("meta.unreachable.routes"),
                    isolated,
                    isolatedTarget,
                    static (_, _) => 1.0,
                    Traits,
                    cost: 1)
                .AddBackend(
                    backend,
                    new LanguageContributionId("meta.unreachable.executor"),
                    target,
                    static (value, _) => value,
                    Traits))
            .UseRouteRuntime("meta.unreachable.runtime", "1")
            .Build();

        var route = RouteIds(Compile(package, backend));

        Assert.That(route, Does.Contain(direct));
        Assert.That(route, Does.Not.Contain(new LanguageContributionId("meta.unreachable.edge")));
    }

    private static LanguagePlan Compile(ILanguageFeaturePackage package, BackendId backend)
    {
        var definition = LanguageDefinitionBuilder.Create("meta.language", "1")
            .UseFeature(package.Descriptor.Features.Single().Id)
            .EnableBackend(backend)
            .Build();
        return new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();
    }

    private static IReadOnlyList<LanguageContributionId> RouteIds(LanguagePlan plan) =>
        plan.Routes.Values.Single().Steps.Select(static step => step.ContributionId).ToArray();
}
