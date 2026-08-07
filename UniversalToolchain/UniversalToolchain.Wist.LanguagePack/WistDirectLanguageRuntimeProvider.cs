using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.Wist.LanguagePack;

/// <summary>
/// Wist policy wrapper over the generic exact-route runtime. It validates Wist-specific policy
/// facts but delegates all artifact routing, component materialization, lifecycle and execution to
/// <see cref="LanguageRouteRuntimeProvider"/>.
/// </summary>
internal sealed class WistDirectLanguageRuntimeProvider : ILanguageRuntimeProvider, ILanguageRuntimePolicyValidator
{
    private readonly LanguagePlan _plan;
    private readonly LanguageRouteRuntimeProvider _inner;

    public WistDirectLanguageRuntimeProvider(LanguagePlan plan, WistLanguageFeaturePackage package)
    {
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        ArgumentNullException.ThrowIfNull(package);
        _inner = LanguageRouteRuntimeAssembler.CreateProvider(plan, [package]);
    }

    public LanguageRuntimeProviderId ProviderId => _inner.ProviderId;
    public LanguageVersion ProviderVersion => _inner.ProviderVersion;
    public ToolchainApiVersion ToolchainApiVersion => _inner.ToolchainApiVersion;
    public LanguageContributionId RuntimeContributionId => _inner.RuntimeContributionId;
    public IReadOnlyCollection<BackendId> SupportedBackends => _inner.SupportedBackends;

    public void ValidatePolicy(LanguagePlan plan, LanguageRuntimePolicy policy, LanguageRuntimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(options);
        if (!ReferenceEquals(plan, _plan) && plan.PlanHash != _plan.PlanHash)
            throw new InvalidOperationException("Direct Wist runtime provider is bound to a different LanguagePlan.");

        WistModuleSelection.ValidateCanonicalPackageProvenance(plan);
        _ = WistSsaPlanPolicy.GetRequiredPolicy(plan);

        var selectsCSharpInterop = plan.Contributions.Any(static contribution =>
            contribution.Contribution.Id == WistContributionIds.CSharpInteropModule);
        if (!policy.AllowHostInterop && (selectsCSharpInterop || options.AllowedAssemblies.Count != 0))
        {
            throw new InvalidOperationException(
                "The Wist language plan forbids host interop, but CSharp interop or allowed host assemblies were selected.");
        }

        _inner.ValidatePolicy(plan, policy, options);
    }

    public ILanguageRuntimeSession CreateSession(LanguagePlan plan, LanguageRuntimeOptions options)
    {
        ValidatePolicy(plan, plan.Definition.RuntimePolicy, options);
        return _inner.CreateSession(plan, options);
    }
}
