using UniversalToolchain.Dialects.Wist.Facade;
using UniversalToolchain.Dialects.Wist.Presets;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistRemediationRegressionTests
{
    [Test]
    public void WistProvider_RejectsSpoofedPackageAndMetadataDslInjection()
    {
        var backend = new BackendId("interpreter");
        var version = WistLanguageFeaturePackage.PackageVersion;
        var providerId = WistLanguageFeaturePackage.RuntimeProviderId;
        var featureId = new LanguageFeatureId("external.injected-feature");
        var source = StandardLanguageArtifactKinds.SourceText.Contract;
        var syntax = WistArtifactKinds.SyntaxTreeContract;
        var air = WistArtifactKinds.AirContract;
        var output = WistArtifactKinds.InterpreterArtifactContract;

        LanguageContributionDescriptor Module(string id, string alias) =>
            new(
                new LanguageContributionId(id),
                LanguageSlots.FrontendSyntax,
                metadata: new Dictionary<string, string> { ["wist.moduleAlias"] = alias });

        var contributions = new List<LanguageContributionDescriptor>
        {
            new(
                WistContributionIds.Frontend,
                LanguageSlots.FrontendParser,
                LanguageSlotMultiplicity.Single,
                ContributionMergePolicy.RejectDuplicate,
                transformation: new ArtifactTransformationDescriptor(source, syntax, 1)),
            new(
                WistContributionIds.LoweringToAir,
                LanguageSlots.Lowering,
                LanguageSlotMultiplicity.Single,
                ContributionMergePolicy.RejectDuplicate,
                transformation: new ArtifactTransformationDescriptor(syntax, air, 1)),
            new(
                WistContributionIds.InterpreterBackend,
                LanguageSlots.Backends,
                providesCapabilities: [LanguageCapabilities.Backend(backend)],
                supportedBackends: [backend],
                transformation: new ArtifactTransformationDescriptor(air, output, 1),
                backendInputContract: output),
            new(
                WistContributionIds.LegacyRuntimeAdapter,
                LanguageSlots.RuntimeProvider,
                LanguageSlotMultiplicity.Single,
                ContributionMergePolicy.RejectDuplicate,
                runtimeProviderId: providerId,
                runtimeProviderVersion: version,
                runtimeInputContracts: new Dictionary<BackendId, LanguageArtifactContract> { [backend] = output }),
            Module("external.module.whitespaces", "Whitespaces"),
            Module("external.module.scopes", "Scopes"),
            Module("external.module.numbers", "Numbers"),
            Module("external.module.arithmetic", "Arithmetic"),
            new(
                new LanguageContributionId("external.tooling.injector"),
                LanguageSlots.Tooling,
                metadata: new Dictionary<string, string>
                {
                    ["wist.moduleAlias"] = "Variables\nbackend compiler\nuse Comments"
                })
        };
        var package = new TestPackage(new LanguagePackageDescriptor(
            new LanguagePackageId("External.Spoofed.Wist"),
            version,
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(featureId, contributions: contributions.Select(static item => item.Id).ToArray())],
            contributions: contributions));
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(
            LanguageDefinitionBuilder.Create("Injected.Runtime", version.Value)
                .UseFeature(featureId)
                .EnableBackend(backend)
                .UseRuntimeProvider(providerId, version)
                .Build()).GetRequiredPlan();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(
                plan,
                new LanguageRuntimeProviderRegistry().AddProvider(new WistLanguageRuntimeProvider())));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("canonical Wist package"));
            Assert.That(exception.Message, Does.Not.Contain("Generated Wist dialect"));
        });
    }

    [Test]
    public void WistProvider_RejectsExactDescriptorCloneWithoutCanonicalImplementationProvenance()
    {
        var canonicalDescriptor = new WistLanguageFeaturePackage().Descriptor;
        var clonedPackage = new TestPackage(canonicalDescriptor);
        var backend = new BackendId("interpreter");
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(clonedPackage)).Compile(
            LanguageDefinitionBuilder.Create("Exact.Clone", "1")
                .UseFeature(WistFeatureIds.Arithmetic)
                .EnableBackend(backend)
                .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
                .Build()).GetRequiredPlan();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(
                plan,
                new LanguageRuntimeProviderRegistry().AddProvider(new WistLanguageRuntimeProvider())));

        Assert.That(exception!.Message, Does.Contain("canonical Wist package"));
    }

    [Test]
    public void WistProvider_AllowsUnrelatedForeignToolingWithoutTreatingMetadataAsDsl()
    {
        var toolingFeature = new LanguageFeatureId("external.safe-tooling");
        var toolingContribution = new LanguageContributionDescriptor(
            new LanguageContributionId("external.safe-tooling.contribution"),
            LanguageSlots.Tooling,
            metadata: new Dictionary<string, string>
            {
                ["wist.moduleAlias"] = "Variables\nbackend cil\nuse Comments"
            });
        var toolingPackage = new TestPackage(new LanguagePackageDescriptor(
            new LanguagePackageId("External.Safe.Tooling"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(toolingFeature, contributions: [toolingContribution.Id])],
            contributions: [toolingContribution]));
        var registry = new LanguagePackageRegistry()
            .AddPackage(new WistLanguageFeaturePackage())
            .AddPackage(toolingPackage);
        var backend = new BackendId("interpreter");
        var plan = new LanguageCompiler(registry).Compile(
            LanguageDefinitionBuilder.Create("Wist.With.Safe.Tooling", "1")
                .UseFeature(WistFeatureIds.Arithmetic)
                .UseFeature(toolingFeature)
                .EnableBackend(backend)
                .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
                .Build()).GetRequiredPlan();

        using var runtime = LanguageRuntime.Create(
            plan,
            new LanguageRuntimeProviderRegistry().AddProvider(new WistLanguageRuntimeProvider()));

        Assert.That(runtime.Run(new LanguageExecutionRequest("2 + 3", backend)).Value?.ToString(), Is.EqualTo("5"));
    }

    [Test]
    public void WistProvider_RejectsCanonicalIdAndVersionWithForgedManifestDigest()
    {
        var canonical = new WistLanguageFeaturePackage().Descriptor;
        var altered = new LanguagePackageDescriptor(
            canonical.Id,
            canonical.Version,
            canonical.ToolchainApiVersion,
            canonical.Features,
            new Dictionary<string, string>(canonical.Metadata, StringComparer.Ordinal)
            {
                ["untrusted-extra"] = "changes-canonical-manifest"
            },
            canonical.Contributions);
        var package = new TestPackage(altered);
        var backend = new BackendId("interpreter");
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(
            LanguageDefinitionBuilder.Create("Forged.Manifest", "1")
                .UseFeature(WistFeatureIds.Arithmetic)
                .EnableBackend(backend)
                .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
                .Build()).GetRequiredPlan();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(
                plan,
                new LanguageRuntimeProviderRegistry().AddProvider(new WistLanguageRuntimeProvider())));

        Assert.That(exception!.Message, Does.Contain("expected manifest digest"));
    }

    [TestCase("if 2 == 2 (1) else (2)", 1)]
    [TestCase("true and not false", true)]
    [TestCase("2 >= 1", true)]
    public void ConditionsAggregate_ProvidesControlFlowBooleanLogicAndComparisonsOnBothBackends(
        string source,
        object expected)
    {
        var package = new WistLanguageFeaturePackage();
        var interpreter = new BackendId("interpreter");
        var cil = new BackendId("cil");
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(
            LanguageDefinitionBuilder.Create("Conditions.Runtime.Check", "1")
                .UseFeature(WistFeatureIds.Conditions)
                .EnableBackend(interpreter)
                .EnableBackend(cil)
                .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
                .Build()).GetRequiredPlan();

        using var runtime = LanguageRuntime.Create(
            plan,
            new LanguageRuntimeProviderRegistry().AddProvider(new WistLanguageRuntimeProvider()));
        var interpreterResult = runtime.Run(new LanguageExecutionRequest(source, interpreter)).Value;
        var cilResult = runtime.Run(new LanguageExecutionRequest(source, cil)).Value;

        Assert.Multiple(() =>
        {
            Assert.That(cilResult?.GetType(), Is.EqualTo(interpreterResult?.GetType()),
                "Both backends must expose the same runtime value type.");
            Assert.That(cilResult?.ToString(), Is.EqualTo(interpreterResult?.ToString()),
                "Both backends must expose the same semantic value.");
            Assert.That(interpreterResult?.ToString(), Is.EqualTo(expected.ToString()));
        });
    }

    [TestCase("2 + 3 * 4")]
    [TestCase("(10 - 4) / 2")]
    public void MinimalArithmeticPreset_GenericPack_HasExecutableEquivalence(string source)
    {
        var backend = new BackendId("interpreter");
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(new WistLanguageFeaturePackage())).Compile(
            LanguageDefinitionBuilder.Create("Minimal.Arithmetic.Parity", "1")
                .UseFeature(WistFeatureIds.Arithmetic)
                .EnableBackend(backend)
                .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
                .Build()).GetRequiredPlan();

        using var genericRuntime = LanguageRuntime.Create(
            plan,
            new LanguageRuntimeProviderRegistry().AddProvider(new WistLanguageRuntimeProvider()));
        using var legacyRuntime = WistRuntimeFacadeBuilder
            .CreateDefault()
            .WithShippedDialectPreset(WistShippedDialectPresets.MinimalArithmetic)
            .Build();

        var genericResult = genericRuntime.Run(new LanguageExecutionRequest(source, backend)).Value;
        var legacyResult = legacyRuntime.Run(
            new WistRunRequest(source, new Dictionary<string, object?>(), "interpreter"));

        Assert.Multiple(() =>
        {
            Assert.That(genericResult?.GetType(), Is.EqualTo(legacyResult?.GetType()));
            Assert.That(genericResult?.ToString(), Is.EqualTo(legacyResult?.ToString()));
        });
    }

    [Test]
    public void SplitConditionFeatures_MapToExactSemanticModules()
    {
        var package = new WistLanguageFeaturePackage();
        var aliases = package.Descriptor.Contributions.ToDictionary(
            static contribution => contribution.Id,
            static contribution => contribution.Metadata.GetValueOrDefault("wist.moduleAlias"));

        Assert.Multiple(() =>
        {
            Assert.That(aliases[WistContributionIds.ComparisonsModule], Is.EqualTo("ComparisonConditions"));
            Assert.That(aliases[WistContributionIds.BooleanLogicModule], Is.EqualTo("BooleanConditions"));
            Assert.That(aliases[WistContributionIds.ConditionalControlFlowModule], Is.EqualTo("Conditions"));
            Assert.That(package.Descriptor.Features.Single(feature => feature.Id == WistFeatureIds.Conditions).Contributions,
                Is.EquivalentTo(new[]
                {
                    WistContributionIds.ComparisonsModule,
                    WistContributionIds.BooleanLogicModule,
                    WistContributionIds.ConditionalControlFlowModule
                }));
        });
    }

    private sealed class TestPackage(LanguagePackageDescriptor descriptor) : ILanguageExtensionPackage
    {
        public LanguagePackageDescriptor Descriptor { get; } = descriptor;
    }
}
