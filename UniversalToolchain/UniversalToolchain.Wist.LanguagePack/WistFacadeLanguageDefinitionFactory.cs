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
/// Wist configuration frontend. It translates parsed dialect semantics into LanguageDefinition;
/// LanguageCompiler remains the sole resolver of dependencies, providers, routes and order.
/// </summary>
internal static class WistFacadeLanguageDefinitionFactory
{
    private const string UnsafeInteropCapability = "unsafe-interop";
    private const string CompositionRestrictedCapability = "composition-restricted";

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
                "Wist facade LanguageDefinition translation does not inherit base dialects; base-dialect ownership must be translated before planning.");
        }

        var groups = WistDialectGroupCatalog.Groups;
        var selectedFeatures = new List<LanguageFeatureId>();
        var selectedAliases = new HashSet<string>(StringComparer.Ordinal);
        var excludedAliases = ExpandModuleAliases(slice.ExcludeModules, groups).ToHashSet(StringComparer.Ordinal);
        var excludedContributions = excludedAliases
            .Select(alias => WistRuntimeComponentCatalog
                .GetRequiredAlias(alias, WistRuntimeComponentKind.Module)
                .ContributionId)
            .Distinct()
            .ToArray();

        foreach (var alias in ExpandModuleAliases(slice.UseModules, groups))
        {
            if (excludedAliases.Contains(alias))
                throw new InvalidOperationException($"Wist dialect both uses and excludes module '{alias}'.");

            var component = WistRuntimeComponentCatalog.GetRequiredAlias(alias, WistRuntimeComponentKind.Module);
            if (selectedAliases.Add(alias))
                selectedFeatures.Add(component.FeatureId);
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
            throw new ArgumentOutOfRangeException(
                nameof(backend),
                backend,
                $"Wist dialect '{slice.Name}' does not enable backend '{backend}'. Enabled backends: {string.Join(", ", enabledBackends)}.");
        }

        var capabilityPolicy = NormalizeCapabilities(slice.CapabilityDirectives);
        var securityFeature = slice.SecurityProfile switch
        {
            DialectSecurityProfile.Trusted => WistInternalFeatureIds.TrustedSecurity,
            DialectSecurityProfile.Restricted or null => WistInternalFeatureIds.RestrictedSecurity,
            _ => throw new InvalidOperationException($"Unknown Wist security profile '{slice.SecurityProfile}'.")
        };
        var allowHostInterop = slice.SecurityProfile == DialectSecurityProfile.Trusted;
        if (capabilityPolicy.TryGetValue(UnsafeInteropCapability, out var unsafeInterop))
        {
            if (unsafeInterop && slice.SecurityProfile != DialectSecurityProfile.Trusted)
            {
                var location = slice.CapabilityDirectives
                    .LastOrDefault(static directive => directive.Name == UnsafeInteropCapability && directive.Value)
                    ?.SourceLocation;
                DialectDefinitionTranslationErrors.Fail(
                    "Wist capability 'unsafe-interop' requires security trusted; restricted dialects cannot enable host interop.",
                    location);
                throw new InvalidOperationException("Unreachable dialect translation error path.");
            }
            allowHostInterop = unsafeInterop;
        }
        if (capabilityPolicy.GetValueOrDefault(CompositionRestrictedCapability))
            selectedFeatures.Add(WistInternalFeatureIds.CompositionRestricted);

        selectedFeatures.Add(securityFeature);
        ApplySsaPolicy(selectedFeatures, ssaPolicy);

        var orderConstraints = slice.OrderDirectives
            .Select(directive => new LanguageContributionOrderConstraint(
                directive.Kind switch
                {
                    DialectOrderDirectiveKind.Requires => LanguageContributionOrderKind.Requires,
                    DialectOrderDirectiveKind.Before => LanguageContributionOrderKind.Before,
                    DialectOrderDirectiveKind.After => LanguageContributionOrderKind.After,
                    _ => throw new InvalidOperationException($"Unknown Wist order directive '{directive.Kind}'.")
                },
                ResolveOrderedModuleContribution(directive.SourceModule, groups, directive.SourceLocation),
                ResolveOrderedModuleContribution(directive.TargetModule, groups, directive.SourceLocation)))
            .ToArray();
        var intrinsicPolicy = slice.IntrinsicDirectives
            .Select(directive =>
            {
                if (!directive.Target.IsAny && !enabledBackends.Contains(directive.Target.BackendId.Value, StringComparer.Ordinal))
                {
                    DialectDefinitionTranslationErrors.Fail(
                        $"Wist intrinsic policy for '{directive.Name}' targets disabled backend '{directive.Target.BackendId.Value}'.",
                        directive.SourceLocation);
                    throw new InvalidOperationException("Unreachable dialect translation error path.");
                }

                return new LanguageIntrinsicPolicyDirective(
                    new LanguageIntrinsicId(directive.Name),
                    directive.Allowed,
                    directive.Target.IsAny ? null : new BackendId(directive.Target.BackendId.Value));
            })
            .ToArray();

        return new LanguageDefinition(
            new LanguageId($"wist.dsl.{slice.Name}"),
            new LanguageVersion(slice.Version ?? WistLanguageFeaturePackage.PackageVersion.Value),
            ToolchainApi.Current,
            selectedFeatures,
            [new BackendId(backend)],
            new LanguageRuntimeProviderReference(
                WistLanguageFeaturePackage.RuntimeProviderId,
                WistLanguageFeaturePackage.PackageVersion),
            new LanguageRuntimePolicy(AllowHostInterop: allowHostInterop),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["wist.source-name"] = sourceName,
                ["wist.dsl-name"] = slice.Name
            },
            null,
            null,
            excludedContributions,
            StandardLanguageArtifactKinds.SourceText.Contract,
            orderConstraints,
            intrinsicPolicy);
    }

    private static IEnumerable<string> ExpandModuleAliases(
        IEnumerable<string> aliases,
        IReadOnlyDictionary<string, IReadOnlyList<string>> groups)
    {
        foreach (var alias in aliases)
        {
            if (groups.TryGetValue(alias, out var includedModules))
            {
                foreach (var included in includedModules)
                    yield return included;
                continue;
            }

            yield return alias;
        }
    }

    private static LanguageContributionId ResolveOrderedModuleContribution(
        string alias,
        IReadOnlyDictionary<string, IReadOnlyList<string>> groups,
        DialectSourceLocation? sourceLocation)
    {
        if (groups.ContainsKey(alias))
        {
            DialectDefinitionTranslationErrors.Fail(
                $"Wist order directive cannot target group '{alias}' as one contribution; order the expanded module aliases explicitly.",
                sourceLocation);
            throw new InvalidOperationException("Unreachable dialect translation error path.");
        }

        if (!WistRuntimeComponentCatalog.TryGetAlias(alias, WistRuntimeComponentKind.Module, out var component))
        {
            DialectDefinitionTranslationErrors.Fail(
                $"Wist alias '{alias}' is not a canonical module component.",
                sourceLocation);
            throw new InvalidOperationException("Unreachable dialect translation error path.");
        }

        return component!.ContributionId;
    }

    private static IReadOnlyDictionary<string, bool> NormalizeCapabilities(
        IReadOnlyList<DialectCapabilityDirective> directives)
    {
        var result = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var directive in directives)
        {
            if (directive.Name is not UnsafeInteropCapability and not CompositionRestrictedCapability)
            {
                DialectDefinitionTranslationErrors.Fail(
                    $"Wist capability '{directive.Name}' has no typed LanguageDefinition policy mapping.",
                    directive.SourceLocation);
                continue;
            }

            if (result.TryGetValue(directive.Name, out var existing) && existing != directive.Value)
            {
                DialectDefinitionTranslationErrors.Fail(
                    $"Wist capability '{directive.Name}' is declared with contradictory values.",
                    directive.SourceLocation);
                continue;
            }
            result[directive.Name] = directive.Value;
        }
        return result;
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
            throw new ArgumentOutOfRangeException(
                nameof(backend),
                backend,
                $"Wist definition '{baseline.Id.Value}' does not enable backend '{backend}'.");
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
            baseline.RuntimeProvider,
            baseline.RuntimePolicy,
            baseline.Metadata,
            baseline.SlotOverrides,
            baseline.CapabilityProviders,
            baseline.ExcludedContributions,
            baseline.EntryArtifact,
            baseline.ContributionOrderConstraints,
            baseline.IntrinsicPolicy);
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
