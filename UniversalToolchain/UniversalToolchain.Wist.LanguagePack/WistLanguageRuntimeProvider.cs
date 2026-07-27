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
    public LanguageContributionId RuntimeContributionId => WistContributionIds.RuntimeProvider;
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
                "The Wist runtime provider cannot satisfy required determinism evidence for its composed module pipeline.");
        }
        var selectedModules = WistModuleSelection.GetModuleAliases(plan);
        var selectsCSharpInterop = selectedModules.Contains("CSharpInterop", StringComparer.Ordinal);
        var unsafeInteropDirective = ReadBooleanMetadata(plan, "wist.capability.unsafe-interop");

        if (!policy.AllowHostInterop &&
            (selectsCSharpInterop || unsafeInteropDirective == true || options.AllowedAssemblies.Count != 0))
        {
            throw new InvalidOperationException(
                "The Wist language plan forbids host interop, but CSharp interop, the unsafe-interop capability, or allowed host assemblies were selected.");
        }
        if (selectsCSharpInterop && unsafeInteropDirective == false)
        {
            throw new InvalidOperationException(
                "The Wist language plan selects CSharpInterop while explicitly disabling the unsafe-interop capability.");
        }
    }

    public ILanguageRuntimeSession CreateSession(LanguagePlan plan, LanguageRuntimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(options);
        // CreateSession is public through ILanguageRuntimeProvider. Revalidate here so
        // callers cannot bypass policy enforcement by skipping LanguageRuntime.Create.
        ValidatePolicy(plan, plan.Definition.RuntimePolicy, options);
        return new WistLanguageRuntimeSession(plan, options);
    }


    private static bool? ReadBooleanMetadata(LanguagePlan plan, string key)
    {
        if (!plan.Definition.Metadata.TryGetValue(key, out var value))
            return null;
        return bool.TryParse(value, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Wist metadata '{key}' must contain a Boolean value.");
    }

    private static void ValidateCanonicalRoute(LanguagePlan plan)
    {
        if (!LanguageArtifactRoute.ContractsConnect(plan.Definition.EntryArtifact, StandardLanguageArtifactKinds.SourceText.Contract))
        {
            throw new InvalidOperationException(
                "The Wist runtime provider accepts only the canonical source.text<string> entry artifact.");
        }
        if (plan.RuntimeProviderContribution?.Contribution.Id != WistContributionIds.RuntimeProvider)
            throw new InvalidOperationException("The Wist provider requires the canonical runtime-provider contribution.");

        foreach (var route in plan.Routes.Values)
        {
            var backendContribution = route.Backend == InterpreterBackend
                ? WistContributionIds.InterpreterBackend
                : route.Backend == CilBackend
                    ? WistContributionIds.CilBackend
                    : throw new InvalidOperationException($"Backend '{route.Backend.Value}' is not supported by Wist.");
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
                    $"The Wist provider cannot execute custom artifact route '{string.Join(" -> ", actual.Select(static id => id.Value))}'.");
            }
        }
    }

    private sealed class WistLanguageRuntimeSession : ILanguageRuntimeSession
    {
        private readonly WistDialectExecutionHost _host;

        public WistLanguageRuntimeSession(LanguagePlan plan, LanguageRuntimeOptions options)
        {
            var dialectDefinition = WistDialectPlanFactory.Create(plan);
            var services = new ServiceCollection();
            services.AddWistDialectServices();
            ServiceProvider? provider = services.BuildServiceProvider();
            try
            {
                var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
                var composition = workflow.ComposeDefinition(
                    $"{plan.Definition.Id.Value}.typed",
                    dialectDefinition);
                if (!composition.IsSuccess)
                {
                    var details = string.Join(
                        Environment.NewLine,
                        composition.SemanticDiagnostics.Concat(composition.ResolutionDiagnostics)
                            .Select(static diagnostic => $"[{diagnostic.Code}] {diagnostic.Message}"));
                    throw new InvalidOperationException($"Typed Wist plan could not be composed:{Environment.NewLine}{details}");
                }

                ValidateExactRuntimeSelection(plan, composition.RuntimeSelection as SelectedRuntimePlan);

                // The composition provider owns the runtime assembly load strategy used by
                // the selected components. Transfer that ownership to the returned host so
                // its collectible AssemblyLoadContext remains alive for the entire session.
                var owner = provider;
                provider = null;
                _host = workflow.CreateHost(
                    composition,
                    new WistRuntimeServiceOptions { AllowedAssemblies = options.AllowedAssemblies },
                    owner);
            }
            finally
            {
                provider?.Dispose();
            }
        }

        private static void ValidateExactRuntimeSelection(LanguagePlan plan, SelectedRuntimePlan? selectedRuntimePlan)
        {
            if (selectedRuntimePlan == null || !selectedRuntimePlan.IsResolved)
                throw new InvalidOperationException("Typed Wist plan did not produce a resolved runtime selection.");

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

            var expectedOptimizers = WistModuleSelection.GetOptimizerAliases(plan).ToHashSet(StringComparer.Ordinal);
            var actualOptimizers = selectedRuntimePlan.EnabledOptimizers
                .Select(static entry => entry.CanonicalAlias)
                .ToHashSet(StringComparer.Ordinal);
            if (!expectedOptimizers.SetEquals(actualOptimizers))
                throw new InvalidOperationException("Wist runtime optimizer selection differs from the verified language plan.");

            var expectedBackends = WistModuleSelection.GetExpectedRuntimeBackendAliases(plan);
            var actualBackends = selectedRuntimePlan.EnabledBackends
                .Select(static entry => entry.CanonicalAlias)
                .ToHashSet(StringComparer.Ordinal);
            if (!expectedBackends.SetEquals(actualBackends))
                throw new InvalidOperationException("Wist runtime backend selection differs from the verified language plan.");
        }

        public LanguageExecutionResult Run(LanguageExecutionRequest request)
        {
            var value = request.Arguments.Count == 0
                ? _host.Run(request.GetRequiredInput<string>(), request.Backend.Value)
                : _host.Run(request.GetRequiredInput<string>(), request.Arguments, request.Backend.Value);
            return new LanguageExecutionResult(request.Backend, WistRuntimeValueNormalizer.Normalize(value));
        }

        public void Dispose() => _host.Dispose();
    }
}
