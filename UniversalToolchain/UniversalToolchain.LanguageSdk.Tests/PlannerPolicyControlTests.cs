using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class PlannerPolicyControlTests
{
    private static readonly LanguageRuntimeComponentTraits Traits =
        LanguageRuntimeComponentTraits.DeterministicNoHostInterop;

    [Test]
    public void Planner_ShouldPreservePlanAndExecution_WhenIrrelevantPackageIsAdded()
    {
        var artifact = new LanguageArtifactKind<int>("control.irrelevant.artifact");
        var backend = new BackendId("control.irrelevant.backend");
        var package = CreateExecutablePackage("Control.Irrelevant", "control.irrelevant.core", artifact, backend);
        var irrelevant = LanguagePackageBuilder.Create("Control.Irrelevant.Unused", "1")
            .AddFeature("control.irrelevant.unused", feature => feature
                .AddTransformer(
                    "control.irrelevant.unused.parse",
                    new LanguageSlotId("control.irrelevant.unused.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    new LanguageArtifactKind<long>("control.irrelevant.unused.artifact"),
                    static (_, _) => 99L,
                    Traits,
                    cost: 1))
            .Build();
        var definition = LanguageDefinitionBuilder.Create("Control.Irrelevant.Language", "1")
            .UseFeature("control.irrelevant.core")
            .EnableBackend(backend)
            .Build();

        var baseline = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();
        var augmented = new LanguageCompiler(new LanguagePackageRegistry()
                .AddPackage(package)
                .AddPackage(irrelevant))
            .Compile(definition)
            .GetRequiredPlan();
        using var baselineRuntime = LanguageRuntime.Create(
            baseline,
            new ILanguageRouteComponentSource[] { package });
        using var augmentedRuntime = LanguageRuntime.Create(
            augmented,
            new ILanguageRouteComponentSource[] { package, irrelevant });

        Assert.Multiple(() =>
        {
            Assert.That(augmented.PlanHash, Is.EqualTo(baseline.PlanHash));
            Assert.That(RouteIds(augmented, backend), Is.EqualTo(RouteIds(baseline, backend)));
            Assert.That(
                augmentedRuntime.Run(new LanguageExecutionRequest("ignored", backend)).Value,
                Is.EqualTo(baselineRuntime.Run(new LanguageExecutionRequest("ignored", backend)).Value));
        });
    }

    [Test]
    public void Planner_ShouldPreservePlanAndExecution_WhenAuthoredInputOrderChanges()
    {
        var artifact = new LanguageArtifactKind<int>("control.input-order.artifact");
        var backend = new BackendId("control.input-order.backend");
        var parse = new LanguageContributionId("control.input-order.parse");
        var execute = new LanguageContributionId("control.input-order.execute");
        var forward = CreatePermutedPackage(artifact, backend, parse, execute, reverse: false);
        var reverse = CreatePermutedPackage(artifact, backend, parse, execute, reverse: true);
        var definition = LanguageDefinitionBuilder.Create("Control.InputOrder.Language", "1")
            .UseFeature("control.input-order.core")
            .EnableBackend(backend)
            .Build();

        var forwardPlan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(forward))
            .Compile(definition)
            .GetRequiredPlan();
        var reversePlan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(reverse))
            .Compile(definition)
            .GetRequiredPlan();
        using var forwardRuntime = LanguageRuntime.Create(
            forwardPlan,
            new ILanguageRouteComponentSource[] { forward });
        using var reverseRuntime = LanguageRuntime.Create(
            reversePlan,
            new ILanguageRouteComponentSource[] { reverse });

        Assert.Multiple(() =>
        {
            Assert.That(reversePlan.PlanHash, Is.EqualTo(forwardPlan.PlanHash));
            Assert.That(RouteIds(reversePlan, backend), Is.EqualTo(RouteIds(forwardPlan, backend)));
            Assert.That(
                reverseRuntime.Run(new LanguageExecutionRequest("ignored", backend)).Value,
                Is.EqualTo(forwardRuntime.Run(new LanguageExecutionRequest("ignored", backend)).Value));
        });
    }

    [Test]
    public void RuntimeProviderReachability_DoesNotResolveProviderAmbiguityImplicitly()
    {
        var backend = new BackendId("control.provider.backend");
        var reachable = new LanguageArtifactKind<int>("control.provider.reachable");
        var unreachable = new LanguageArtifactKind<long>("control.provider.unreachable");
        var feature = new LanguageFeatureId("control.provider.core");
        var parse = new LanguageContributionId("control.provider.parse");
        var executor = new LanguageContributionId("control.provider.executor");
        var providerA = new LanguageContributionId("control.provider.runtime-a");
        var providerB = new LanguageContributionId("control.provider.runtime-b");
        var version = new LanguageVersion("1");
        var mainDescriptor = new LanguagePackageDescriptor(
            new LanguagePackageId("Control.Provider.Main"),
            version,
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(feature, contributions: [parse, executor])],
            contributions:
            [
                new LanguageContributionDescriptor(
                    parse,
                    new LanguageSlotId("control.provider.routes"),
                    transformation: ArtifactTransformationDescriptor.Create(
                        StandardLanguageArtifactKinds.SourceText,
                        reachable,
                        1)),
                new LanguageContributionDescriptor(
                    executor,
                    LanguageSlots.Backends,
                    providesCapabilities: [LanguageCapabilities.Backend(backend)],
                    supportedBackends: [backend])
            ]);
        var providerADescriptor = RuntimeProviderPackage(
            "Control.Provider.A",
            providerA,
            "control.provider.a",
            version,
            backend,
            unreachable.Contract);
        var providerBDescriptor = RuntimeProviderPackage(
            "Control.Provider.B",
            providerB,
            "control.provider.b",
            version,
            backend,
            reachable.Contract);
        var registry = new LanguagePackageRegistry()
            .AddPackage(new DescriptorPackage(mainDescriptor))
            .AddPackage(new DescriptorPackage(providerADescriptor))
            .AddPackage(new DescriptorPackage(providerBDescriptor));
        var definition = LanguageDefinitionBuilder.Create("Control.Provider.Language", "1")
            .UseFeature(feature)
            .EnableBackend(backend)
            .Build();

        var result = new LanguageCompiler(registry).Compile(definition);

        Assert.Multiple(() =>
        {
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Code), Does.Contain("UTL2302"));
        });
    }

    [Test]
    public void RouteSearch_ShouldChooseMoreExpensiveRoute_WhenCheaperRouteViolatesDescriptorOrder()
    {
        var backend = new BackendId("control.order.backend");
        var middle = new LanguageArtifactKind<int>("control.order.middle");
        var executable = new LanguageArtifactKind<int>("control.order.executable");
        var feature = new LanguageFeatureId("control.order.core");
        var cheapParse = new LanguageContributionId("control.order.cheap.parse");
        var cheapLower = new LanguageContributionId("control.order.cheap.lower");
        var direct = new LanguageContributionId("control.order.direct");
        var executor = new LanguageContributionId("control.order.executor");
        var version = new LanguageVersion("1");
        var mainDescriptor = new LanguagePackageDescriptor(
            new LanguagePackageId("Control.Order.Main"),
            version,
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(
                feature,
                contributions: [cheapParse, cheapLower, direct, executor])],
            contributions:
            [
                new LanguageContributionDescriptor(
                    cheapParse,
                    new LanguageSlotId("control.order.cheap-parse"),
                    transformation: ArtifactTransformationDescriptor.Create(
                        StandardLanguageArtifactKinds.SourceText,
                        middle,
                        1),
                    afterContributions: [cheapLower]),
                new LanguageContributionDescriptor(
                    cheapLower,
                    new LanguageSlotId("control.order.cheap-lower"),
                    transformation: ArtifactTransformationDescriptor.Create(
                        middle,
                        executable,
                        1)),
                new LanguageContributionDescriptor(
                    direct,
                    new LanguageSlotId("control.order.direct-route"),
                    transformation: ArtifactTransformationDescriptor.Create(
                        StandardLanguageArtifactKinds.SourceText,
                        executable,
                        10)),
                new LanguageContributionDescriptor(
                    executor,
                    LanguageSlots.Backends,
                    providesCapabilities: [LanguageCapabilities.Backend(backend)],
                    supportedBackends: [backend],
                    backendInputContract: executable.Contract)
            ]);
        var providerDescriptor = RuntimeProviderPackage(
            "Control.Order.Provider",
            new LanguageContributionId("control.order.runtime"),
            "control.order.runtime",
            version,
            backend,
            executable.Contract);
        var registry = new LanguagePackageRegistry()
            .AddPackage(new DescriptorPackage(mainDescriptor))
            .AddPackage(new DescriptorPackage(providerDescriptor));
        var definition = LanguageDefinitionBuilder.Create("Control.Order.Language", "1")
            .UseFeature(feature)
            .EnableBackend(backend)
            .Build();

        var result = new LanguageCompiler(registry).Compile(definition);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(RouteIds(result.GetRequiredPlan(), backend), Is.EqualTo(new[] { direct }));
        });
    }

    private static AuthoredLanguagePackage CreateExecutablePackage(
        string packageId,
        string featureId,
        LanguageArtifactKind<int> artifact,
        BackendId backend) =>
        LanguagePackageBuilder.Create(packageId, "1")
            .AddFeature(featureId, feature => feature
                .AddTransformer(
                    featureId + ".parse",
                    new LanguageSlotId(featureId + ".routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    artifact,
                    static (_, _) => 7,
                    Traits,
                    cost: 1)
                .AddBackend(
                    backend,
                    new LanguageContributionId(featureId + ".execute"),
                    artifact,
                    static (value, _) => value,
                    Traits))
            .UseRouteRuntime(featureId + ".runtime", "1")
            .Build();

    private static AuthoredLanguagePackage CreatePermutedPackage(
        LanguageArtifactKind<int> artifact,
        BackendId backend,
        LanguageContributionId parse,
        LanguageContributionId execute,
        bool reverse)
    {
        var builder = LanguagePackageBuilder.Create("Control.InputOrder", "1");
        builder.AddFeature("control.input-order.core", feature =>
        {
            if (reverse)
            {
                feature.AddBackend(backend, execute, artifact, static (value, _) => value, Traits);
                feature.AddTransformer(
                    parse.Value,
                    new LanguageSlotId("control.input-order.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    artifact,
                    static (_, _) => 11,
                    Traits,
                    cost: 1);
            }
            else
            {
                feature.AddTransformer(
                    parse.Value,
                    new LanguageSlotId("control.input-order.routes"),
                    StandardLanguageArtifactKinds.SourceText,
                    artifact,
                    static (_, _) => 11,
                    Traits,
                    cost: 1);
                feature.AddBackend(backend, execute, artifact, static (value, _) => value, Traits);
            }
        });
        return builder.UseRouteRuntime("control.input-order.runtime", "1").Build();
    }

    private static LanguagePackageDescriptor RuntimeProviderPackage(
        string packageId,
        LanguageContributionId contributionId,
        string providerId,
        LanguageVersion version,
        BackendId backend,
        LanguageArtifactContract input) =>
        new(
            new LanguagePackageId(packageId),
            version,
            ToolchainApi.Current,
            [],
            contributions:
            [
                new LanguageContributionDescriptor(
                    contributionId,
                    LanguageSlots.RuntimeProvider,
                    LanguageSlotMultiplicity.Single,
                    ContributionMergePolicy.RejectDuplicate,
                    providesCapabilities: [LanguageCapabilities.RuntimeProvider],
                    runtimeProviderId: new LanguageRuntimeProviderId(providerId),
                    runtimeProviderVersion: version,
                    runtimeInputContracts: new Dictionary<BackendId, LanguageArtifactContract>
                    {
                        [backend] = input
                    })
            ]);

    private static IReadOnlyList<LanguageContributionId> RouteIds(LanguagePlan plan, BackendId backend) =>
        plan.Routes[backend].Steps.Select(static step => step.ContributionId).ToArray();

    private sealed class DescriptorPackage(LanguagePackageDescriptor descriptor) : ILanguageExtensionPackage
    {
        public LanguagePackageDescriptor Descriptor { get; } = descriptor;
    }
}
