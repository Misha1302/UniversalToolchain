using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class TypedLanguageAuthoringTests
{
    [Test]
    public void Compiler_RejectsClrTypeMismatchWhilePlanningRoute()
    {
        var source = new LanguageArtifactKind<string>("typed.source");
        var syntaxAsObject = new LanguageArtifactKind<object>("typed.syntax");
        var syntaxAsString = new LanguageArtifactKind<string>("typed.syntax");
        var executable = new LanguageArtifactKind<string>("typed.executable");
        var feature = new LanguageFeatureId("typed.feature");
        var parse = new LanguageContributionId("typed.parse");
        var lower = new LanguageContributionId("typed.lower");
        var backendContribution = new LanguageContributionId("typed.backend");
        var runtimeContribution = new LanguageContributionId("typed.runtime");
        var backend = new BackendId("typed");
        var provider = new LanguageRuntimeProviderId("typed.provider");
        var descriptor = new LanguagePackageDescriptor(
            new LanguagePackageId("Typed.Mismatch"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(feature, contributions: [parse, lower, backendContribution])],
            contributions:
            [
                new LanguageContributionDescriptor(parse, LanguageSlots.FrontendParser,
                    transformation: ArtifactTransformationDescriptor.Create(source, syntaxAsObject, 1)),
                new LanguageContributionDescriptor(lower, LanguageSlots.Lowering,
                    transformation: ArtifactTransformationDescriptor.Create(syntaxAsString, executable, 1)),
                new LanguageContributionDescriptor(backendContribution, LanguageSlots.Backends,
                    providesCapabilities: [LanguageCapabilities.Backend(backend)], supportedBackends: [backend]),
                new LanguageContributionDescriptor(runtimeContribution, LanguageSlots.RuntimeProvider,
                    LanguageSlotMultiplicity.Single, ContributionMergePolicy.RejectDuplicate,
                    runtimeProviderId: provider, runtimeProviderVersion: new LanguageVersion("1"),
                    runtimeInputContracts: new Dictionary<BackendId, LanguageArtifactContract>
                    {
                        [backend] = executable.Contract
                    })
            ]);

        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(new TestPackage(descriptor))).Compile(
            LanguageDefinitionBuilder.Create("Typed.Mismatch", "1")
                .WithEntryArtifact(source)
                .UseFeature(feature)
                .EnableBackend(backend)
                .Build());

        Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "UTL2201"), Is.True);
        Assert.That(result.Plan, Is.Null);
    }

    [Test]
    public void AuthoringBuilder_CreatesAndExecutesNonWistLanguageFromOneRegistrationSource()
    {
        var tokens = new LanguageArtifactKind<int[]>("numbers.tokens");
        var backend = new BackendId("sum");
        var package = LanguagePackageBuilder.Create("Numbers.Language", "1.0.0")
            .AddFeature("numbers.core", feature => feature
                .AddTransformer(
                    "numbers.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    tokens,
                    static (source, _) => source.Split(',', StringSplitOptions.TrimEntries).Select(int.Parse).ToArray(),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1)
                .AddBackend(
                    backend,
                    new LanguageContributionId("numbers.sum"),
                    tokens,
                    static (values, _) => values.Sum(),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("numbers.runtime", "1.0.0")
            .Build();
        var definition = LanguageDefinitionBuilder.Create("Numbers.Language", "1.0.0")
            .UseFeature("numbers.core")
            .EnableBackend(backend)
            .Build();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();

        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
        var result = runtime.Run(new LanguageExecutionRequest("1, 2, 3", backend));

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(6));
            Assert.That(plan.Routes[backend].SourceContract, Is.EqualTo(StandardLanguageArtifactKinds.SourceText.Contract));
            Assert.That(plan.Routes[backend].TargetContract, Is.EqualTo(tokens.Contract));
        });
    }

    [Test]
    public void Runtime_AcceptsCustomTypedEntryArtifact()
    {
        var document = new LanguageArtifactKind<NumberDocument>("number.document");
        var normalized = new LanguageArtifactKind<int>("number.normalized");
        var backend = new BackendId("number");
        var package = LanguagePackageBuilder.Create("Number.Document.Language", "1")
            .AddFeature("number.core", feature => feature
                .AddTransformer(
                    "number.normalize",
                    LanguageSlots.Lowering,
                    document,
                    normalized,
                    static (input, _) => input.Value,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1)
                .AddBackend(
                    backend,
                    new LanguageContributionId("number.backend"),
                    normalized,
                    static (value, _) => value * 2,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("number.runtime", "1")
            .Build();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(
            LanguageDefinitionBuilder.Create("Number.Document.Language", "1")
                .WithEntryArtifact(document)
                .UseFeature("number.core")
                .EnableBackend(backend)
                .Build()).GetRequiredPlan();

        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
        var result = runtime.Run(LanguageExecutionRequest.FromArtifact(document, new NumberDocument(21), backend));

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void Compiler_AllowsPlanningOnlyLanguageAndRuntimeRejectsExecution()
    {
        var contribution = new LanguageContributionId("tooling.formatter");
        var feature = new LanguageFeatureId("tooling.feature");
        var descriptor = new LanguagePackageDescriptor(
            new LanguagePackageId("Tooling.Language"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(feature, contributions: [contribution])],
            contributions: [new LanguageContributionDescriptor(contribution, LanguageSlots.Tooling)]);
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(new TestPackage(descriptor))).Compile(
            LanguageDefinitionBuilder.Create("Tooling.Language", "1")
                .UseFeature(feature)
                .Build()).GetRequiredPlan();

        Assert.Multiple(() =>
        {
            Assert.That(plan.IsExecutable, Is.False);
            Assert.That(plan.RuntimeProvider, Is.Null);
            Assert.That(plan.Routes, Is.Empty);
            Assert.Throws<InvalidOperationException>(() => LanguageRuntime.Create(plan, new LanguageRuntimeProviderRegistry()));
        });
    }

    [Test]
    public void ManifestV5_RoundTripPreservesTypedArtifactContracts()
    {
        var syntax = new LanguageArtifactKind<NumberDocument>("manifest.syntax");
        var executable = new LanguageArtifactKind<int>("manifest.executable");
        var backend = new BackendId("manifest");
        var contribution = new LanguageContributionId("manifest.lower");
        var runtime = new LanguageContributionId("manifest.runtime");
        var descriptor = new LanguagePackageDescriptor(
            new LanguagePackageId("Manifest.Typed"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(new LanguageFeatureId("manifest.feature"), contributions: [contribution])],
            contributions:
            [
                new LanguageContributionDescriptor(contribution, LanguageSlots.Lowering,
                    transformation: ArtifactTransformationDescriptor.Create(syntax, executable, 1)),
                new LanguageContributionDescriptor(runtime, LanguageSlots.RuntimeProvider,
                    LanguageSlotMultiplicity.Single, ContributionMergePolicy.RejectDuplicate,
                    runtimeProviderId: new LanguageRuntimeProviderId("manifest.provider"),
                    runtimeProviderVersion: new LanguageVersion("1"),
                    runtimeInputContracts: new Dictionary<BackendId, LanguageArtifactContract> { [backend] = executable.Contract })
            ]);

        var json = LanguageFeatureManifestSerializer.Serialize(descriptor);
        var restored = LanguageFeatureManifestSerializer.Deserialize(json);

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"schemaVersion\": 5"));
            Assert.That(restored.Contributions.Single(x => x.Id == contribution).Transformation!.SourceContract,
                Is.EqualTo(syntax.Contract));
            Assert.That(restored.Contributions.Single(x => x.Id == runtime).RuntimeInputContracts[backend],
                Is.EqualTo(executable.Contract));
        });
    }

    [Test]
    public void Runtime_RejectsExecutorThatDoesNotImplementSelectedBackendContribution()
    {
        var syntax = new LanguageArtifactKind<int>("executor.syntax");
        var feature = new LanguageFeatureId("executor.feature");
        var parse = new LanguageContributionId("executor.parse");
        var selectedBackend = new LanguageContributionId("executor.backend.selected");
        var wrongBackend = new LanguageContributionId("executor.backend.wrong");
        var runtimeContribution = new LanguageContributionId("executor.runtime");
        var backend = new BackendId("executor");
        var providerId = new LanguageRuntimeProviderId("executor.provider");
        var version = new LanguageVersion("1");
        var descriptor = new LanguagePackageDescriptor(
            new LanguagePackageId("Executor.Identity"),
            version,
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(feature, contributions: [parse, selectedBackend])],
            contributions:
            [
                new LanguageContributionDescriptor(
                    parse,
                    LanguageSlots.FrontendParser,
                    transformation: ArtifactTransformationDescriptor.Create(
                        StandardLanguageArtifactKinds.SourceText, syntax, 1)),
                new LanguageContributionDescriptor(
                    selectedBackend,
                    LanguageSlots.Backends,
                    providesCapabilities: [LanguageCapabilities.Backend(backend)],
                    supportedBackends: [backend]),
                new LanguageContributionDescriptor(
                    runtimeContribution,
                    LanguageSlots.RuntimeProvider,
                    LanguageSlotMultiplicity.Single,
                    ContributionMergePolicy.RejectDuplicate,
                    runtimeProviderId: providerId,
                    runtimeProviderVersion: version,
                    runtimeInputContracts: new Dictionary<BackendId, LanguageArtifactContract>
                    {
                        [backend] = syntax.Contract
                    })
            ]);
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(new TestPackage(descriptor))).Compile(
            LanguageDefinitionBuilder.Create("Executor.Identity", "1")
                .UseFeature(feature)
                .EnableBackend(backend)
                .UseRuntimeProvider(providerId, version)
                .Build()).GetRequiredPlan();
        var traits = LanguageRuntimeComponentTraits.DeterministicNoHostInterop;
        var components = new LanguageRouteComponentRegistry()
            .AddTransformer(LanguageTransformerRegistration.Create(
                parse,
                StandardLanguageArtifactKinds.SourceText,
                syntax,
                traits,
                _ => new DelegateLanguageArtifactTransformer<string, int>(
                    parse,
                    StandardLanguageArtifactKinds.SourceText,
                    syntax,
                    static (source, _) => int.Parse(source),
                    traits)))
            .AddExecutor(LanguageExecutorRegistration.Create<int, int>(
                wrongBackend,
                backend,
                syntax,
                traits,
                _ => new DelegateLanguageArtifactExecutor<int, int>(
                    wrongBackend,
                    backend,
                    syntax,
                    static (value, _) => value,
                    traits)));
        var provider = new LanguageRouteRuntimeProvider(
            providerId, version, ToolchainApi.Current, runtimeContribution, components);

        var exception = Assert.Throws<InvalidOperationException>(() => LanguageRuntime.Create(plan, provider));

        Assert.That(exception!.Message, Does.Contain(selectedBackend.Value));
    }

    [Test]
    public void Runtime_FailsClosedWhenRestrictedPolicyComponentTraitsAreUnknown()
    {
        var syntax = new LanguageArtifactKind<int>("policy.syntax");
        var backend = new BackendId("policy");
        var package = LanguagePackageBuilder.Create("Policy.Language", "1")
            .AddFeature("policy.feature", feature => feature
                .AddTransformer(
                    "policy.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    syntax,
                    static (source, _) => int.Parse(source),
                    LanguageRuntimeComponentTraits.Unknown,
                    cost: 1)
                .AddBackend(
                    backend,
                    new LanguageContributionId("policy.backend"),
                    syntax,
                    static (value, _) => value,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("policy.runtime", "1")
            .Build();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(
            LanguageDefinitionBuilder.Create("Policy.Language", "1")
                .UseFeature("policy.feature")
                .EnableBackend(backend)
                .WithRuntimePolicy(new LanguageRuntimePolicy(AllowHostInterop: false))
                .Build()).GetRequiredPlan();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package }));

        Assert.That(exception!.Message, Does.Contain("host-interop-free"));
    }

    [Test]
    public void LanguageTemplate_HasNoWistOrExampleLanguageHardcode()
    {
        var root = FindRepositoryRoot();
        var template = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.Templates", "content", "ut-language");
        var sourceFiles = Directory.EnumerateFiles(template, "*", SearchOption.AllDirectories)
            .Where(static path =>
                path.EndsWith(".cs", StringComparison.Ordinal) ||
                path.EndsWith(".csproj", StringComparison.Ordinal) ||
                path.EndsWith("template.json", StringComparison.Ordinal));
        var content = string.Join('\n', sourceFiles.Select(File.ReadAllText));

        Assert.Multiple(() =>
        {
            Assert.That(content, Does.Not.Contain("Wist"));
            Assert.That(content, Does.Not.Contain("Acme.Pricing"));
            Assert.That(content, Does.Contain("TemplateLanguage"));
            Assert.That(content, Does.Not.Contain("TemplateLanguageSyntax"));
            Assert.That(content, Does.Contain("internal sealed record LanguageSyntax"));
            Assert.That(content, Does.Contain("UniversalToolchain.LanguageAuthoring"));
        });
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "readme.md")))
            current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed record NumberDocument(int Value);

    private sealed class TestPackage(LanguagePackageDescriptor descriptor) : ILanguageExtensionPackage
    {
        public LanguagePackageDescriptor Descriptor { get; } = descriptor;
    }
}
