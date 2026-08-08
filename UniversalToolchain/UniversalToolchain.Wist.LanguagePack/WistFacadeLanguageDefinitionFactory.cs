using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.Wist.LanguagePack;

internal enum WistFacadeSsaPolicy
{
    Disabled,
    Prefer,
    Require,
    Debug
}

/// <summary>
/// Staged facade adapter. It produces only LanguageDefinition; LanguageCompiler remains the sole
/// resolver of dependencies, providers, routes and order. S10 replaces the temporary legacy DSL
/// parser used for text/file sources without changing this translation contract.
/// </summary>
internal static class WistFacadeLanguageDefinitionFactory
{
    public static LanguageDefinition FromPreset(
        string presetId,
        string backend,
        WistFacadeSsaPolicy ssaPolicy)
    {
        var baseline = WistLanguageDefinitions.Create(presetId);
        return Narrow(baseline, backend, ssaPolicy);
    }

    public static LanguageDefinition FromDialectText(
        string sourceText,
        string sourceName,
        string backend,
        WistFacadeSsaPolicy ssaPolicy)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        using var compiler = new DialectDslCompiler();
        var slice = compiler.Compile(sourceText);
        return FromSlice(slice, sourceName, backend, ssaPolicy);
    }

    private static LanguageDefinition FromSlice(
        DialectDefinitionSlice slice,
        string sourceName,
        string backend,
        WistFacadeSsaPolicy ssaPolicy)
    {
        ArgumentNullException.ThrowIfNull(slice);
        ArgumentException.ThrowIfNullOrWhiteSpace(backend);

        if (slice.BaseDialectName != null)
        {
            throw new NotSupportedException(
                "Wist facade LanguageDefinition translation does not inherit base dialects; S10 requires explicit feature ownership.");
        }
        if (slice.OrderDirectives.Count != 0 || slice.IntrinsicDirectives.Count != 0 || slice.CapabilityDirectives.Count != 0)
        {
            throw new NotSupportedException(
                "This Wist dialect uses directives whose canonical LanguageDefinition translation is not available in the S09 facade cutover.");
        }

        var selectedFeatures = new List<LanguageFeatureId>();
        var selectedAliases = new HashSet<string>(StringComparer.Ordinal);
        foreach (var alias in slice.UseModules)
        {
            var component = WistRuntimeComponentCatalog.GetRequiredAlias(alias, WistRuntimeComponentKind.Module);
            if (selectedAliases.Add(alias))
                selectedFeatures.Add(component.FeatureId);
        }
        foreach (var alias in slice.ExcludeModules)
        {
            _ = WistRuntimeComponentCatalog.GetRequiredAlias(alias, WistRuntimeComponentKind.Module);
            if (selectedAliases.Contains(alias))
                throw new InvalidOperationException($"Wist dialect both uses and excludes module '{alias}'.");
        }
        foreach (var optimizer in slice.OptimizerDirectives)
        {
            var component = WistRuntimeComponentCatalog.GetRequiredAlias(optimizer.Name, WistRuntimeComponentKind.Optimizer);
            if (optimizer.Enabled && !selectedFeatures.Contains(component.FeatureId))
                selectedFeatures.Add(component.FeatureId);
            if (!optimizer.Enabled)
                selectedFeatures.Remove(component.FeatureId);
        }

        var enabledBackends = slice.BackendDirectives
            .Where(static directive => directive.Enabled)
            .Select(static directive => directive.Backend.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (!enabledBackends.Contains(backend, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Wist dialect '{slice.Name}' does not enable requested backend '{backend}'. Enabled backends: {string.Join(", ", enabledBackends)}.");
        }

        var securityFeature = slice.SecurityProfile switch
        {
            DialectSecurityProfile.Trusted => WistInternalFeatureIds.TrustedSecurity,
            DialectSecurityProfile.Restricted or null => WistInternalFeatureIds.RestrictedSecurity,
            _ => throw new InvalidOperationException($"Unknown Wist security profile '{slice.SecurityProfile}'.")
        };
        var allowHostInterop = slice.SecurityProfile == DialectSecurityProfile.Trusted;
        selectedFeatures.Add(securityFeature);
        ApplySsaPolicy(selectedFeatures, ssaPolicy);

        return new LanguageDefinition(
            new LanguageId($"wist.dsl.{slice.Name}"),
            new LanguageVersion(slice.Version ?? WistLanguageFeaturePackage.PackageVersion.Value),
            ToolchainApi.Current,
            selectedFeatures,
            [new BackendId(backend)],
            runtimeProvider: new LanguageRuntimeProviderReference(
                WistLanguageFeaturePackage.RuntimeProviderId,
                WistLanguageFeaturePackage.PackageVersion),
            runtimePolicy: new LanguageRuntimePolicy(AllowHostInterop: allowHostInterop),
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["wist.source-name"] = sourceName,
                ["wist.dsl-name"] = slice.Name
            });
    }

    private static LanguageDefinition Narrow(
        LanguageDefinition baseline,
        string backend,
        WistFacadeSsaPolicy ssaPolicy)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentException.ThrowIfNullOrWhiteSpace(backend);
        var backendId = new BackendId(backend);
        if (!baseline.Backends.Contains(backendId))
        {
            throw new InvalidOperationException(
                $"Wist definition '{baseline.Id.Value}' does not enable requested backend '{backend}'.");
        }

        var selectedFeatures = baseline.SelectedFeatures
            .Where(feature => !WistSsaPolicyFeatureIds.All.Contains(feature))
            .Where(feature => feature != WistFeatureIds.SsaOptimization)
            .ToList();
        ApplySsaPolicy(selectedFeatures, ssaPolicy);

        return new LanguageDefinition(
            baseline.Id,
            baseline.Version,
            baseline.ToolchainApiVersion,
            selectedFeatures,
            [backendId],
            runtimeProvider: baseline.RuntimeProvider,
            runtimePolicy: baseline.RuntimePolicy,
            metadata: baseline.Metadata,
            slotOverrides: baseline.SlotOverrides,
            capabilityProviders: baseline.CapabilityProviders,
            excludedContributions: baseline.ExcludedContributions,
            entryArtifact: baseline.EntryArtifact);
    }

    private static void ApplySsaPolicy(List<LanguageFeatureId> selectedFeatures, WistFacadeSsaPolicy policy)
    {
        selectedFeatures.RemoveAll(feature => WistSsaPolicyFeatureIds.All.Contains(feature));
        selectedFeatures.Remove(WistFeatureIds.SsaOptimization);

        var policyFeature = policy switch
        {
            WistFacadeSsaPolicy.Disabled => WistSsaPolicyFeatureIds.Disabled,
            WistFacadeSsaPolicy.Prefer => WistSsaPolicyFeatureIds.Prefer,
            WistFacadeSsaPolicy.Require => WistSsaPolicyFeatureIds.Require,
            WistFacadeSsaPolicy.Debug => WistSsaPolicyFeatureIds.Debug,
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null)
        };
        selectedFeatures.Add(policyFeature);
        if (policy != WistFacadeSsaPolicy.Disabled)
            selectedFeatures.Add(WistFeatureIds.SsaOptimization);
    }
}
