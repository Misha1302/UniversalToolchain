using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.Wist.LanguagePack;

/// <summary>
/// Public Wist runtime provider over the canonical exact-route runtime.
/// LanguageCompiler owns all semantic decisions; this provider recovers the exact package instance
/// captured by LanguagePlan provenance and materializes only the already-planned route.
/// </summary>
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
        CreateDirectProvider(plan).ValidatePolicy(plan, policy, options);
    }

    public ILanguageRuntimeSession CreateSession(LanguagePlan plan, LanguageRuntimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(options);
        var provider = CreateDirectProvider(plan);
        provider.ValidatePolicy(plan, plan.Definition.RuntimePolicy, options);
        return provider.CreateSession(plan, options);
    }

    private static WistDirectLanguageRuntimeProvider CreateDirectProvider(LanguagePlan plan)
    {
        WistModuleSelection.ValidateCanonicalPackageProvenance(plan);
        var runtimeContribution = plan.RuntimeProviderContribution
            ?? throw new InvalidOperationException("Wist LanguagePlan has no runtime-provider contribution.");
        if (runtimeContribution.Contribution.Id != WistContributionIds.RuntimeProvider ||
            runtimeContribution.PackageId != WistLanguageFeaturePackage.PackageId ||
            runtimeContribution.PackageVersion != WistLanguageFeaturePackage.PackageVersion)
        {
            throw new InvalidOperationException(
                "Wist runtime provider requires the canonical Wist runtime-provider contribution and package identity.");
        }

        var package = runtimeContribution.PackageIdentity
            .GetRequiredImplementation<WistLanguageFeaturePackage>();
        return new WistDirectLanguageRuntimeProvider(plan, package);
    }
}
