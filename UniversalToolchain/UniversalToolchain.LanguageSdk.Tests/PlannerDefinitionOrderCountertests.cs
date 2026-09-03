using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class PlannerDefinitionOrderCountertests
{
    [Test]
    public void Planner_ShouldApplyDefinitionContributionOrder_ToExecutablePassRoute()
    {
        var traits = LanguageRuntimeComponentTraits.DeterministicNoHostInterop;
        var artifact = new LanguageArtifactKind<int>("counter.definition-order.artifact");
        var backend = new BackendId("counter.definition-order.backend");
        var add = new LanguageContributionId("counter.definition-order.a-add");
        var multiply = new LanguageContributionId("counter.definition-order.z-multiply");
        var package = LanguagePackageBuilder.Create("Counter.DefinitionOrder", "1")
            .AddFeature("counter.definition-order.core", feature => feature
                .AddTransformer(
                    "counter.definition-order.parse",
                    new LanguageSlotId("counter.definition-order.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    artifact,
                    static (_, _) => 1,
                    traits,
                    cost: 1)
                .AddPass(
                    add.Value,
                    LanguageSlots.Optimizers,
                    artifact,
                    static (value, _) => value + 1,
                    traits)
                .AddPass(
                    multiply.Value,
                    LanguageSlots.Optimizers,
                    artifact,
                    static (value, _) => value * 2,
                    traits)
                .AddBackend(
                    backend,
                    new LanguageContributionId("counter.definition-order.executor"),
                    artifact,
                    static (value, _) => value,
                    traits))
            .UseRouteRuntime("counter.definition-order.runtime", "1")
            .Build();
        var definition = LanguageDefinitionBuilder.Create("Counter.DefinitionOrder.Language", "1")
            .UseFeature("counter.definition-order.core")
            .EnableBackend(backend)
            .OrderContributionBefore(multiply, add)
            .Build();

        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();
        var contributionOrder = plan.Contributions
            .Select(static item => item.Contribution.Id)
            .ToList();
        var routeOrder = plan.Routes[backend].Steps
            .Select(static step => step.ContributionId)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(
                contributionOrder.IndexOf(multiply),
                Is.LessThan(contributionOrder.IndexOf(add)),
                "The definition-level planner records the requested contribution order.");
            Assert.That(
                routeOrder.IndexOf(multiply),
                Is.LessThan(routeOrder.IndexOf(add)),
                "The executable route must honor the same explicit definition-level order.");
        });
    }
}
