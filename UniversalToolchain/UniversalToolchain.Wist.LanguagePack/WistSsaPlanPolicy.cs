using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Ssa.Optimization;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistSsaPolicyFeatureIds
{
    public static LanguageFeatureId Disabled { get; } = new("wist.policy.ssa.disabled");
    public static LanguageFeatureId Prefer { get; } = new("wist.policy.ssa.prefer");
    public static LanguageFeatureId Require { get; } = new("wist.policy.ssa.require");
    public static LanguageFeatureId Debug { get; } = new("wist.policy.ssa.debug");

    public static IReadOnlyList<LanguageFeatureId> All { get; } =
    [
        Disabled,
        Prefer,
        Require,
        Debug
    ];
}

internal static class WistSsaPlanPolicy
{
    public static SsaRoutePolicy GetRequiredPolicy(LanguagePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var selected = plan.Features
            .Where(static feature => WistSsaPolicyFeatureIds.All.Contains(feature.Feature.Id))
            .Select(static feature => feature.Feature.Id)
            .ToArray();
        var selectsSsaPass = plan.Contributions.Any(static contribution =>
            contribution.Contribution.Id == WistContributionIds.SsaOptimizer);

        if (selected.Length == 0)
        {
            if (selectsSsaPass)
            {
                throw new InvalidOperationException(
                    "Wist LanguagePlan selects the SSA optimizer contribution but does not select an explicit typed SSA policy feature.");
            }
            return SsaRoutePolicy.Off;
        }
        if (selected.Length != 1)
        {
            throw new InvalidOperationException(
                $"Wist LanguagePlan must select at most one typed SSA policy feature, but {selected.Length} were selected.");
        }

        var policy = selected[0] == WistSsaPolicyFeatureIds.Disabled
            ? SsaRoutePolicy.Off
            : selected[0] == WistSsaPolicyFeatureIds.Prefer
                ? SsaRoutePolicy.Prefer
                : selected[0] == WistSsaPolicyFeatureIds.Require
                    ? SsaRoutePolicy.Require
                    : selected[0] == WistSsaPolicyFeatureIds.Debug
                        ? SsaRoutePolicy.Debug
                        : throw new InvalidOperationException($"Unknown Wist SSA policy feature '{selected[0].Value}'.");

        if (policy == SsaRoutePolicy.Off && selectsSsaPass)
            throw new InvalidOperationException("Wist LanguagePlan disables SSA but still selects the SSA optimizer contribution.");
        if (policy != SsaRoutePolicy.Off && !selectsSsaPass)
            throw new InvalidOperationException("Wist LanguagePlan requests SSA but does not select the SSA optimizer contribution.");

        return policy;
    }

    public static SsaRuntimeExecutionOptions CreateRuntimeOptions(
        LanguagePlan plan,
        bool captureTrace = false)
    {
        var policy = GetRequiredPolicy(plan);
        return new SsaRuntimeExecutionOptions
        {
            Policy = policy,
            Diagnostics = policy == SsaRoutePolicy.Debug || captureTrace && policy != SsaRoutePolicy.Off
                ? SsaDiagnosticMode.Verbose
                : SsaDiagnosticMode.Default,
            ProfileId = SsaRuntimeExecutionDefaults.ProfileId
        };
    }
}
