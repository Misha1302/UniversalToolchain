using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Testing;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class ExternalLanguageAuthoringTests
{
    private static readonly LanguageCapabilityId WistFrontendCapability = new("frontend:wist");

    [Test]
    public void Compiler_ResolvesContributionsRoutesAndRuntimeProviderDeterministically()
    {
        var registry = Registry();
        var definition = Definition();
        var first = new LanguageCompiler(registry).Compile(definition);
        var second = new LanguageCompiler(registry).Compile(definition);

        Assert.Multiple(() =>
        {
            Assert.That(first.IsSuccess, Is.True, Format(first));
            Assert.That(second.IsSuccess, Is.True, Format(second));
            Assert.That(first.Plan!.PlanHash, Is.EqualTo(second.Plan!.PlanHash));
            Assert.That(LanguageLockFile.Serialize(first.Plan), Is.EqualTo(LanguageLockFile.Serialize(second.Plan)));
            Assert.That(first.Plan.RuntimeProvider!.ProviderId, Is.EqualTo(WistLanguageFeaturePackage.RuntimeProviderId));
            Assert.That(first.Plan.Contributions.Select(static x => x.Contribution.Id), Does.Contain(WistContributionIds.ArithmeticModule));
            Assert.That(first.Plan.Routes[new BackendId("interpreter")].Steps.Select(static x => x.ContributionId), Is.EqualTo(new[]
            {
                WistContributionIds.Frontend,
                WistContributionIds.LoweringToBytecode,
                WistContributionIds.LoweringToAir,
                WistContributionIds.InterpreterBackend
            }));
        });
    }

    [Test]
    public void Compiler_DoesNotRequireExplicitRuntimeProviderWhenSelectionIsUnique()
    {
        var result = new LanguageCompiler(Registry()).Compile(Definition());
        Assert.That(result.IsSuccess, Is.True, Format(result));
        Assert.That(result.Plan!.RuntimeProvider!.ProviderId, Is.EqualTo(WistLanguageFeaturePackage.RuntimeProviderId));
    }

    [Test]
    public void Compiler_RejectsMissingFeatureDependency()
    {
        var package = new TestPackage(new LanguagePackageDescriptor(
            new LanguagePackageId("Acme"),
            new LanguageVersion("1.0.0"),
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(new LanguageFeatureId("acme.main"), [new LanguageFeatureId("acme.missing")])],
            contributions: []));
        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(
            LanguageDefinitionBuilder.Create("Acme", "1.0.0")
                .UseFeature("acme.main")
                .EnableBackend("interpreter")
                .Build());
        Assert.That(result.Diagnostics.Any(static x => x.Code == "UTL1001"), Is.True);
    }

    [Test]
    public void Compiler_RejectsContributionCycle()
    {
        var a = new LanguageContributionId("cycle.a");
        var b = new LanguageContributionId("cycle.b");
        var descriptor = new LanguagePackageDescriptor(
            new LanguagePackageId("Cycle"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(new LanguageFeatureId("cycle.feature"), contributions: [a])],
            contributions:
            [
                new LanguageContributionDescriptor(a, LanguageSlots.Tooling, requiresContributions: [b]),
                new LanguageContributionDescriptor(b, LanguageSlots.Tooling, requiresContributions: [a])
            ]);
        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(new TestPackage(descriptor))).Compile(
            LanguageDefinitionBuilder.Create("Cycle", "1")
                .UseFeature("cycle.feature")
                .EnableBackend("interpreter")
                .Build());
        Assert.That(result.Diagnostics.Any(static x => x.Code == "UTL2003"), Is.True);
    }

    [Test]
    public void Compiler_RejectsAmbiguousCapabilityProviderAndAcceptsExplicitPreference()
    {
        var rootFeature = new LanguageFeatureId("acme.requires-formatting");
        var rootContribution = new LanguageContributionId("acme.root");
        var capability = new LanguageCapabilityId("tooling.formatter");
        var providerA = new LanguageContributionId("acme.formatter.a");
        var providerB = new LanguageContributionId("acme.formatter.b");
        var package = new TestPackage(new LanguagePackageDescriptor(
            new LanguagePackageId("Acme.Formatters"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(rootFeature, contributions: [rootContribution])],
            contributions:
            [
                new LanguageContributionDescriptor(rootContribution, LanguageSlots.Tooling, requiresCapabilities: [capability]),
                new LanguageContributionDescriptor(providerA, LanguageSlots.Tooling, providesCapabilities: [capability]),
                new LanguageContributionDescriptor(providerB, LanguageSlots.Tooling, providesCapabilities: [capability])
            ]));
        var registry = Registry().AddPackage(package);

        var ambiguous = new LanguageCompiler(registry).Compile(
            DefinitionBuilder().UseFeature(rootFeature).Build());
        var explicitResult = new LanguageCompiler(registry).Compile(
            DefinitionBuilder()
                .UseFeature(rootFeature)
                .PreferCapabilityProvider(capability, providerB)
                .Build());

        Assert.Multiple(() =>
        {
            Assert.That(ambiguous.Diagnostics.Any(static x => x.Code == "UTL2002"), Is.True);
            Assert.That(explicitResult.IsSuccess, Is.True, Format(explicitResult));
            Assert.That(explicitResult.Plan!.Contributions.Select(static x => x.Contribution.Id), Does.Contain(providerB));
            Assert.That(explicitResult.Plan.Contributions.Select(static x => x.Contribution.Id), Does.Not.Contain(providerA));
        });
    }

    [Test]
    public void Compiler_RequiresExplicitOverrideForSingleOwnerSlot()
    {
        var feature = new LanguageFeatureId("acme.alternative-frontend");
        var alternative = new LanguageContributionId("acme.frontend.parser");
        var package = new TestPackage(new LanguagePackageDescriptor(
            new LanguagePackageId("Acme.Frontend"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(feature, contributions: [alternative])],
            contributions:
            [
                new LanguageContributionDescriptor(
                    alternative,
                    LanguageSlots.FrontendParser,
                    LanguageSlotMultiplicity.Single,
                    ContributionMergePolicy.RejectDuplicate,
                    providesCapabilities: [WistFrontendCapability],
                    transformation: new ArtifactTransformationDescriptor(
                        StandardLanguageArtifactKinds.SourceText.Contract,
                        WistArtifactKinds.SyntaxTreeContract,
                        5))
            ]));
        var registry = Registry().AddPackage(package);

        var conflict = new LanguageCompiler(registry).Compile(
            DefinitionBuilder()
                .UseFeature(feature)
                .PreferCapabilityProvider(WistFrontendCapability, WistContributionIds.Frontend)
                .Build());
        var replaced = new LanguageCompiler(registry).Compile(
            DefinitionBuilder()
                .UseFeature(feature)
                .PreferCapabilityProvider(WistFrontendCapability, WistContributionIds.Frontend)
                .ReplaceSlot(LanguageSlots.FrontendParser, alternative, WistContributionIds.Frontend)
                .Build());

        Assert.Multiple(() =>
        {
            Assert.That(conflict.Diagnostics.Any(static x => x.Code == "UTL2101"), Is.True);
            Assert.That(replaced.IsSuccess, Is.True, Format(replaced));
            Assert.That(replaced.Plan!.Contributions.Select(static x => x.Contribution.Id), Does.Contain(alternative));
            Assert.That(replaced.Plan.Contributions.Select(static x => x.Contribution.Id), Does.Not.Contain(WistContributionIds.Frontend));
            Assert.That(replaced.Plan.Routes[new BackendId("cil")].Steps[0].ContributionId, Is.EqualTo(alternative));
        });
    }

    [Test]
    public void RoutePlanner_SelectsLowestCostPathDeterministically()
    {
        var ids = new
        {
            Feature = new LanguageFeatureId("route.language"),
            Cheap = new LanguageContributionId("route.a-cheap"),
            Expensive = new LanguageContributionId("route.b-expensive"),
            Backend = new LanguageContributionId("route.backend"),
            Runtime = new LanguageContributionId("route.runtime")
        };
        var middle = new LanguageArtifactKind<string>("route.middle", "route.middle.string/v1");
        var target = new LanguageArtifactKind<string>("route.target", "route.target.string/v1");
        var backend = new BackendId("route");
        var providerId = new LanguageRuntimeProviderId("route.provider");
        var package = new TestPackage(new LanguagePackageDescriptor(
            new LanguagePackageId("Route"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(ids.Feature, contributions: [ids.Cheap, ids.Expensive])],
            contributions:
            [
                new LanguageContributionDescriptor(ids.Cheap, LanguageSlots.FrontendSyntax,
                    transformation: ArtifactTransformationDescriptor.Create(StandardLanguageArtifactKinds.SourceText, middle, 1)),
                new LanguageContributionDescriptor(ids.Expensive, LanguageSlots.Tooling,
                    transformation: ArtifactTransformationDescriptor.Create(StandardLanguageArtifactKinds.SourceText, middle, 9)),
                new LanguageContributionDescriptor(ids.Backend, LanguageSlots.Backends,
                    providesCapabilities: [LanguageCapabilities.Backend(backend)],
                    supportedBackends: [backend],
                    transformation: ArtifactTransformationDescriptor.Create(middle, target, 1),
                    backendInputContract: target.Contract),
                new LanguageContributionDescriptor(ids.Runtime, LanguageSlots.RuntimeProvider,
                    LanguageSlotMultiplicity.Single, ContributionMergePolicy.RejectDuplicate,
                    runtimeProviderId: providerId,
                    runtimeProviderVersion: new LanguageVersion("1"),
                    runtimeInputContracts: new Dictionary<BackendId, LanguageArtifactContract> { [backend] = target.Contract })
            ]));
        var definition = LanguageDefinitionBuilder.Create("Route", "1")
            .UseFeature(ids.Feature)
            .EnableBackend(backend)
            .Build();
        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(definition);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True, Format(result));
            Assert.That(result.Plan!.Routes[backend].TotalCost, Is.EqualTo(2));
            Assert.That(result.Plan.Routes[backend].Steps.Select(static x => x.ContributionId),
                Is.EqualTo(new[] { ids.Cheap, ids.Backend }));
        });
    }

    [Test]
    public void Runtime_ExecutesThroughProviderRegistry()
    {
        var definition = LanguageDefinitionBuilder.Create("Acme.Pricing", "1.0.0")
            .UseFeature(WistFeatureIds.Arithmetic)
            .EnableBackend(new BackendId("interpreter"))
            .EnableBackend(new BackendId("cil"))
            .WithRuntimePolicy(new LanguageRuntimePolicy(MaximumSourceLength: 4096))
            .Build();
        var plan = new LanguageCompiler(Registry()).Compile(definition).GetRequiredPlan();
        var providers = new LanguageRuntimeProviderRegistry().AddProvider(new WistLanguageRuntimeProvider());
        using var runtime = LanguageRuntime.Create(plan, providers);
        var parity = LanguageContractSuite.RequireParity(
            runtime,
            "2 + 3 * 4",
            new BackendId("interpreter"),
            new BackendId("cil"));
        Assert.That(parity.FirstValue?.ToString(), Is.EqualTo("14"));
    }

    [Test]
    public void WistRuntime_FailsClosedWhenDeterminismEvidenceIsRequired()
    {
        var plan = new LanguageCompiler(Registry()).Compile(Definition()).GetRequiredPlan();
        var providers = new LanguageRuntimeProviderRegistry().AddProvider(new WistLanguageRuntimeProvider());

        var error = Assert.Throws<InvalidOperationException>(() => LanguageRuntime.Create(plan, providers));

        Assert.That(error!.Message, Does.Contain("determinism evidence"));
    }

    [Test]
    public void RouteRuntime_ExecutesExplicitFrontendReplacement()
    {
        var fixture = CreateExecutableRouteFixture();
        var definition = LanguageDefinitionBuilder.Create("Executable.Route", "1")
            .UseFeature(fixture.BaseFeature)
            .UseFeature(fixture.AlternativeFeature)
            .EnableBackend(fixture.Backend)
            .ReplaceSlot(LanguageSlots.FrontendParser, fixture.AlternativeFrontend, fixture.DefaultFrontend)
            .Build();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(fixture.Package))
            .Compile(definition)
            .GetRequiredPlan();
        var components = new LanguageRouteComponentRegistry()
            .AddTransformer(LanguageTransformerRegistration.FromStatelessSingleton(
                new StringArtifactTransformer(
                    fixture.AlternativeFrontend,
                    StandardLanguageArtifactKinds.SourceText,
                    fixture.SyntaxArtifact,
                    static value => $"alternative({value})")))
            .AddTransformer(LanguageTransformerRegistration.FromStatelessSingleton(
                new StringArtifactTransformer(
                    fixture.Lowering,
                    fixture.SyntaxArtifact,
                    fixture.ExecutableArtifact,
                    static value => $"lowered({value})")))
            .AddExecutor(LanguageExecutorRegistration.FromStatelessSingleton(
                new StringArtifactExecutor(
                    fixture.BackendContribution, fixture.Backend, fixture.ExecutableArtifact)));
        var provider = new LanguageRouteRuntimeProvider(
            fixture.ProviderId,
            fixture.ProviderVersion,
            ToolchainApi.Current,
            fixture.RuntimeContribution,
            components);

        using var runtime = LanguageRuntime.Create(plan, provider);
        var result = runtime.Run(new LanguageExecutionRequest("source", fixture.Backend));

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo("lowered(alternative(source))"));
            Assert.That(plan.Routes[fixture.Backend].Steps.Select(static x => x.ContributionId),
                Is.EqualTo(new[] { fixture.AlternativeFrontend, fixture.Lowering }));
            Assert.That(plan.Contributions.Select(static x => x.Contribution.Id), Does.Not.Contain(fixture.DefaultFrontend));
        });
    }

    [Test]
    public void RouteRuntime_RejectsMissingTransformerImplementationBeforeExecution()
    {
        var fixture = CreateExecutableRouteFixture();
        var definition = LanguageDefinitionBuilder.Create("Executable.Route", "1")
            .UseFeature(fixture.BaseFeature)
            .UseFeature(fixture.AlternativeFeature)
            .EnableBackend(fixture.Backend)
            .ReplaceSlot(LanguageSlots.FrontendParser, fixture.AlternativeFrontend, fixture.DefaultFrontend)
            .Build();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(fixture.Package))
            .Compile(definition)
            .GetRequiredPlan();
        var components = new LanguageRouteComponentRegistry()
            .AddTransformer(LanguageTransformerRegistration.FromStatelessSingleton(
                new StringArtifactTransformer(
                    fixture.AlternativeFrontend,
                    StandardLanguageArtifactKinds.SourceText,
                    fixture.SyntaxArtifact,
                    static value => value)))
            .AddExecutor(LanguageExecutorRegistration.FromStatelessSingleton(
                new StringArtifactExecutor(
                    fixture.BackendContribution, fixture.Backend, fixture.ExecutableArtifact)));
        var provider = new LanguageRouteRuntimeProvider(
            fixture.ProviderId,
            fixture.ProviderVersion,
            ToolchainApi.Current,
            fixture.RuntimeContribution,
            components);

        var exception = Assert.Throws<InvalidOperationException>(() => LanguageRuntime.Create(plan, provider));
        Assert.That(exception!.Message, Does.Contain(fixture.Lowering.Value));
    }

    [Test]
    public void RuntimeProviderRegistry_AllowsSideBySideVersionsAndResolvesExactly()
    {
        var first = new VersionedNoopProvider(new LanguageVersion("1.0.0"));
        var second = new VersionedNoopProvider(new LanguageVersion("2.0.0"));
        var registry = new LanguageRuntimeProviderRegistry()
            .AddProvider(first)
            .AddProvider(second);

        Assert.Multiple(() =>
        {
            Assert.That(
                registry.GetRequiredProvider(new LanguageRuntimeProviderReference(first.ProviderId, first.ProviderVersion)),
                Is.SameAs(first));
            Assert.That(
                registry.GetRequiredProvider(new LanguageRuntimeProviderReference(second.ProviderId, second.ProviderVersion)),
                Is.SameAs(second));
        });
    }

    [Test]
    public void Runtime_RejectsWrongProviderVersion()
    {
        var plan = new LanguageCompiler(Registry()).Compile(Definition()).GetRequiredPlan();
        var registry = new LanguageRuntimeProviderRegistry().AddProvider(new WrongVersionProvider());
        Assert.Throws<InvalidOperationException>(() => LanguageRuntime.Create(plan, registry));
    }

    [Test]
    public void Manifest_RoundTripsContributionsAndRoutesDeterministically()
    {
        var descriptor = new WistLanguageFeaturePackage().Descriptor;
        var first = LanguageFeatureManifestSerializer.Serialize(descriptor);
        var restored = LanguageFeatureManifestSerializer.Deserialize(first);
        var roundTrip = LanguageFeatureManifestSerializer.Serialize(restored);
        Assert.Multiple(() =>
        {
            Assert.That(roundTrip, Is.EqualTo(first));
            Assert.That(restored.Contributions.Count, Is.EqualTo(descriptor.Contributions.Count));
            Assert.That(restored.Contributions.Single(x => x.Id == WistContributionIds.CilBackend).Transformation, Is.Not.Null);
        });
    }

    [Test]
    public void Registry_AddPackageIsTransactionalForContributionConflicts()
    {
        var registry = Registry();
        var conflicting = new TestPackage(new LanguagePackageDescriptor(
            new LanguagePackageId("Other"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [],
            contributions: [new LanguageContributionDescriptor(WistContributionIds.Frontend, LanguageSlots.Tooling)]));
        Assert.Throws<InvalidOperationException>(() => registry.AddPackage(conflicting));
        Assert.That(registry.Packages.Count, Is.EqualTo(1));
    }

    [Test]
    public void GenericSdkProjectsDoNotReferenceWist()
    {
        var root = FindRepositoryRoot();
        var projects = new[]
        {
            "UniversalToolchain.Language.Abstractions",
            "UniversalToolchain.FeatureSdk",
            "UniversalToolchain.LanguageSdk",
            "UniversalToolchain.Runtime",
            "UniversalToolchain.LanguageAuthoring",
            "UniversalToolchain.Testing"
        };
        foreach (var project in projects)
        {
            var text = File.ReadAllText(Path.Combine(root, "UniversalToolchain", project, $"{project}.csproj"));
            Assert.That(text, Does.Not.Contain("Dialects.Wist"), project);
            Assert.That(text, Does.Not.Contain("UniversalToolchain.Wist.csproj"), project);
        }
    }

    private static ExecutableRouteFixture CreateExecutableRouteFixture()
    {
        var baseFeature = new LanguageFeatureId("route.base");
        var alternativeFeature = new LanguageFeatureId("route.alternative");
        var defaultFrontend = new LanguageContributionId("route.frontend.default");
        var alternativeFrontend = new LanguageContributionId("route.frontend.alternative");
        var lowering = new LanguageContributionId("route.lowering");
        var backendContribution = new LanguageContributionId("route.backend");
        var runtimeContribution = new LanguageContributionId("route.runtime");
        var syntaxArtifact = new LanguageArtifactKind<string>("route.syntax", "route.syntax.string/v1");
        var executableArtifact = new LanguageArtifactKind<string>("route.executable", "route.executable.string/v1");
        var backend = new BackendId("route");
        var providerId = new LanguageRuntimeProviderId("route.provider");
        var providerVersion = new LanguageVersion("1");
        var descriptor = new LanguagePackageDescriptor(
            new LanguagePackageId("Route.Executable"),
            providerVersion,
            ToolchainApi.Current,
            [
                new LanguageFeatureDescriptor(baseFeature, contributions: [defaultFrontend, lowering]),
                new LanguageFeatureDescriptor(alternativeFeature, contributions: [alternativeFrontend])
            ],
            contributions:
            [
                new LanguageContributionDescriptor(
                    defaultFrontend,
                    LanguageSlots.FrontendParser,
                    LanguageSlotMultiplicity.Single,
                    ContributionMergePolicy.RejectDuplicate,
                    transformation: ArtifactTransformationDescriptor.Create(StandardLanguageArtifactKinds.SourceText, syntaxArtifact, 5)),
                new LanguageContributionDescriptor(
                    alternativeFrontend,
                    LanguageSlots.FrontendParser,
                    LanguageSlotMultiplicity.Single,
                    ContributionMergePolicy.RejectDuplicate,
                    transformation: ArtifactTransformationDescriptor.Create(StandardLanguageArtifactKinds.SourceText, syntaxArtifact, 3)),
                new LanguageContributionDescriptor(
                    lowering,
                    LanguageSlots.Lowering,
                    LanguageSlotMultiplicity.Single,
                    ContributionMergePolicy.RejectDuplicate,
                    transformation: ArtifactTransformationDescriptor.Create(syntaxArtifact, executableArtifact, 4)),
                new LanguageContributionDescriptor(
                    backendContribution,
                    LanguageSlots.Backends,
                    providesCapabilities: [LanguageCapabilities.Backend(backend)],
                    supportedBackends: [backend],
                    backendInputContract: executableArtifact.Contract),
                new LanguageContributionDescriptor(
                    runtimeContribution,
                    LanguageSlots.RuntimeProvider,
                    LanguageSlotMultiplicity.Single,
                    ContributionMergePolicy.RejectDuplicate,
                    runtimeProviderId: providerId,
                    runtimeProviderVersion: providerVersion,
                    runtimeInputContracts: new Dictionary<BackendId, LanguageArtifactContract> { [backend] = executableArtifact.Contract })
            ]);
        return new ExecutableRouteFixture(
            new TestPackage(descriptor),
            baseFeature,
            alternativeFeature,
            defaultFrontend,
            alternativeFrontend,
            lowering,
            backendContribution,
            runtimeContribution,
            syntaxArtifact,
            executableArtifact,
            backend,
            providerId,
            providerVersion);
    }

    private sealed record ExecutableRouteFixture(
        TestPackage Package,
        LanguageFeatureId BaseFeature,
        LanguageFeatureId AlternativeFeature,
        LanguageContributionId DefaultFrontend,
        LanguageContributionId AlternativeFrontend,
        LanguageContributionId Lowering,
        LanguageContributionId BackendContribution,
        LanguageContributionId RuntimeContribution,
        LanguageArtifactKind<string> SyntaxArtifact,
        LanguageArtifactKind<string> ExecutableArtifact,
        BackendId Backend,
        LanguageRuntimeProviderId ProviderId,
        LanguageVersion ProviderVersion);

    private sealed class StringArtifactTransformer(
        LanguageContributionId contributionId,
        LanguageArtifactKind<string> sourceKind,
        LanguageArtifactKind<string> targetKind,
        Func<string, string> transform) : ILanguageArtifactTransformer<string, string>, IStatelessLanguageRuntimeComponent
    {
        public LanguageContributionId ContributionId => contributionId;
        public LanguageArtifactKind<string> TypedSourceKind => sourceKind;
        public LanguageArtifactKind<string> TypedTargetKind => targetKind;
        public LanguageRuntimeComponentTraits TypedTraits => LanguageRuntimeComponentTraits.DeterministicNoHostInterop;
        public string Transform(string source, LanguageArtifactTransformationContext context) => transform(source);
    }

    private sealed class StringArtifactExecutor(
        LanguageContributionId contributionId,
        BackendId backend,
        LanguageArtifactKind<string> inputKind) : ILanguageArtifactExecutor<string, string>, IStatelessLanguageRuntimeComponent
    {
        public LanguageContributionId ContributionId => contributionId;
        public BackendId Backend => backend;
        public LanguageArtifactKind<string> TypedInputKind => inputKind;
        public LanguageRuntimeComponentTraits TypedTraits => LanguageRuntimeComponentTraits.DeterministicNoHostInterop;
        public string Execute(string input, LanguageArtifactTransformationContext context) => input;
    }

    private static LanguagePackageRegistry Registry() =>
        new LanguagePackageRegistry().AddPackage(new WistLanguageFeaturePackage());

    private static LanguageDefinitionBuilder DefinitionBuilder() =>
        LanguageDefinitionBuilder.Create("Acme.Pricing", "1.0.0")
            .UseFeature(WistFeatureIds.Arithmetic)
            .EnableBackend(new BackendId("interpreter"))
            .EnableBackend(new BackendId("cil"))
            .WithRuntimePolicy(new LanguageRuntimePolicy(RequireDeterminism: true, MaximumSourceLength: 4096));

    private static LanguageDefinition Definition() => DefinitionBuilder().Build();

    private static string Format(LanguageBuildResult result) =>
        string.Join(Environment.NewLine, result.Diagnostics.Select(static x => $"[{x.Code}] {x.Message}"));

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "readme.md")))
            current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class TestPackage(LanguagePackageDescriptor descriptor) : ILanguageExtensionPackage
    {
        public LanguagePackageDescriptor Descriptor { get; } = descriptor;
    }

    private sealed class VersionedNoopProvider(LanguageVersion version) : ILanguageRuntimeProvider
    {
        public LanguageRuntimeProviderId ProviderId => new("versioned.provider");
        public LanguageVersion ProviderVersion => version;
        public ToolchainApiVersion ToolchainApiVersion => ToolchainApi.Current;
        public LanguageContributionId RuntimeContributionId => new("versioned.runtime");
        public IReadOnlyCollection<BackendId> SupportedBackends => [new BackendId("test")];
        public ILanguageRuntimeSession CreateSession(LanguagePlan plan, LanguageRuntimeOptions options) => throw new NotSupportedException();
    }

    private sealed class WrongVersionProvider : ILanguageRuntimeProvider
    {
        public LanguageRuntimeProviderId ProviderId => WistLanguageFeaturePackage.RuntimeProviderId;
        public LanguageVersion ProviderVersion => new("9.9.9");
        public ToolchainApiVersion ToolchainApiVersion => ToolchainApi.Current;
        public LanguageContributionId RuntimeContributionId => WistContributionIds.RuntimeProvider;
        public IReadOnlyCollection<BackendId> SupportedBackends => [new BackendId("cil"), new BackendId("interpreter")];
        public ILanguageRuntimeSession CreateSession(LanguagePlan plan, LanguageRuntimeOptions options) => throw new NotSupportedException();
    }
}
