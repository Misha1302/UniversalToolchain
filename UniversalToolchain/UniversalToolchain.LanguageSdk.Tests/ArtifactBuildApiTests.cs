using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class ArtifactBuildApiTests
{
    private static readonly BackendId CompiledBackend = new("compiled-test");

    [Test]
    public void Build_DoesNotInvokeExecutorUntilExecuteBuilt()
    {
        var executions = 0;
        var executable = new LanguageArtifactKind<Func<int>>("build.executable");
        var package = LanguagePackageBuilder.Create("Build.Independent", "1")
            .AddFeature("build.core", feature => feature
                .AddTransformer(
                    "build.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    new LanguageArtifactKind<int>("build.syntax"),
                    static (source, _) => int.Parse(source),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1)
                .AddTransformer(
                    "build.compile",
                    LanguageSlots.Lowering,
                    new LanguageArtifactKind<int>("build.syntax"),
                    executable,
                    static (value, _) => () => value,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1,
                    supportedBackends: [CompiledBackend])
                .AddBackend(
                    CompiledBackend,
                    new LanguageContributionId("build.backend"),
                    executable,
                    (program, _) =>
                    {
                        executions++;
                        return program();
                    },
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("build.runtime", "1")
            .Build();
        var plan = Compile(package, "build.core", CompiledBackend);
        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });

        var built = runtime.Build(LanguageArtifactBuildRequest.FromText("21", CompiledBackend));

        Assert.That(executions, Is.Zero);
        var result = runtime.ExecuteBuilt(built);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(21));
            Assert.That(executions, Is.EqualTo(1));
        });
    }

    [Test]
    public void BuildResult_ContainsPlanBackendContractAndRouteMetadata()
    {
        var executable = new LanguageArtifactKind<string>("build.metadata.executable");
        var package = LanguagePackageBuilder.Create("Build.Metadata", "1")
            .AddFeature("build.metadata.core", feature => feature
                .AddTransformer(
                    "build.metadata.compile",
                    LanguageSlots.Lowering,
                    StandardLanguageArtifactKinds.SourceText,
                    executable,
                    static (source, _) => source.ToUpperInvariant(),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1,
                    supportedBackends: [CompiledBackend])
                .AddBackend(
                    CompiledBackend,
                    new LanguageContributionId("build.metadata.backend"),
                    executable,
                    static (program, _) => program,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("build.metadata.runtime", "1")
            .Build();
        var plan = Compile(package, "build.metadata.core", CompiledBackend);
        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });

        var built = runtime.Build(LanguageArtifactBuildRequest.FromText("abc", CompiledBackend));

        Assert.Multiple(() =>
        {
            Assert.That(built.LanguageId, Is.EqualTo(plan.Definition.Id));
            Assert.That(built.LanguageVersion, Is.EqualTo(plan.Definition.Version));
            Assert.That(built.PlanHash, Is.EqualTo(plan.PlanHash));
            Assert.That(built.Backend, Is.EqualTo(CompiledBackend));
            Assert.That(built.ArtifactContract, Is.EqualTo(executable.Contract));
            Assert.That(built.Lifetime, Is.EqualTo(LanguageBuiltArtifactLifetime.OriginatingRuntime));
            Assert.That(built.Steps.Select(static step => step.ContributionId.Value),
                Is.EqualTo(new[] { "build.metadata.compile" }));
            Assert.That(built.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void BuildAwareTransformer_ReceivesDeclaredTypesSeparatelyFromRuntimeValues()
    {
        var package = BuildAwarePackage.Create();
        var plan = Compile(package, BuildAwarePackage.FeatureId.Value, BuildAwarePackage.Backend);
        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
        var request = LanguageArtifactBuildRequest.FromText(
            "ignored",
            BuildAwarePackage.Backend,
            [LanguageBuildBinding.Create("value", typeof(object), "sample")]);

        var built = runtime.Build(request);
        var value = runtime.GetBuiltArtifactValue(built, BuildAwarePackage.OutputKind);

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(typeof(object).AssemblyQualifiedName));
            Assert.That(request.DeclaredBindingTypes["value"], Is.EqualTo(typeof(object)));
            Assert.That(request.RuntimeArguments["value"], Is.EqualTo("sample"));
        });
    }

    [Test]
    public void ExecuteBuilt_RejectsArtifactFromDifferentRuntime()
    {
        var package = BuildAwarePackage.Create();
        var plan = Compile(package, BuildAwarePackage.FeatureId.Value, BuildAwarePackage.Backend);
        using var first = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
        using var second = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
        var built = first.Build(LanguageArtifactBuildRequest.FromText("x", BuildAwarePackage.Backend));

        var error = Assert.Throws<InvalidOperationException>(() => second.ExecuteBuilt(built));

        Assert.That(error!.Message, Does.Contain("different language runtime/build session"));
    }

    [Test]
    public void BuiltArtifact_CannotBeUsedThroughDisposedOriginatingRuntime()
    {
        var package = BuildAwarePackage.Create();
        var plan = Compile(package, BuildAwarePackage.FeatureId.Value, BuildAwarePackage.Backend);
        var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
        var built = runtime.Build(LanguageArtifactBuildRequest.FromText("x", BuildAwarePackage.Backend));

        runtime.Dispose();

        Assert.Multiple(() =>
        {
            Assert.Throws<ObjectDisposedException>(() => runtime.ExecuteBuilt(built));
            Assert.Throws<ObjectDisposedException>(() => runtime.GetBuiltArtifactValue(built, BuildAwarePackage.OutputKind));
        });
    }

    [Test]
    public void GetBuiltArtifactValue_RejectsContractMismatch()
    {
        var package = BuildAwarePackage.Create();
        var plan = Compile(package, BuildAwarePackage.FeatureId.Value, BuildAwarePackage.Backend);
        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
        var built = runtime.Build(LanguageArtifactBuildRequest.FromText("x", BuildAwarePackage.Backend));
        var wrongKind = new LanguageArtifactKind<string>("build.wrong-output");

        var error = Assert.Throws<InvalidOperationException>(() => runtime.GetBuiltArtifactValue(built, wrongKind));

        Assert.That(error!.Message, Does.Contain("does not match requested contract"));
    }

    [Test]
    public void BuildRequest_RejectsDuplicateOrInvalidBindings()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => new LanguageArtifactBuildRequest(
                new LanguageArtifact<string>(StandardLanguageArtifactKinds.SourceText, "x"),
                CompiledBackend,
                [LanguageBuildBinding.Declare<int>("x"), LanguageBuildBinding.Declare<string>("x")]));
            Assert.Throws<ArgumentException>(() => LanguageBuildBinding.Create("x", typeof(int), null));
            Assert.Throws<ArgumentException>(() => LanguageBuildBinding.Create("x", typeof(int), "not-an-int"));
        });
    }

    [Test]
    public void WistCilArtifact_BuildsWithoutExecution()
    {
        var cil = new BackendId("cil");
        var package = new WistLanguageFeaturePackage();
        var definition = LanguageDefinitionBuilder
            .Create("wist.build-only.cil", WistLanguageFeaturePackage.PackageVersion.Value)
            .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
            .UseFeature(WistFeatureIds.Arithmetic)
            .UseFeature(WistSsaPolicyFeatureIds.Disabled)
            .EnableBackend(cil)
            .Build();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();
        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });

        var built = runtime.Build(LanguageArtifactBuildRequest.FromText("2 + 3", cil));

        Assert.Multiple(() =>
        {
            Assert.That(built.Backend, Is.EqualTo(cil));
            Assert.That(built.ArtifactContract, Is.EqualTo(WistArtifactKinds.CilArtifactContract));
            Assert.That(built.Steps[^1].ContributionId, Is.EqualTo(WistContributionIds.CilBackend));
        });
    }

    private static LanguagePlan Compile(
        ILanguageExtensionPackage package,
        string feature,
        BackendId backend) =>
        new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(LanguageDefinitionBuilder.Create("Build.Test", "1")
                .UseFeature(feature)
                .EnableBackend(backend)
                .Build())
            .GetRequiredPlan();

    private sealed class BuildAwarePackage : ILanguageExtensionPackage, ILanguageRouteComponentSource
    {
        public static readonly LanguageFeatureId FeatureId = new("build-aware.feature");
        public static readonly BackendId Backend = new("build-aware");
        public static readonly LanguageArtifactKind<string> OutputKind = new("build-aware.output");
        private static readonly LanguageContributionId ParseId = new("build-aware.parse");
        private static readonly LanguageContributionId BackendId = new("build-aware.backend");
        private static readonly LanguageContributionId RuntimeId = new("build-aware.runtime");
        private static readonly LanguageRuntimeProviderId ProviderId = new("build-aware.provider");
        private static readonly LanguageVersion Version = new("1");

        private BuildAwarePackage()
        {
            Descriptor = new LanguagePackageDescriptor(
                new LanguagePackageId("Build.Aware"),
                Version,
                ToolchainApi.Current,
                [new LanguageFeatureDescriptor(FeatureId, contributions: [ParseId, BackendId])],
                contributions:
                [
                    new LanguageContributionDescriptor(
                        ParseId,
                        LanguageSlots.FrontendParser,
                        transformation: ArtifactTransformationDescriptor.Create(
                            StandardLanguageArtifactKinds.SourceText,
                            OutputKind,
                            1)),
                    new LanguageContributionDescriptor(
                        BackendId,
                        LanguageSlots.Backends,
                        providesCapabilities: [LanguageCapabilities.Backend(Backend)],
                        supportedBackends: [Backend]),
                    new LanguageContributionDescriptor(
                        RuntimeId,
                        LanguageSlots.RuntimeProvider,
                        LanguageSlotMultiplicity.Single,
                        ContributionMergePolicy.RejectDuplicate,
                        runtimeProviderId: ProviderId,
                        runtimeProviderVersion: Version,
                        runtimeInputContracts: new Dictionary<BackendId, LanguageArtifactContract>
                        {
                            [Backend] = OutputKind.Contract
                        })
                ]);
            Components = new LanguageRouteComponentRegistry()
                .AddTransformer(LanguageTransformerRegistration.Create<string, string>(
                    ParseId,
                    StandardLanguageArtifactKinds.SourceText,
                    OutputKind,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    _ => new BuildAwareTransformer()))
                .AddExecutor(LanguageExecutorRegistration.Create<string, string>(
                    BackendId,
                    Backend,
                    OutputKind,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    _ => new DelegateLanguageArtifactExecutor<string, string>(
                        BackendId,
                        Backend,
                        OutputKind,
                        static (value, _) => value,
                        LanguageRuntimeComponentTraits.DeterministicNoHostInterop)))
                .CreateCatalog();
        }

        public LanguagePackageDescriptor Descriptor { get; }
        public LanguageRouteComponentCatalog Components { get; }

        public static BuildAwarePackage Create() => new();

        private sealed class BuildAwareTransformer : ILanguageArtifactTransformer<string, string>, ILanguageArtifactBuildTransformer
        {
            public LanguageContributionId ContributionId => ParseId;
            public LanguageArtifactKind<string> TypedSourceKind => StandardLanguageArtifactKinds.SourceText;
            public LanguageArtifactKind<string> TypedTargetKind => OutputKind;
            public LanguageRuntimeComponentTraits TypedTraits => LanguageRuntimeComponentTraits.DeterministicNoHostInterop;

            public string Transform(string source, LanguageArtifactTransformationContext context) => "ordinary-runtime";

            public LanguageArtifact TransformForBuild(LanguageArtifact source, LanguageArtifactBuildContext context)
            {
                _ = source.GetRequiredValue<string>();
                var declaredType = context.Request.DeclaredBindingTypes.TryGetValue("value", out var type)
                    ? type
                    : typeof(void);
                return new LanguageArtifact<string>(OutputKind, declaredType.AssemblyQualifiedName!);
            }
        }
    }
}
