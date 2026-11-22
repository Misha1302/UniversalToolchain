using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.LanguageSdk.Tests;

/// <summary>
///     Pins the diagnostics that the artifact-route phase and the resolution tail can emit, none of which
///     had a test before.
/// </summary>
/// <remarks>
///     <para>
///         These eight codes are a different shape from the registry-level ones: the fixture has to build a
///         route whose <em>topology or contracts</em> are contradictory, not a registry whose declarations are.
///         That is why they were left over while the cheaper ones were covered first.
///     </para>
///     <para>
///         Two of them (<c>UTL2012</c>, <c>UTL2013</c>) are reachable only through a slot override: the required
///         neighbour must be registered and eligible, be resolved, and then be removed by
///         <see cref="LanguageDefinitionBuilder.ReplaceSlot" />. Without the override the resolver would have
///         materialised it, so the requirement could never be observed missing.
///     </para>
///     <para>
///         <c>UTL2304</c> is the deliberate inverse: the runtime provider is picked correctly and then deleted by
///         a slot override that names an unrelated contribution for its slot.
///     </para>
/// </remarks>
[TestFixture]
public sealed class ArtifactRouteDiagnosticCoverageTests
{
    private static readonly LanguageVersion V1 = new("1");
    private static readonly BackendId RouteBackend = new("coverage.route");
    private static readonly LanguageRuntimeProviderId RouteProvider = new("coverage.route.provider");

    private static readonly LanguageArtifactKind<string> Source = StandardLanguageArtifactKinds.SourceText;
    private static readonly LanguageArtifactKind<int> Middle = new("coverage.middle");
    private static readonly LanguageArtifactKind<int> MiddleOther = new("coverage.middle.other");
    private static readonly LanguageArtifactKind<int> Executable = new("coverage.executable");

    [Test]
    public void BackendWithNoRegisteredRuntimeProvider_ReportsProviderSelectionFailure()
    {
        // Enabling a backend makes the definition executable, and an executable definition must name a runtime
        // provider. Here one exists in no package at all.
        var feature = new LanguageFeatureId("coverage.provider.feature");
        var main = MainPackage(
            feature,
            [BackendContribution(new LanguageContributionId("coverage.provider.backend"), Executable.Contract)]);

        var result = Compile(
            registry => registry.AddPackage(new TestPackage(main)),
            builder => builder.UseFeature(feature).EnableBackend(RouteBackend));

        AssertCode(result, "UTL2301");
    }

    [Test]
    public void BackendThatDeclaresNoInputContract_ReportsMissingExecutionArtifact()
    {
        // UTL2303 is the case where a runtime provider was selected, but neither it nor the backend contribution
        // says which artifact the backend consumes. The route has no target, so this is reported before any
        // route search happens.
        var feature = new LanguageFeatureId("coverage.no-input.feature");
        var main = MainPackage(
            feature,
            [
                BackendContribution(new LanguageContributionId("coverage.no-input.backend"), backendInput: null),
                Transformer(new LanguageContributionId("coverage.no-input.parse"), Source, Executable, 1)
            ]);
        var provider = RuntimeProviderPackage(inputContracts: null);

        var result = Compile(
            registry => registry.AddPackage(new TestPackage(main)).AddPackage(new TestPackage(provider)),
            builder => builder.UseFeature(feature).EnableBackend(RouteBackend));

        AssertCode(result, "UTL2303");
    }

    [Test]
    public void BackendInputContractThatNoTransformationProduces_ReportsNoRoute()
    {
        // The target contract is declared and the source contract is the language entry artifact. They are
        // different kinds and no transformation bridges them, so no route exists.
        var feature = new LanguageFeatureId("coverage.no-route.feature");
        var main = MainPackage(
            feature,
            [BackendContribution(new LanguageContributionId("coverage.no-route.backend"), Executable.Contract)]);
        var provider = RuntimeProviderPackage(Executable.Contract);

        var result = Compile(
            registry => registry.AddPackage(new TestPackage(main)).AddPackage(new TestPackage(provider)),
            builder => builder.UseFeature(feature).EnableBackend(RouteBackend));

        AssertCode(result, "UTL2201");
    }

    [Test]
    public void TwoEquallyCheapFeasibleRoutes_ReportAmbiguityInsteadOfPickingByContributionId()
    {
        // Two distinct minimum-cost paths, both fully feasible. Picking one silently would make the executed
        // program depend on contribution-ID ordering, which is exactly the accident the diagnostic exists to
        // prevent.
        var feature = new LanguageFeatureId("coverage.ambiguous.feature");
        var main = MainPackage(
            feature,
            [
                Transformer(new LanguageContributionId("coverage.ambiguous.left.parse"), Source, Middle, 1),
                Transformer(new LanguageContributionId("coverage.ambiguous.left.lower"), Middle, Executable, 1),
                Transformer(new LanguageContributionId("coverage.ambiguous.right.parse"), Source, MiddleOther, 1),
                Transformer(new LanguageContributionId("coverage.ambiguous.right.lower"), MiddleOther, Executable, 1),
                BackendContribution(new LanguageContributionId("coverage.ambiguous.backend"), Executable.Contract)
            ]);
        var provider = RuntimeProviderPackage(Executable.Contract);

        var result = Compile(
            registry => registry.AddPackage(new TestPackage(main)).AddPackage(new TestPackage(provider)),
            builder => builder.UseFeature(feature).EnableBackend(RouteBackend));

        AssertCode(result, "UTL2207");
    }

    [Test]
    public void DescriptorOrderThatTheTopologyCannotSatisfy_ReportsDescriptorOrderViolation()
    {
        // 'lower' must run before 'parse', but 'parse' is the only way to reach 'lower'. The constrained search
        // is correctly infeasible; the diagnostic comes from replaying the unconstrained route and checking the
        // declarations against it, which is the only place the contradiction becomes reportable.
        var feature = new LanguageFeatureId("coverage.descriptor-order.feature");
        var parse = new LanguageContributionId("coverage.descriptor-order.parse");
        var lower = new LanguageContributionId("coverage.descriptor-order.lower");
        var main = MainPackage(
            feature,
            [
                Transformer(parse, Source, Middle, 1),
                Transformer(lower, Middle, Executable, 1, beforeContributions: [parse]),
                BackendContribution(new LanguageContributionId("coverage.descriptor-order.backend"), Executable.Contract)
            ]);
        var provider = RuntimeProviderPackage(Executable.Contract);

        var result = Compile(
            registry => registry.AddPackage(new TestPackage(main)).AddPackage(new TestPackage(provider)),
            builder => builder.UseFeature(feature).EnableBackend(RouteBackend));

        AssertCode(result, "UTL2206");
    }

    [Test]
    public void DefinitionOrderThatTheTopologyCannotSatisfy_ReportsDefinitionOrderViolation()
    {
        // Same contradiction, declared on the language definition instead of the package. Both contributions sit
        // in one slot, because a cross-slot constraint is rejected earlier with UTL2111 and would mask this.
        var feature = new LanguageFeatureId("coverage.definition-order.feature");
        var slot = new LanguageSlotId("coverage.definition-order.slot");
        var parse = new LanguageContributionId("coverage.definition-order.parse");
        var lower = new LanguageContributionId("coverage.definition-order.lower");
        var main = MainPackage(
            feature,
            [
                Transformer(parse, Source, Middle, 1, slot: slot),
                Transformer(lower, Middle, Executable, 1, slot: slot),
                BackendContribution(new LanguageContributionId("coverage.definition-order.backend"), Executable.Contract)
            ]);
        var provider = RuntimeProviderPackage(Executable.Contract);

        var result = Compile(
            registry => registry.AddPackage(new TestPackage(main)).AddPackage(new TestPackage(provider)),
            builder => builder
                .UseFeature(feature)
                .EnableBackend(RouteBackend)
                .OrderContributionBefore(lower, parse));

        AssertCode(result, "UTL2205");
    }

    [Test]
    public void RequiredContributionRemovedBySlotOverride_ReportsDanglingRequirement()
    {
        // The requirement is satisfied at visit time - without the override the resolver materialises 'victim'.
        // The override then removes it, so the surviving graph has a consumer whose dependency is gone. Checking
        // requirements against the *effective* set is what makes that observable.
        var feature = new LanguageFeatureId("coverage.dangling.feature");
        var consumer = new LanguageContributionId("coverage.dangling.consumer");
        var victim = new LanguageContributionId("coverage.dangling.victim");
        var replacement = new LanguageContributionId("coverage.dangling.replacement");
        var overriddenSlot = new LanguageSlotId("coverage.dangling.overridden");
        var main = MainPackage(
            feature,
            [
                new LanguageContributionDescriptor(
                    consumer,
                    LanguageSlots.Tooling,
                    requiresContributions: [victim]),
                new LanguageContributionDescriptor(victim, overriddenSlot),
                new LanguageContributionDescriptor(replacement, overriddenSlot)
            ]);

        var result = Compile(
            registry => registry.AddPackage(new TestPackage(main)),
            builder => builder.UseFeature(feature).ReplaceSlot(overriddenSlot, replacement));

        AssertCode(result, "UTL2012");
    }

    [Test]
    public void CapabilityProviderRemovedBySlotOverride_ReportsDanglingCapabilityRequirement()
    {
        // The capability has exactly one provider, so the resolver visits it and marks the requirement satisfiable.
        // The override removes that provider from the effective graph, leaving a consumer that needs a capability
        // nothing provides any more.
        var feature = new LanguageFeatureId("coverage.dangling-capability.feature");
        var capability = new LanguageCapabilityId("coverage.dangling.capability");
        var consumer = new LanguageContributionId("coverage.dangling-capability.consumer");
        var provider = new LanguageContributionId("coverage.dangling-capability.provider");
        var replacement = new LanguageContributionId("coverage.dangling-capability.replacement");
        var overriddenSlot = new LanguageSlotId("coverage.dangling-capability.overridden");
        var main = MainPackage(
            feature,
            [
                new LanguageContributionDescriptor(
                    consumer,
                    LanguageSlots.Tooling,
                    requiresCapabilities: [capability]),
                new LanguageContributionDescriptor(provider, overriddenSlot, providesCapabilities: [capability]),
                new LanguageContributionDescriptor(replacement, overriddenSlot)
            ]);

        var result = Compile(
            registry => registry.AddPackage(new TestPackage(main)),
            builder => builder.UseFeature(feature).ReplaceSlot(overriddenSlot, replacement));

        AssertCode(result, "UTL2013");
    }

    [Test]
    public void RuntimeProviderRemovedBySlotOverride_ReportsProviderLoss()
    {
        // The provider is selected correctly and then deleted by an override that claims its slot for an
        // unrelated contribution. The plan would be executable with no provider, so this cannot be silent.
        var feature = new LanguageFeatureId("coverage.provider-loss.feature");
        var intruder = new LanguageContributionId("coverage.provider-loss.intruder");
        var main = MainPackage(
            feature,
            [
                BackendContribution(new LanguageContributionId("coverage.provider-loss.backend"), Executable.Contract),
                Transformer(new LanguageContributionId("coverage.provider-loss.parse"), Source, Executable, 1),
                new LanguageContributionDescriptor(intruder, LanguageSlots.RuntimeProvider)
            ]);
        var provider = RuntimeProviderPackage(Executable.Contract);

        var result = Compile(
            registry => registry.AddPackage(new TestPackage(main)).AddPackage(new TestPackage(provider)),
            builder => builder
                .UseFeature(feature)
                .EnableBackend(RouteBackend)
                .ReplaceSlot(LanguageSlots.RuntimeProvider, intruder));

        AssertCode(result, "UTL2304");
    }

    private static void AssertCode(LanguageBuildResult result, string code)
    {
        var observed = string.Join(
            " | ",
            result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Code), Does.Contain(code),
                $"{code} was not reported; observed: [{observed}]");
            Assert.That(
                result.Diagnostics.First(diagnostic => diagnostic.Code == code).Severity,
                Is.EqualTo(LanguageDiagnosticSeverity.Error));
            Assert.That(result.Plan, Is.Null);
        });
    }

    private static LanguageBuildResult Compile(
        Func<LanguagePackageRegistry, LanguagePackageRegistry> configureRegistry,
        Func<LanguageDefinitionBuilder, LanguageDefinitionBuilder> configureDefinition) =>
        new LanguageCompiler(configureRegistry(new LanguagePackageRegistry())).Compile(
            configureDefinition(LanguageDefinitionBuilder.Create("Coverage.Tests", "1")).Build());

    private static LanguageContributionDescriptor Transformer<TSource, TTarget>(
        LanguageContributionId id,
        LanguageArtifactKind<TSource> from,
        LanguageArtifactKind<TTarget> to,
        int cost,
        LanguageSlotId? slot = null,
        IEnumerable<LanguageContributionId>? beforeContributions = null) =>
        new(
            id,
            slot ?? new LanguageSlotId($"coverage.slot.{id.Value}"),
            transformation: ArtifactTransformationDescriptor.Create(from, to, cost),
            beforeContributions: beforeContributions);

    private static LanguageContributionDescriptor BackendContribution(
        LanguageContributionId id,
        LanguageArtifactContract? backendInput) =>
        new(
            id,
            LanguageSlots.Backends,
            providesCapabilities: [LanguageCapabilities.Backend(RouteBackend)],
            supportedBackends: [RouteBackend],
            backendInputContract: backendInput);

    private static LanguagePackageDescriptor MainPackage(
        LanguageFeatureId feature,
        LanguageContributionDescriptor[] contributions) =>
        new(
            new LanguagePackageId("Coverage.Main"),
            V1,
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(feature, contributions: contributions.Select(static x => x.Id).ToArray())],
            contributions: contributions);

    private static LanguagePackageDescriptor RuntimeProviderPackage(
        LanguageArtifactContract? inputContracts) =>
        new(
            new LanguagePackageId("Coverage.Provider"),
            V1,
            ToolchainApi.Current,
            [],
            contributions:
            [
                new LanguageContributionDescriptor(
                    new LanguageContributionId("coverage.provider"),
                    LanguageSlots.RuntimeProvider,
                    LanguageSlotMultiplicity.Single,
                    ContributionMergePolicy.RejectDuplicate,
                    providesCapabilities: [LanguageCapabilities.RuntimeProvider],
                    runtimeProviderId: RouteProvider,
                    runtimeProviderVersion: V1,
                    runtimeInputContracts: inputContracts == null
                        ? null
                        : new Dictionary<BackendId, LanguageArtifactContract> { [RouteBackend] = inputContracts.Value })
            ]);

    private sealed class TestPackage(LanguagePackageDescriptor descriptor) : ILanguageExtensionPackage
    {
        public LanguagePackageDescriptor Descriptor { get; } = descriptor;
    }
}
