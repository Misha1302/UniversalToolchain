using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class PlannerRepairRegressionTests
{
    private static readonly LanguageRuntimeComponentTraits Traits =
        LanguageRuntimeComponentTraits.DeterministicNoHostInterop;

    [Test]
    public void DefinitionOrder_ReverseDirectionChangesRouteRuntimeHashAndLockCoherently()
    {
        var artifact = new LanguageArtifactKind<int>("repair.order.artifact");
        var backend = new BackendId("repair.order.backend");
        var add = new LanguageContributionId("repair.order.add");
        var multiply = new LanguageContributionId("repair.order.multiply");
        var package = CreateArithmeticPackage(artifact, backend, add, multiply);
        var compiler = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package));

        var multiplyThenAdd = compiler.Compile(LanguageDefinitionBuilder.Create("Repair.Order.Language", "1")
            .UseFeature("repair.order.core")
            .EnableBackend(backend)
            .OrderContributionBefore(multiply, add)
            .Build()).GetRequiredPlan();
        var addThenMultiply = compiler.Compile(LanguageDefinitionBuilder.Create("Repair.Order.Language", "1")
            .UseFeature("repair.order.core")
            .EnableBackend(backend)
            .OrderContributionBefore(add, multiply)
            .Build()).GetRequiredPlan();

        using var firstRuntime = LanguageRuntime.Create(
            multiplyThenAdd,
            new ILanguageRouteComponentSource[] { package });
        using var secondRuntime = LanguageRuntime.Create(
            addThenMultiply,
            new ILanguageRouteComponentSource[] { package });
        var firstValue = firstRuntime.Run(new LanguageExecutionRequest("ignored", backend)).Value;
        var secondValue = secondRuntime.Run(new LanguageExecutionRequest("ignored", backend)).Value;
        var firstLock = LanguageLockFile.Serialize(multiplyThenAdd);
        var secondLock = LanguageLockFile.Serialize(addThenMultiply);

        Assert.Multiple(() =>
        {
            Assert.That(RouteIndex(multiplyThenAdd, backend, multiply), Is.LessThan(RouteIndex(multiplyThenAdd, backend, add)));
            Assert.That(RouteIndex(addThenMultiply, backend, add), Is.LessThan(RouteIndex(addThenMultiply, backend, multiply)));
            Assert.That(firstValue, Is.EqualTo(3));
            Assert.That(secondValue, Is.EqualTo(4));
            Assert.That(addThenMultiply.PlanHash, Is.Not.EqualTo(multiplyThenAdd.PlanHash));
            Assert.That(secondLock, Is.Not.EqualTo(firstLock));
            Assert.That(LanguageLockFile.SerializeCanonical(addThenMultiply), Is.EqualTo(LanguageLockFile.SerializeCanonical(addThenMultiply)));
        });
    }

    [Test]
    public void DescriptorAndDefinitionOrder_ComposeIntoOneExecutablePartialOrder()
    {
        var artifact = new LanguageArtifactKind<int>("repair.compose.artifact");
        var backend = new BackendId("repair.compose.backend");
        var a = new LanguageContributionId("repair.compose.a");
        var b = new LanguageContributionId("repair.compose.b");
        var c = new LanguageContributionId("repair.compose.c");
        var package = LanguagePackageBuilder.Create("Repair.Compose", "1")
            .AddFeature("repair.compose.core", feature => feature
                .AddTransformer(
                    "repair.compose.parse",
                    new LanguageSlotId("repair.compose.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    artifact,
                    static (_, _) => 1,
                    Traits,
                    cost: 1)
                .AddPass(a.Value, LanguageSlots.Optimizers, artifact, static (value, _) => value + 1, Traits,
                    order: 30)
                .AddPass(b.Value, LanguageSlots.Optimizers, artifact, static (value, _) => value * 2, Traits,
                    order: 20, configure: contribution => contribution.After(a))
                .AddPass(c.Value, LanguageSlots.Optimizers, artifact, static (value, _) => value - 3, Traits,
                    order: 10)
                .AddBackend(backend, new LanguageContributionId("repair.compose.executor"), artifact,
                    static (value, _) => value, Traits))
            .UseRouteRuntime("repair.compose.runtime", "1")
            .Build();
        var definition = LanguageDefinitionBuilder.Create("Repair.Compose.Language", "1")
            .UseFeature("repair.compose.core")
            .EnableBackend(backend)
            .OrderContributionBefore(a, c)
            .Build();

        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();
        var route = plan.Routes[backend].Steps.Select(static step => step.ContributionId).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(route.IndexOf(a), Is.LessThan(route.IndexOf(c)));
            Assert.That(route.IndexOf(a), Is.LessThan(route.IndexOf(b)));
        });
    }

    [Test]
    public void ContradictoryDescriptorAndDefinitionOrder_ReturnsPlanningDiagnosticWithoutPlan()
    {
        var artifact = new LanguageArtifactKind<int>("repair.contradiction.artifact");
        var backend = new BackendId("repair.contradiction.backend");
        var a = new LanguageContributionId("repair.contradiction.a");
        var b = new LanguageContributionId("repair.contradiction.b");
        var package = LanguagePackageBuilder.Create("Repair.Contradiction", "1")
            .AddFeature("repair.contradiction.core", feature => feature
                .AddTransformer("repair.contradiction.parse", new LanguageSlotId("repair.contradiction.routes"),
                    StandardLanguageArtifactKinds.SourceText, artifact, static (_, _) => 1, Traits, cost: 1)
                .AddPass(a.Value, LanguageSlots.Optimizers, artifact, static (value, _) => value + 1, Traits,
                    configure: contribution => contribution.Before(b))
                .AddPass(b.Value, LanguageSlots.Optimizers, artifact, static (value, _) => value * 2, Traits)
                .AddBackend(backend, new LanguageContributionId("repair.contradiction.executor"), artifact,
                    static (value, _) => value, Traits))
            .UseRouteRuntime("repair.contradiction.runtime", "1")
            .Build();
        var definition = LanguageDefinitionBuilder.Create("Repair.Contradiction.Language", "1")
            .UseFeature("repair.contradiction.core")
            .EnableBackend(backend)
            .OrderContributionBefore(b, a)
            .Build();

        LanguageBuildResult? result = null;
        Assert.DoesNotThrow(() => result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Plan, Is.Null);
            Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Code), Does.Contain("UTL2202"));
        });
    }

    [Test]
    public void SingleMaximumCost_IsRepresentableWithoutOverflow()
    {
        var target = new LanguageArtifactKind<int>("repair.cost.single.target");
        var backend = new BackendId("repair.cost.single.backend");
        var package = LanguagePackageBuilder.Create("Repair.Cost.Single", "1")
            .AddFeature("repair.cost.single.core", feature => feature
                .AddTransformer("repair.cost.single.edge", new LanguageSlotId("repair.cost.single.routes"),
                    StandardLanguageArtifactKinds.SourceText, target, static (_, _) => 1, Traits,
                    cost: int.MaxValue)
                .AddBackend(backend, new LanguageContributionId("repair.cost.single.executor"), target,
                    static (value, _) => value, Traits))
            .UseRouteRuntime("repair.cost.single.runtime", "1")
            .Build();

        LanguagePlan? plan = null;
        Assert.DoesNotThrow(() => plan = Compile(package, "repair.cost.single.core", backend));
        Assert.That(plan!.Routes[backend].TotalCost, Is.EqualTo((long)int.MaxValue));
    }

    [Test]
    public void LongMaximumCostChain_UsesMathematicalAggregateAndRemainsDeterministic()
    {
        var first = new LanguageArtifactKind<int>("repair.cost.chain.first");
        var second = new LanguageArtifactKind<long>("repair.cost.chain.second");
        var target = new LanguageArtifactKind<double>("repair.cost.chain.target");
        var backend = new BackendId("repair.cost.chain.backend");
        var package = LanguagePackageBuilder.Create("Repair.Cost.Chain", "1")
            .AddFeature("repair.cost.chain.core", feature => feature
                .AddTransformer("repair.cost.chain.1", new LanguageSlotId("repair.cost.chain.routes"),
                    StandardLanguageArtifactKinds.SourceText, first, static (_, _) => 1, Traits,
                    cost: int.MaxValue)
                .AddTransformer("repair.cost.chain.2", new LanguageSlotId("repair.cost.chain.routes"),
                    first, second, static (value, _) => (long)value, Traits, cost: int.MaxValue)
                .AddTransformer("repair.cost.chain.3", new LanguageSlotId("repair.cost.chain.routes"),
                    second, target, static (value, _) => (double)value, Traits, cost: int.MaxValue)
                .AddBackend(backend, new LanguageContributionId("repair.cost.chain.executor"), target,
                    static (value, _) => value, Traits))
            .UseRouteRuntime("repair.cost.chain.runtime", "1")
            .Build();

        var firstPlan = Compile(package, "repair.cost.chain.core", backend);
        var secondPlan = Compile(package, "repair.cost.chain.core", backend);
        var expectedCost = 3L * int.MaxValue;

        Assert.Multiple(() =>
        {
            Assert.That(firstPlan.Routes[backend].TotalCost, Is.EqualTo(expectedCost));
            Assert.That(secondPlan.Routes[backend].Steps.Select(static step => step.ContributionId),
                Is.EqualTo(firstPlan.Routes[backend].Steps.Select(static step => step.ContributionId)));
            Assert.That(secondPlan.PlanHash, Is.EqualTo(firstPlan.PlanHash));
        });
    }

    private static AuthoredLanguagePackage CreateArithmeticPackage(
        LanguageArtifactKind<int> artifact,
        BackendId backend,
        LanguageContributionId add,
        LanguageContributionId multiply) =>
        LanguagePackageBuilder.Create("Repair.Order", "1")
            .AddFeature("repair.order.core", feature => feature
                .AddTransformer("repair.order.parse", new LanguageSlotId("repair.order.routes"),
                    StandardLanguageArtifactKinds.SourceText, artifact, static (_, _) => 1, Traits, cost: 1)
                .AddPass(add.Value, LanguageSlots.Optimizers, artifact, static (value, _) => value + 1, Traits,
                    order: 0)
                .AddPass(multiply.Value, LanguageSlots.Optimizers, artifact, static (value, _) => value * 2, Traits,
                    order: 0)
                .AddBackend(backend, new LanguageContributionId("repair.order.executor"), artifact,
                    static (value, _) => value, Traits))
            .UseRouteRuntime("repair.order.runtime", "1")
            .Build();

    private static LanguagePlan Compile(
        AuthoredLanguagePackage package,
        string feature,
        BackendId backend)
    {
        var definition = LanguageDefinitionBuilder.Create($"{feature}.language", "1")
            .UseFeature(feature)
            .EnableBackend(backend)
            .Build();
        return new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();
    }

    private static int RouteIndex(LanguagePlan plan, BackendId backend, LanguageContributionId contribution) =>
        plan.Routes[backend].Steps.Select(static step => step.ContributionId).ToList().IndexOf(contribution);
}
