using System.Xml.Linq;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class LanguageArchitectureRegressionTests
{
    [Test]
    public void CrossPackageRoute_ExecutesComponentsFromAllSelectedPackages()
    {
        var syntax = new LanguageArtifactKind<int>("cross.syntax");
        var backend = new BackendId("cross");
        var frontend = LanguagePackageBuilder.Create("Cross.Frontend", "1")
            .AddFeature("cross.frontend", feature => feature.AddTransformer(
                "cross.parse",
                LanguageSlots.FrontendParser,
                StandardLanguageArtifactKinds.SourceText,
                syntax,
                static (source, _) => int.Parse(source),
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                cost: 1))
            .Build();
        var execution = LanguagePackageBuilder.Create("Cross.Execution", "1")
            .AddBackend(
                backend.Value,
                "cross.backend",
                syntax,
                static (value, _) => value + 1,
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
            .UseRouteRuntime("cross.runtime", "1")
            .Build();
        var registry = new LanguagePackageRegistry()
            .AddPackage(frontend)
            .AddPackage(execution);
        var plan = new LanguageCompiler(registry).Compile(
            LanguageDefinitionBuilder.Create("Cross.Language", "1")
                .UseFeature("cross.frontend")
                .EnableBackend(backend)
                .UseRuntimeProvider("cross.runtime", "1")
                .Build()).GetRequiredPlan();

        using var runtime = LanguageRuntime.Create(
            plan,
            new ILanguageRouteComponentSource[] { frontend, execution });
        var result = runtime.Run(new LanguageExecutionRequest("41", backend));

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void WistProvider_RejectsUnsupportedCustomRoute()
    {
        var customSyntax = new LanguageArtifactKind<string>(
            WistArtifactKinds.SyntaxTree,
            WistArtifactKinds.SyntaxTreeContract.ValueTypeIdentity!);
        var custom = LanguagePackageBuilder.Create("Custom.Wist.Frontend", "1")
            .AddFeature("custom.wist.frontend", feature => feature.AddTransformer(
                "custom.wist.parse",
                LanguageSlots.FrontendParser,
                StandardLanguageArtifactKinds.SourceText,
                customSyntax,
                static (source, _) => source,
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                cost: 1,
                configure: contribution => contribution.ProvidesCapabilities(new LanguageCapabilityId("frontend:wist"))))
            .Build();
        var wist = new WistLanguageFeaturePackage();
        var backend = new BackendId("interpreter");
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(wist).AddPackage(custom)).Compile(
            LanguageDefinitionBuilder.Create("Custom.Wist", "1")
                .UseFeature(WistFeatureIds.Arithmetic)
                .UseFeature("custom.wist.frontend")
                .EnableBackend(backend)
                .PreferCapabilityProvider(
                    new LanguageCapabilityId("frontend:wist"),
                    new LanguageContributionId("custom.wist.parse"))
                .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
                .Build()).GetRequiredPlan();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(plan, new WistLanguageRuntimeProvider()));

        Assert.That(exception!.Message, Does.Contain("cannot execute custom artifact route"));
    }

    [Test]
    public void SameArtifactPasses_AreOrderedAndExecuted()
    {
        var syntax = new LanguageArtifactKind<int>("passes.syntax");
        var backend = new BackendId("passes");
        var add = new LanguageContributionId("passes.add");
        var multiply = new LanguageContributionId("passes.multiply");
        var package = LanguagePackageBuilder.Create("Passes.Language", "1")
            .AddFeature("passes.core", feature => feature
                .AddTransformer(
                    "passes.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    syntax,
                    static (source, _) => int.Parse(source),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1)
                .AddPass(
                    add.Value,
                    LanguageSlots.Optimizers,
                    syntax,
                    static (value, _) => value + 1,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    order: 100,
                    configure: contribution => contribution.Before(multiply))
                .AddPass(
                    multiply.Value,
                    LanguageSlots.Optimizers,
                    syntax,
                    static (value, _) => value * 2,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    order: -100)
                .AddBackend(
                    backend,
                    new LanguageContributionId("passes.backend"),
                    syntax,
                    static (value, _) => value,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("passes.runtime", "1")
            .Build();
        var plan = CompileSinglePackage(package, "passes.core", backend);

        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
        var result = runtime.Run(new LanguageExecutionRequest("2", backend));

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(6));
            Assert.That(
                plan.Routes[backend].Steps.Select(static step => step.ContributionId),
                Does.Contain(add).And.Contain(multiply));
            Assert.That(
                plan.Routes[backend].Steps.ToList().FindIndex(step => step.ContributionId == add),
                Is.LessThan(plan.Routes[backend].Steps.ToList().FindIndex(step => step.ContributionId == multiply)));
        });
    }

    [Test]
    public void PackageVersion_MatchesDescriptorProviderAndProjectVersion()
    {
        var projectPath = Path.Combine(
            FindRepositoryRoot(),
            "UniversalToolchain",
            "UniversalToolchain.Wist.LanguagePack",
            "UniversalToolchain.Wist.LanguagePack.csproj");
        var projectVersion = XDocument.Load(projectPath).Descendants("Version").Single().Value;
        var package = new WistLanguageFeaturePackage();
        var provider = new WistLanguageRuntimeProvider();
        var manifest = LanguageFeatureManifestSerializer.Deserialize(
            LanguageFeatureManifestSerializer.Serialize(package.Descriptor));

        Assert.Multiple(() =>
        {
            Assert.That(WistLanguageFeaturePackage.PackageVersion.Value, Is.EqualTo(projectVersion));
            Assert.That(provider.ProviderVersion.Value, Is.EqualTo(projectVersion));
            Assert.That(package.Descriptor.Version.Value, Is.EqualTo(projectVersion));
            Assert.That(manifest.Version.Value, Is.EqualTo(projectVersion));
        });
    }

    [Test]
    public void CapabilityResolution_CannotBypassFeatureConflict()
    {
        var syntax = new LanguageArtifactKind<int>("conflict.syntax");
        var backend = new BackendId("conflict");
        var package = LanguagePackageBuilder.Create("Conflict.Language", "1")
            .AddFeature("conflict.core", feature => feature.AddTransformer(
                "conflict.parse",
                LanguageSlots.FrontendParser,
                StandardLanguageArtifactKinds.SourceText,
                syntax,
                static (source, _) => int.Parse(source),
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .AddFeature("conflict.backend-feature", feature => feature
                .ConflictsWith(new LanguageFeatureId("conflict.core"))
                .AddBackend(
                    backend,
                    new LanguageContributionId("conflict.backend"),
                    syntax,
                    static (value, _) => value,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("conflict.runtime", "1")
            .Build();

        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(
            LanguageDefinitionBuilder.Create("Conflict.Language", "1")
                .UseFeature("conflict.core")
                .EnableBackend(backend)
                .Build());

        Assert.Multiple(() =>
        {
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "UTL2004"), Is.True);
        });
    }

    [Test]
    public void AlternativeExecutors_SelectExactPlannedContribution()
    {
        var syntax = new LanguageArtifactKind<int>("alternative.syntax");
        var backend = new BackendId("alternative");
        var first = new LanguageContributionId("alternative.first");
        var second = new LanguageContributionId("alternative.second");
        var package = LanguagePackageBuilder.Create("Alternative.Language", "1")
            .AddFeature("alternative.core", feature => feature.AddTransformer(
                "alternative.parse",
                LanguageSlots.FrontendParser,
                StandardLanguageArtifactKinds.SourceText,
                syntax,
                static (source, _) => int.Parse(source),
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .AddBackend(
                backend.Value,
                first.Value,
                syntax,
                static (value, _) => value + 10,
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
            .AddBackend(
                backend.Value,
                second.Value,
                syntax,
                static (value, _) => value + 20,
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
            .UseRouteRuntime("alternative.runtime", "1")
            .Build();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(
            LanguageDefinitionBuilder.Create("Alternative.Language", "1")
                .UseFeature("alternative.core")
                .EnableBackend(backend)
                .PreferCapabilityProvider(LanguageCapabilities.Backend(backend), second)
                .Build()).GetRequiredPlan();

        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
        var result = runtime.Run(new LanguageExecutionRequest("1", backend));

        Assert.That(result.Value, Is.EqualTo(21));
    }

    [Test]
    public void WhitespaceSource_ReachesLanguageFrontend()
    {
        var syntax = new LanguageArtifactKind<int>("whitespace.syntax");
        var backend = new BackendId("whitespace");
        var package = LanguagePackageBuilder.Create("Whitespace.Language", "1")
            .AddFeature("whitespace.core", feature => feature
                .AddTransformer(
                    "whitespace.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    syntax,
                    static (source, _) => source.Length,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
                .AddBackend(
                    backend,
                    new LanguageContributionId("whitespace.backend"),
                    syntax,
                    static (value, _) => value,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("whitespace.runtime", "1")
            .Build();
        var plan = CompileSinglePackage(package, "whitespace.core", backend);

        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
        var result = runtime.Run(new LanguageExecutionRequest("   ", backend));

        Assert.That(result.Value, Is.EqualTo(3));
    }

    [Test]
    public void RuntimePolicy_RejectsNegativeLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LanguageRuntimePolicy(MaximumSourceLength: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LanguageRuntimePolicy(MaximumExternalParameters: -1));
        });
    }

    [Test]
    public void ArtifactKindEquality_IncludesExplicitContractIdentity()
    {
        var first = new LanguageArtifactKind<int>("identity.syntax", "identity/v1");
        var second = new LanguageArtifactKind<int>("identity.syntax", "identity/v2");

        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    public void GenericArtifactIdentity_ExcludesAssemblyVersions()
    {
        var identity = LanguageTypeIdentity.For<Dictionary<string, int>>();

        Assert.Multiple(() =>
        {
            Assert.That(identity, Does.Contain("Dictionary`2"));
            Assert.That(identity, Does.Not.Contain("Version="));
            Assert.That(identity, Does.Not.Contain("Culture="));
            Assert.That(identity, Does.Not.Contain("PublicKeyToken="));
        });
    }

    [Test]
    public void ManifestV4_RoundTripsPassOrderingAndBackendInput()
    {
        var syntax = new LanguageArtifactKind<int>("manifest-v4.syntax");
        var pass = new LanguageContributionId("manifest-v4.pass");
        var other = new LanguageContributionId("manifest-v4.other");
        var backend = new BackendId("manifest-v4");
        var descriptor = new LanguagePackageDescriptor(
            new LanguagePackageId("Manifest.V4"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(new LanguageFeatureId("manifest-v4.core"), contributions: [pass])],
            contributions:
            [
                new LanguageContributionDescriptor(
                    pass,
                    LanguageSlots.Optimizers,
                    mergePolicy: ContributionMergePolicy.Decorate,
                    transformation: ArtifactTransformationDescriptor.Create(syntax, syntax, 0),
                    beforeContributions: [other]),
                new LanguageContributionDescriptor(
                    other,
                    LanguageSlots.Backends,
                    providesCapabilities: [LanguageCapabilities.Backend(backend)],
                    supportedBackends: [backend],
                    backendInputContract: syntax.Contract)
            ]);

        var json = LanguageFeatureManifestSerializer.Serialize(descriptor);
        var restored = LanguageFeatureManifestSerializer.Deserialize(json);
        var restoredPass = restored.Contributions.Single(contribution => contribution.Id == pass);
        var restoredBackend = restored.Contributions.Single(contribution => contribution.Id == other);

        Assert.Multiple(() =>
        {
            Assert.That(LanguageFeatureManifestSerializer.SchemaVersion, Is.EqualTo(5));
            Assert.That(restoredPass.Transformation!.IsPass, Is.True);
            Assert.That(restoredPass.BeforeContributions, Is.EqualTo(new[] { other }));
            Assert.That(restoredBackend.BackendInputContract, Is.EqualTo(syntax.Contract));
        });
    }

    [Test]
    public void RuntimeAssembler_RejectsComponentSourceWithDifferentManifest()
    {
        var syntax = new LanguageArtifactKind<int>("binding.syntax");
        var backend = new BackendId("binding");
        var package = LanguagePackageBuilder.Create("Binding.Language", "1")
            .AddFeature("binding.core", feature => feature
                .AddTransformer(
                    "binding.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    syntax,
                    static (source, _) => int.Parse(source),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
                .AddBackend(
                    backend,
                    new LanguageContributionId("binding.backend"),
                    syntax,
                    static (value, _) => value,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("binding.runtime", "1")
            .Build();
        var plan = CompileSinglePackage(package, "binding.core", backend);
        var tamperedDescriptor = new LanguagePackageDescriptor(
            package.Descriptor.Id,
            package.Descriptor.Version,
            package.Descriptor.ToolchainApiVersion,
            package.Descriptor.Features,
            new Dictionary<string, string> { ["tampered"] = "true" },
            package.Descriptor.Contributions);
        var tampered = new ComponentSource(tamperedDescriptor, package.Components);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { tampered }));

        Assert.That(exception!.Message, Does.Contain("exact package manifest"));
    }

    [Test]
    public void RuntimeAssembler_RequiresRuntimeProviderPackageSource()
    {
        var syntax = new LanguageArtifactKind<int>("provider-source.syntax");
        var backend = new BackendId("provider-source");
        var frontend = LanguagePackageBuilder.Create("ProviderSource.Frontend", "1")
            .AddFeature("provider-source.frontend", feature => feature.AddTransformer(
                "provider-source.parse",
                LanguageSlots.FrontendParser,
                StandardLanguageArtifactKinds.SourceText,
                syntax,
                static (source, _) => int.Parse(source),
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .Build();
        var execution = LanguagePackageBuilder.Create("ProviderSource.Execution", "1")
            .AddBackend(
                backend.Value,
                "provider-source.backend",
                syntax,
                static (value, _) => value,
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
            .UseRouteRuntime("provider-source.runtime", "1")
            .Build();
        var plan = new LanguageCompiler(new LanguagePackageRegistry()
                .AddPackage(frontend)
                .AddPackage(execution))
            .Compile(LanguageDefinitionBuilder.Create("ProviderSource.Language", "1")
                .UseFeature("provider-source.frontend")
                .EnableBackend(backend)
                .UseRuntimeProvider("provider-source.runtime", "1")
                .Build())
            .GetRequiredPlan();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { frontend }));

        Assert.That(exception!.Message, Does.Contain("No runtime component source was supplied"));
    }

    [Test]
    public void ComponentRegistry_AllowsOneContributionToImplementDifferentBackends()
    {
        var contribution = new LanguageContributionId("multi-backend.execute");
        var input = new LanguageArtifactKind<int>("multi-backend.syntax");
        var traits = LanguageRuntimeComponentTraits.DeterministicNoHostInterop;
        var interpreter = new BackendId("interpreter");
        var compiled = new BackendId("compiled");
        var registry = new LanguageRouteComponentRegistry()
            .AddExecutor(LanguageExecutorRegistration.Create<int, int>(
                contribution,
                interpreter,
                input,
                traits,
                _ => new DelegateLanguageArtifactExecutor<int, int>(
                    contribution,
                    interpreter,
                    input,
                    static (value, _) => value,
                    traits)))
            .AddExecutor(LanguageExecutorRegistration.Create<int, int>(
                contribution,
                compiled,
                input,
                traits,
                _ => new DelegateLanguageArtifactExecutor<int, int>(
                    contribution,
                    compiled,
                    input,
                    static (value, _) => value,
                    traits)));

        Assert.That(registry.CreateCatalog().Executors, Has.Count.EqualTo(2));
    }

    [Test]
    public void SelectedPassThatCannotBePlaced_FailsPlanning()
    {
        var syntax = new LanguageArtifactKind<int>("unplaced.syntax");
        var unrelated = new LanguageArtifactKind<int>("unplaced.unrelated");
        var backend = new BackendId("unplaced");
        var package = LanguagePackageBuilder.Create("Unplaced.Language", "1")
            .AddFeature("unplaced.core", feature => feature
                .AddTransformer(
                    "unplaced.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    syntax,
                    static (source, _) => int.Parse(source),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
                .AddPass(
                    "unplaced.pass",
                    LanguageSlots.Optimizers,
                    unrelated,
                    static (value, _) => value,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
                .AddBackend(
                    backend,
                    new LanguageContributionId("unplaced.backend"),
                    syntax,
                    static (value, _) => value,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("unplaced.runtime", "1")
            .Build();

        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(
            LanguageDefinitionBuilder.Create("Unplaced.Language", "1")
                .UseFeature("unplaced.core")
                .EnableBackend(backend)
                .UseRuntimeProvider("unplaced.runtime", "1")
                .Build());

        Assert.Multiple(() =>
        {
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "UTL2204"), Is.True);
        });
    }

    [Test]
    public void FeaturelessPackageLevelLanguage_CompilesAndRuns()
    {
        var syntax = new LanguageArtifactKind<int>("featureless.syntax");
        var backend = new BackendId("featureless");
        var package = LanguagePackageBuilder.Create("Featureless.Language", "1")
            .AddTransformer(
                "featureless.parse",
                LanguageSlots.FrontendParser,
                StandardLanguageArtifactKinds.SourceText,
                syntax,
                static (source, _) => int.Parse(source),
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                configure: contribution => contribution.ProvidesCapabilities(new LanguageCapabilityId("featureless.frontend")))
            .AddBackend(
                backend.Value,
                "featureless.backend",
                syntax,
                static (value, _) => value + 1,
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                configure: contribution => contribution.RequiresCapabilities(new LanguageCapabilityId("featureless.frontend")))
            .UseRouteRuntime("featureless.runtime", "1")
            .Build();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(
            LanguageDefinitionBuilder.Create("Featureless.Language", "1")
                .EnableBackend(backend)
                .UseRuntimeProvider("featureless.runtime", "1")
                .Build()).GetRequiredPlan();

        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
        var result = runtime.Run(new LanguageExecutionRequest("41", backend));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Features, Is.Empty);
            Assert.That(result.Value, Is.EqualTo(42));
        });
    }

    private static LanguagePlan CompileSinglePackage(
        AuthoredLanguagePackage package,
        string feature,
        BackendId backend) =>
        new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(
            LanguageDefinitionBuilder.Create(package.PackageId.Value, package.PackageVersion.Value)
                .UseFeature(feature)
                .EnableBackend(backend)
                .UseRuntimeProvider(package.RuntimeProvider!.ProviderId, package.RuntimeProvider.Version)
                .Build()).GetRequiredPlan();

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "readme.md")))
            current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }


    private sealed class ComponentSource(
        LanguagePackageDescriptor descriptor,
        LanguageRouteComponentCatalog components) : ILanguageRouteComponentSource
    {
        public LanguagePackageDescriptor Descriptor { get; } = descriptor;
        public LanguageRouteComponentCatalog Components { get; } = components;
    }

    private sealed class EmptySession : ILanguageRuntimeSession
    {
        public LanguageExecutionResult Run(LanguageExecutionRequest request) => new(request.Backend, null);
        public void Dispose()
        {
        }
    }
}
