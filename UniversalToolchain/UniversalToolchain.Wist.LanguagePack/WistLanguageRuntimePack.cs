using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.Wist.LanguagePack;

public sealed class WistLanguageRuntimeProvider : ILanguageRuntimeProvider, ILanguageRuntimePolicyValidator
{
    private static readonly BackendId CilBackend = new("cil");
    private static readonly BackendId InterpreterBackend = new("interpreter");
    private static readonly BackendId[] Backends = [CilBackend, InterpreterBackend];

    public LanguageRuntimeProviderId ProviderId => WistLanguageFeaturePackage.RuntimeProviderId;
    public LanguageVersion ProviderVersion => WistLanguageFeaturePackage.PackageVersion;
    public ToolchainApiVersion ToolchainApiVersion => ToolchainApi.Current;
    public LanguageContributionId RuntimeContributionId => WistContributionIds.LegacyRuntimeAdapter;
    public IReadOnlyCollection<BackendId> SupportedBackends => Backends;

    public void ValidatePolicy(LanguagePlan plan, LanguageRuntimePolicy policy, LanguageRuntimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(options);
        ValidateCanonicalRoute(plan);
        WistModuleSelection.ValidateCanonicalPackageProvenance(plan);
        if (policy.RequireDeterminism)
        {
            throw new InvalidOperationException(
                "The legacy Wist compatibility provider does not expose complete component-level determinism evidence.");
        }
        if (!policy.AllowHostInterop && options.AllowedAssemblies.Count != 0)
        {
            throw new InvalidOperationException(
                "The Wist language plan forbids host interop, but allowed host assemblies were supplied.");
        }
    }

    public ILanguageRuntimeSession CreateSession(LanguagePlan plan, LanguageRuntimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(options);
        ValidateCanonicalRoute(plan);
        WistModuleSelection.ValidateCanonicalPackageProvenance(plan);
        return new WistLanguageRuntimeSession(plan, options);
    }

    private static void ValidateCanonicalRoute(LanguagePlan plan)
    {
        if (!LanguageArtifactRoute.ContractsConnect(plan.Definition.EntryArtifact, StandardLanguageArtifactKinds.SourceText.Contract))
        {
            throw new InvalidOperationException(
                "The legacy Wist compatibility provider accepts only the canonical source.text<string> entry artifact.");
        }
        if (plan.RuntimeProviderContribution?.Contribution.Id != WistContributionIds.LegacyRuntimeAdapter)
            throw new InvalidOperationException("The Wist provider requires the canonical legacy runtime-adapter contribution.");

        foreach (var route in plan.Routes.Values)
        {
            var backendContribution = route.Backend == InterpreterBackend
                ? WistContributionIds.InterpreterBackend
                : route.Backend == CilBackend
                    ? WistContributionIds.CilBackend
                    : throw new InvalidOperationException($"Backend '{route.Backend.Value}' is not supported by the Wist compatibility provider.");
            var expected = new[]
            {
                WistContributionIds.Frontend,
                WistContributionIds.LoweringToAir,
                backendContribution
            };
            var actual = route.Steps.Select(static step => step.ContributionId).ToArray();
            if (!actual.SequenceEqual(expected))
            {
                throw new InvalidOperationException(
                    $"The Wist compatibility provider cannot execute custom artifact route '{string.Join(" -> ", actual.Select(static id => id.Value))}'.");
            }
        }
    }

    private sealed class WistLanguageRuntimeSession : ILanguageRuntimeSession
    {
        private readonly WistDialectExecutionHost _host;

        public WistLanguageRuntimeSession(LanguagePlan plan, LanguageRuntimeOptions options)
        {
            var dialectText = WistLegacyDialectAdapter.BuildDialectText(plan);
            var services = new ServiceCollection();
            services.AddWistDialectServices();
            using var provider = services.BuildServiceProvider();
            var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
            var composition = workflow.ComposeText(dialectText, $"{plan.Definition.Id.Value}.generated.wistdialect");
            if (!composition.IsSuccess)
            {
                var details = string.Join(
                    Environment.NewLine,
                    composition.SemanticDiagnostics.Concat(composition.ResolutionDiagnostics)
                        .Select(static diagnostic => $"[{diagnostic.Code}] {diagnostic.Message}"));
                throw new InvalidOperationException($"Generated Wist dialect could not be composed:{Environment.NewLine}{details}");
            }
            ValidateExactRuntimeSelection(plan, composition.RuntimeSelection as SelectedRuntimePlan);
            _host = workflow.CreateHost(
                composition,
                new WistRuntimeServiceOptions { AllowedAssemblies = options.AllowedAssemblies });
        }


        private static void ValidateExactRuntimeSelection(LanguagePlan plan, SelectedRuntimePlan? selectedRuntimePlan)
        {
            if (selectedRuntimePlan == null || !selectedRuntimePlan.IsResolved)
                throw new InvalidOperationException("Generated Wist dialect did not produce a resolved runtime selection.");

            var expectedModules = WistModuleSelection.GetModuleAliases(plan).ToHashSet(StringComparer.Ordinal);
            var actualModules = selectedRuntimePlan.OrderedModules
                .Select(static entry => entry.CanonicalAlias)
                .ToHashSet(StringComparer.Ordinal);
            if (!expectedModules.SetEquals(actualModules))
            {
                throw new InvalidOperationException(
                    $"Wist runtime module selection differs from the verified language plan. " +
                    $"Expected [{string.Join(", ", expectedModules.OrderBy(static x => x, StringComparer.Ordinal))}], " +
                    $"actual [{string.Join(", ", actualModules.OrderBy(static x => x, StringComparer.Ordinal))}].");
            }

            var expectedBackends = WistModuleSelection.GetExpectedRuntimeBackendAliases(plan);
            var actualBackends = selectedRuntimePlan.EnabledBackends
                .Select(static entry => entry.CanonicalAlias)
                .ToHashSet(StringComparer.Ordinal);
            if (!expectedBackends.SetEquals(actualBackends))
                throw new InvalidOperationException("Wist runtime backend selection differs from the verified language plan.");
            if (selectedRuntimePlan.EnabledOptimizers.Count != 0)
                throw new InvalidOperationException("The compatibility language plan did not select runtime optimizers, but the composed dialect did.");
        }

        public LanguageExecutionResult Run(LanguageExecutionRequest request)
        {
            var value = request.Arguments.Count == 0
                ? _host.Run(request.GetRequiredInput<string>(), request.Backend.Value)
                : _host.Run(request.GetRequiredInput<string>(), request.Arguments, request.Backend.Value);
            return new LanguageExecutionResult(request.Backend, value);
        }

        public void Dispose() => _host.Dispose();
    }
}

#pragma warning disable CS0618
[Obsolete("[UTL-DEP-005] Use WistLanguageRuntimeProvider with LanguageRuntimeProviderRegistry. Removal is blocked by the shipped-preset parity gate.")]
public sealed class WistLanguageRuntimePack : ILanguageRuntimePack, ILanguageRuntimePolicyValidator
{
    private static readonly WistLanguageFeaturePackage FeaturePackage = new();
    private readonly WistLanguageRuntimeProvider _provider = new();

    public LanguagePackageId PackageId => WistLanguageFeaturePackage.PackageId;
    public LanguageVersion PackageVersion => WistLanguageFeaturePackage.PackageVersion;
    public ToolchainApiVersion ToolchainApiVersion => ToolchainApi.Current;
    public IReadOnlyCollection<LanguageFeatureId> SupportedFeatures =>
        FeaturePackage.Descriptor.Features.Select(static feature => feature.Id).ToArray();
    public IReadOnlyCollection<BackendId> SupportedBackends => _provider.SupportedBackends;

    public void ValidatePolicy(LanguagePlan plan, LanguageRuntimePolicy policy, LanguageRuntimeOptions options) =>
        _provider.ValidatePolicy(plan, policy, options);

    public ILanguageRuntimeSession CreateSession(LanguagePlan plan, LanguageRuntimeOptions options) =>
        _provider.CreateSession(plan, options);
}
#pragma warning restore CS0618
