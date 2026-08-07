using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistSinglePlanAuthorityTests
{
    private static readonly BackendId Interpreter = new("interpreter");

    [Test]
    public void RuntimeProjection_PreservesPlannerOwnedContributionOrder()
    {
        var aliases = WistModuleSelection.ProjectModuleAliases(
        [
            WistContributionIds.WhitespacesModule,
            WistContributionIds.ArithmeticModule,
            WistContributionIds.VariablesModule
        ]);

        Assert.That(aliases, Is.EqualTo(new[] { "Whitespaces", "Arithmetic", "Variables" }));
    }

    [Test]
    public void ShippedPresets_UseTypedPolicyFeaturesWithoutBehavioralMetadata()
    {
        foreach (var presetId in WistLanguageDefinitions.PresetIds)
        {
            var definition = WistLanguageDefinitions.Create(presetId);
            var trusted = definition.SelectedFeatures.Contains(WistInternalFeatureIds.TrustedSecurity);
            var restricted = definition.SelectedFeatures.Contains(WistInternalFeatureIds.RestrictedSecurity);

            Assert.Multiple(() =>
            {
                Assert.That(definition.Metadata.Keys, Does.Not.Contain("wist.security"), presetId);
                Assert.That(definition.Metadata.Keys.Any(static key => key.StartsWith("wist.capability.", StringComparison.Ordinal)),
                    Is.False, presetId);
                Assert.That(trusted ^ restricted, Is.True, $"{presetId} must select exactly one typed security feature.");
                Assert.That(
                    definition.SelectedFeatures.Contains(WistInternalFeatureIds.CompositionRestricted),
                    Is.EqualTo(presetId == WistLanguageDefinitions.CompositionRestrictedId),
                    presetId);
            });
        }
    }

    [Test]
    public void OpaqueUnsafeInteropMetadata_CannotChangeRuntimeBehavior()
    {
        var definition = LanguageDefinitionBuilder.Create("Wist.S02.MetadataGuard", "1")
            .UseFeature(WistInternalFeatureIds.RestrictedSecurity)
            .UseFeature(WistFeatureIds.Arithmetic)
            .EnableBackend(Interpreter)
            .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
            .WithRuntimePolicy(new LanguageRuntimePolicy(AllowHostInterop: false))
            .WithMetadata("wist.capability.unsafe-interop", bool.TrueString)
            .Build();
        var plan = Compile(new WistLanguageFeaturePackage(), definition);

        using var runtime = LanguageRuntime.Create(
            plan,
            new LanguageRuntimeProviderRegistry().AddProvider(new WistLanguageRuntimeProvider()));
        var result = runtime.Run(new LanguageExecutionRequest("2 + 3", Interpreter));

        Assert.That(result.Value?.ToString(), Is.EqualTo("5"));
    }

    [Test]
    public void CanonicalPlan_StoresExplicitModuleAndOptimizerOrder()
    {
        var plan = Compile(
            new WistLanguageFeaturePackage(),
            WistLanguageDefinitions.Create(WistLanguageDefinitions.FullDefaultId));
        var modules = plan.Contributions
            .Where(static contribution => contribution.Contribution.Slot == LanguageSlots.FrontendSyntax)
            .ToArray();
        var optimizers = plan.Contributions
            .Where(static contribution => contribution.Contribution.Slot == LanguageSlots.Optimizers)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(modules.Select(static contribution => contribution.Contribution.Id), Is.EqualTo(new[]
            {
                WistContributionIds.ArithmeticModule,
                WistContributionIds.BooleanLogicModule,
                WistContributionIds.CSharpInteropModule,
                WistContributionIds.CommentsModule,
                WistContributionIds.ComparisonsModule,
                WistContributionIds.ConditionalControlFlowModule,
                WistContributionIds.EqualityModule,
                WistContributionIds.IdentifiersModule,
                WistContributionIds.LabelsModule,
                WistContributionIds.LoopsModule,
                WistContributionIds.NumbersModule,
                WistContributionIds.ScopesModule,
                WistContributionIds.SemicolonAsNewLineModule,
                WistContributionIds.VariablesModule,
                WistContributionIds.WhitespacesModule
            }));
            Assert.That(modules.Select(static contribution => contribution.Contribution.Order), Is.Ordered.Ascending);
            Assert.That(optimizers.Select(static contribution => contribution.Contribution.Id), Is.EqualTo(new[]
            {
                WistContributionIds.BooleanOptimizer,
                WistContributionIds.ComparisonIntrinsicOptimizer
            }));
            Assert.That(optimizers.Select(static contribution => contribution.Contribution.Order), Is.Ordered.Ascending);
        });
    }

    [Test]
    public void CanonicalInputOrderIsStable_WhileProvenanceDistinctPlanKeepsSemanticProjection()
    {
        var descriptor = new WistLanguageFeaturePackage().Descriptor;
        var reorderedDescriptor = new LanguagePackageDescriptor(
            descriptor.Id,
            descriptor.Version,
            descriptor.ToolchainApiVersion,
            descriptor.Features.Reverse(),
            descriptor.Metadata,
            descriptor.Contributions.Reverse());
        var definition = WistLanguageDefinitions.Create(WistLanguageDefinitions.MinimalArithmeticId);

        var first = Compile(new Package(descriptor), definition);
        var reordered = Compile(new Package(reorderedDescriptor), definition);
        var canonicalImplementation = Compile(new WistLanguageFeaturePackage(), definition);

        Assert.Multiple(() =>
        {
            Assert.That(reordered.PlanHash, Is.EqualTo(first.PlanHash));
            Assert.That(LanguageLockFile.Serialize(reordered), Is.EqualTo(LanguageLockFile.Serialize(first)));
            Assert.That(SemanticProjection(canonicalImplementation), Is.EqualTo(SemanticProjection(first)));
            Assert.That(canonicalImplementation.PlanHash, Is.Not.EqualTo(first.PlanHash));
        });
    }

    [Test]
    public void PassOrderingCycle_ProducesStablePlanningDiagnostic()
    {
        var middle = new LanguageArtifactContract(
            new LanguageArtifactKindId("s02.middle"),
            LanguageTypeIdentity.For<string>());
        var output = new LanguageArtifactContract(
            new LanguageArtifactKindId("s02.output"),
            LanguageTypeIdentity.For<string>());
        var backend = new BackendId("s02");
        var feature = new LanguageFeatureId("s02.feature");
        var parse = new LanguageContributionId("s02.parse");
        var passA = new LanguageContributionId("s02.pass.a");
        var passB = new LanguageContributionId("s02.pass.b");
        var backendContribution = new LanguageContributionId("s02.backend");
        var runtimeContribution = new LanguageContributionId("s02.runtime");
        var package = new Package(new LanguagePackageDescriptor(
            new LanguagePackageId("S02.Ordering"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(feature, contributions: [parse, passA, passB, backendContribution])],
            contributions:
            [
                new LanguageContributionDescriptor(
                    parse,
                    LanguageSlots.FrontendParser,
                    transformation: new ArtifactTransformationDescriptor(StandardLanguageArtifactKinds.SourceText.Contract, middle, 1)),
                new LanguageContributionDescriptor(
                    passA,
                    LanguageSlots.Optimizers,
                    transformation: new ArtifactTransformationDescriptor(middle, middle, 0),
                    afterContributions: [passB]),
                new LanguageContributionDescriptor(
                    passB,
                    LanguageSlots.Optimizers,
                    transformation: new ArtifactTransformationDescriptor(middle, middle, 0),
                    afterContributions: [passA]),
                new LanguageContributionDescriptor(
                    backendContribution,
                    LanguageSlots.Backends,
                    providesCapabilities: [LanguageCapabilities.Backend(backend)],
                    supportedBackends: [backend],
                    transformation: new ArtifactTransformationDescriptor(middle, output, 1),
                    backendInputContract: output),
                new LanguageContributionDescriptor(
                    runtimeContribution,
                    LanguageSlots.RuntimeProvider,
                    LanguageSlotMultiplicity.Single,
                    ContributionMergePolicy.RejectDuplicate,
                    runtimeProviderId: new LanguageRuntimeProviderId("s02.runtime"),
                    runtimeProviderVersion: new LanguageVersion("1"),
                    runtimeInputContracts: new Dictionary<BackendId, LanguageArtifactContract> { [backend] = output })
            ]));
        var definition = LanguageDefinitionBuilder.Create("S02.Ordering", "1")
            .UseFeature(feature)
            .EnableBackend(backend)
            .UseRuntimeProvider("s02.runtime", "1")
            .Build();

        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(definition);
        var diagnostics = result.Diagnostics.Where(static diagnostic => diagnostic.Code == "UTL2202").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(diagnostics, Has.Length.EqualTo(1));
            Assert.That(diagnostics[0].Stage, Is.EqualTo("planning"));
            Assert.That(diagnostics[0].Message, Does.Contain("ordering cycle"));
        });
    }

    private static LanguagePlan Compile(ILanguageFeaturePackage package, LanguageDefinition definition) =>
        new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(definition).GetRequiredPlan();

    private static string SemanticProjection(LanguagePlan plan) => string.Join("\n",
        plan.Definition.SelectedFeatures.OrderBy(static feature => feature.Value, StringComparer.Ordinal).Select(static feature => $"feature:{feature.Value}")
            .Concat(plan.Contributions.Select(static contribution => $"contribution:{contribution.Contribution.Id.Value}:{contribution.Contribution.Order}"))
            .Concat(plan.Routes.Values.OrderBy(static route => route.Backend.Value, StringComparer.Ordinal)
                .SelectMany(static route => route.Steps.Select(step =>
                    $"route:{route.Backend.Value}:{step.ContributionId.Value}:{step.SourceContract}:{step.TargetContract}:{step.Cost}")))
            .Append($"policy:{plan.Definition.RuntimePolicy.RequireDeterminism}:{plan.Definition.RuntimePolicy.AllowHostInterop}")
            .Append($"runtime:{plan.RuntimeProvider?.ProviderId.Value}@{plan.RuntimeProvider?.Version.Value}"));

    private sealed class Package(LanguagePackageDescriptor descriptor) : ILanguageExtensionPackage
    {
        public LanguagePackageDescriptor Descriptor { get; } = descriptor;
    }
}
