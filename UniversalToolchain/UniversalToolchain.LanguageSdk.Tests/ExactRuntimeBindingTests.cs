using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class ExactRuntimeBindingTests
{
    private static readonly LanguageRuntimeComponentTraits SafeTraits =
        LanguageRuntimeComponentTraits.DeterministicNoHostInterop;

    [Test]
    public void RuntimeAssembler_RejectsEquivalentButDifferentPackageRegistration()
    {
        var planned = CreateSinglePackage("Exact.Binding", "exact", SafeTraits);
        var equivalent = CreateSinglePackage("Exact.Binding", "exact", SafeTraits);
        var plan = CompileSinglePackage(planned, "exact.core", new BackendId("exact"));

        Assert.That(
            LanguageFeatureManifestSerializer.ComputeSha256(equivalent.Descriptor),
            Is.EqualTo(LanguageFeatureManifestSerializer.ComputeSha256(planned.Descriptor)));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { equivalent }));

        Assert.That(exception!.Message, Does.Contain("exact package implementation"));
    }

    [Test]
    public void RuntimeAssembler_MaterializesOnlyPlannedComponents()
    {
        var syntax = new LanguageArtifactKind<int>("materialize.syntax");
        var backend = new BackendId("materialize");
        var hidden = new LanguageContributionId("materialize.hidden");
        var frontendBase = LanguagePackageBuilder.Create("Materialize.Frontend", "1")
            .AddFeature("materialize.frontend", feature => feature.AddTransformer(
                "materialize.parse",
                LanguageSlots.FrontendParser,
                StandardLanguageArtifactKinds.SourceText,
                syntax,
                static (source, _) => int.Parse(source),
                SafeTraits,
                cost: 1))
            .Build();
        var executionBase = LanguagePackageBuilder.Create("Materialize.Execution", "1")
            .AddBackend(
                backend.Value,
                "materialize.backend",
                syntax,
                static (value, _) => value + 1,
                SafeTraits)
            .UseRouteRuntime("materialize.runtime", "1")
            .Build();
        var frontend = new RuntimePackage(frontendBase.Descriptor, AddHidden(frontendBase.Components, hidden));
        var execution = new RuntimePackage(executionBase.Descriptor, AddHidden(executionBase.Components, hidden));
        var plan = new LanguageCompiler(new LanguagePackageRegistry()
                .AddPackage(frontend)
                .AddPackage(execution))
            .Compile(LanguageDefinitionBuilder.Create("Materialize.Language", "1")
                .UseFeature("materialize.frontend")
                .EnableBackend(backend)
                .UseRuntimeProvider("materialize.runtime", "1")
                .Build())
            .GetRequiredPlan();

        using var runtime = LanguageRuntime.Create(
            plan,
            new ILanguageRouteComponentSource[] { frontend, execution });
        var result = runtime.Run(new LanguageExecutionRequest("41", backend));

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(42));
            Assert.That(plan.Contributions.Select(static item => item.Contribution.Id), Does.Not.Contain(hidden));
        });
    }

    [Test]
    public void RuntimeAssembler_RejectsSelectedTransformerWithWrongContract()
    {
        var syntax = new LanguageArtifactKind<int>("contract.syntax");
        var wrongSyntax = new LanguageArtifactKind<int>("contract.wrong-syntax");
        var backend = new BackendId("contract");
        var parser = new LanguageContributionId("contract.parse");
        var packageBase = LanguagePackageBuilder.Create("Contract.Binding", "1")
            .AddFeature("contract.core", feature => feature
                .AddTransformer(
                    parser.Value,
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    syntax,
                    static (source, _) => int.Parse(source),
                    SafeTraits)
                .AddBackend(
                    backend,
                    new LanguageContributionId("contract.backend"),
                    syntax,
                    static (value, _) => value,
                    SafeTraits))
            .UseRouteRuntime("contract.runtime", "1")
            .Build();
        var components = new LanguageRouteComponentRegistry()
            .AddTransformer(LanguageTransformerRegistration.Create<string, int>(
                parser,
                StandardLanguageArtifactKinds.SourceText,
                wrongSyntax,
                SafeTraits,
                _ => new DelegateLanguageArtifactTransformer<string, int>(
                    parser,
                    StandardLanguageArtifactKinds.SourceText,
                    wrongSyntax,
                    static (source, _) => int.Parse(source),
                    SafeTraits)));
        foreach (var executor in packageBase.Components.Executors)
            components.AddExecutor(executor);
        var package = new RuntimePackage(packageBase.Descriptor, components.CreateCatalog());
        var plan = CompileSinglePackage(package, "contract.core", backend, "contract.runtime");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package }));

        Assert.That(exception!.Message, Does.Contain("exact artifact contracts"));
    }

    [Test]
    public void RuntimeFactory_TraitDriftIsRejectedBeforeExecution()
    {
        var syntax = new LanguageArtifactKind<int>("traits.syntax");
        var backend = new BackendId("traits");
        var parser = new LanguageContributionId("traits.parse");
        var package = LanguagePackageBuilder.Create("Traits.Binding", "1")
            .AddFeature("traits.core", feature => feature
                .AddTransformerFactory(
                    parser.Value,
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    syntax,
                    _ => new DelegateLanguageArtifactTransformer<string, int>(
                        parser,
                        StandardLanguageArtifactKinds.SourceText,
                        syntax,
                        static (source, _) => int.Parse(source),
                        LanguageRuntimeComponentTraits.Unknown),
                    SafeTraits)
                .AddBackend(
                    backend,
                    new LanguageContributionId("traits.backend"),
                    syntax,
                    static (value, _) => value,
                    SafeTraits))
            .UseRouteRuntime("traits.runtime", "1")
            .Build();
        var plan = CompileSinglePackage(package, "traits.core", backend, "traits.runtime");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package }));

        Assert.That(exception!.Message, Does.Contain("traits do not match its registration"));
    }

    [Test]
    public void StrictPolicy_AcceptsEvidenceBackedTraitsAndRejectsUnknownTraits()
    {
        var safe = CreateSinglePackage("Strict.Safe", "strict-safe", SafeTraits);
        var unknown = CreateSinglePackage("Strict.Unknown", "strict-unknown", LanguageRuntimeComponentTraits.Unknown);
        var safeBackend = new BackendId("strict-safe");
        var unknownBackend = new BackendId("strict-unknown");
        var safePlan = CompileSinglePackage(
            safe,
            "strict-safe.core",
            safeBackend,
            "strict-safe.runtime",
            new LanguageRuntimePolicy(RequireDeterminism: true, AllowHostInterop: false));
        var unknownPlan = CompileSinglePackage(
            unknown,
            "strict-unknown.core",
            unknownBackend,
            "strict-unknown.runtime",
            new LanguageRuntimePolicy(RequireDeterminism: true, AllowHostInterop: false));

        using var runtime = LanguageRuntime.Create(safePlan, new ILanguageRouteComponentSource[] { safe });
        var result = runtime.Run(new LanguageExecutionRequest("41", safeBackend));
        var exception = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(unknownPlan, new ILanguageRouteComponentSource[] { unknown }));

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(42));
            Assert.That(exception!.Message, Does.Contain("deterministic"));
        });
    }

    [Test]
    public void ComponentRegistry_RejectsDuplicateExactRegistrations()
    {
        var transformerId = new LanguageContributionId("duplicate.transformer");
        var backendId = new BackendId("duplicate");
        var executorId = new LanguageContributionId("duplicate.executor");
        var transformer = LanguageTransformerRegistration.Create<string, string>(
            transformerId,
            StandardLanguageArtifactKinds.SourceText,
            StandardLanguageArtifactKinds.SourceText,
            SafeTraits,
            _ => new DelegateLanguageArtifactTransformer<string, string>(
                transformerId,
                StandardLanguageArtifactKinds.SourceText,
                StandardLanguageArtifactKinds.SourceText,
                static (value, _) => value,
                SafeTraits));
        var executor = LanguageExecutorRegistration.Create<string, string>(
            executorId,
            backendId,
            StandardLanguageArtifactKinds.SourceText,
            SafeTraits,
            _ => new DelegateLanguageArtifactExecutor<string, string>(
                executorId,
                backendId,
                StandardLanguageArtifactKinds.SourceText,
                static (value, _) => value,
                SafeTraits));
        var transformerRegistry = new LanguageRouteComponentRegistry().AddTransformer(transformer);
        var executorRegistry = new LanguageRouteComponentRegistry().AddExecutor(executor);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => transformerRegistry.AddTransformer(transformer));
            Assert.Throws<InvalidOperationException>(() => executorRegistry.AddExecutor(executor));
        });
    }

    private static AuthoredLanguagePackage CreateSinglePackage(
        string packageId,
        string prefix,
        LanguageRuntimeComponentTraits traits)
    {
        var syntax = new LanguageArtifactKind<int>($"{prefix}.syntax");
        var backend = new BackendId(prefix);
        return LanguagePackageBuilder.Create(packageId, "1")
            .AddFeature($"{prefix}.core", feature => feature
                .AddTransformer(
                    $"{prefix}.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    syntax,
                    static (source, _) => int.Parse(source),
                    traits)
                .AddBackend(
                    backend,
                    new LanguageContributionId($"{prefix}.backend"),
                    syntax,
                    static (value, _) => value + 1,
                    traits))
            .UseRouteRuntime($"{prefix}.runtime", "1")
            .Build();
    }

    private static LanguagePlan CompileSinglePackage(
        ILanguageExtensionPackage package,
        string feature,
        BackendId backend,
        string? runtimeProvider = null,
        LanguageRuntimePolicy? policy = null) =>
        new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(LanguageDefinitionBuilder.Create(package.Descriptor.Id.Value, package.Descriptor.Version.Value)
                .UseFeature(feature)
                .EnableBackend(backend)
                .UseRuntimeProvider(runtimeProvider ?? $"{backend.Value}.runtime", "1")
                .WithRuntimePolicy(policy ?? new LanguageRuntimePolicy())
                .Build())
            .GetRequiredPlan();

    private static LanguageRouteComponentCatalog AddHidden(
        LanguageRouteComponentCatalog source,
        LanguageContributionId hidden)
    {
        var registry = new LanguageRouteComponentRegistry().AddCatalog(source);
        registry.AddTransformer(LanguageTransformerRegistration.Create<string, string>(
            hidden,
            StandardLanguageArtifactKinds.SourceText,
            StandardLanguageArtifactKinds.SourceText,
            SafeTraits,
            _ => new DelegateLanguageArtifactTransformer<string, string>(
                hidden,
                StandardLanguageArtifactKinds.SourceText,
                StandardLanguageArtifactKinds.SourceText,
                static (value, _) => value,
                SafeTraits)));
        return registry.CreateCatalog();
    }

    private sealed class RuntimePackage(
        LanguagePackageDescriptor descriptor,
        LanguageRouteComponentCatalog components) : ILanguageExtensionPackage, ILanguageRouteComponentSource
    {
        public LanguagePackageDescriptor Descriptor { get; } = descriptor;
        public LanguageRouteComponentCatalog Components { get; } = components;
    }
}
