using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Capabilities.Core;

public static class DialectFeatureExplanationProjector
{
    public static DialectFeatureExplanation Project(
        CapabilityCatalog knownCapabilityCatalog,
        CapabilityCatalog selectedCapabilityCatalog,
        SelectedRuntimePlan selectedRuntimePlan,
        string dialectName)
    {
        ArgumentNullException.ThrowIfNull(knownCapabilityCatalog);
        ArgumentNullException.ThrowIfNull(selectedCapabilityCatalog);
        ArgumentNullException.ThrowIfNull(selectedRuntimePlan);
        ArgumentException.ThrowIfNullOrWhiteSpace(dialectName);

        var availableFeatureIds = DetermineAvailableFeatureIds(selectedCapabilityCatalog, selectedRuntimePlan);
        var availableFeatures = selectedCapabilityCatalog.LanguageFeatures
            .Where(x => availableFeatureIds.Contains(x.FeatureId))
            .ToList();
        var unavailableKnownFeatures = knownCapabilityCatalog.LanguageFeatures
            .Where(x => !availableFeatureIds.Contains(x.FeatureId))
            .Select(x => new DialectFeatureExplanation.UnavailableFeatureExplanation(
                x,
                BuildUnavailableReasons(x, knownCapabilityCatalog, selectedCapabilityCatalog, selectedRuntimePlan, availableFeatureIds)))
            .ToList();
        var availableSymbols = availableFeatures
            .SelectMany(static x => x.ProvidedSymbols)
            .OrderBy(static x => x.Name, StringComparer.Ordinal)
            .ThenBy(static x => x.Kind)
            .ThenBy(static x => x.Signature, StringComparer.Ordinal)
            .ToList();
        var availableFunctions = selectedCapabilityCatalog.BuiltinFunctionDescriptors
            .Where(x => availableFeatureIds.Contains(x.FeatureId))
            .ToList();
        var backendSupport = selectedRuntimePlan.EnabledBackends
            .Select(static x => x.CanonicalAlias)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();

        return new DialectFeatureExplanation(
            dialectName,
            availableFeatures,
            unavailableKnownFeatures,
            availableSymbols,
            availableFunctions,
            backendSupport);
    }

    internal static IReadOnlySet<LanguageFeatureId> DetermineAvailableFeatureIds(
        CapabilityCatalog selectedCapabilityCatalog,
        SelectedRuntimePlan selectedRuntimePlan)
    {
        ArgumentNullException.ThrowIfNull(selectedCapabilityCatalog);
        ArgumentNullException.ThrowIfNull(selectedRuntimePlan);

        var selectedAliases = GetSelectedRuntimeComponentAliases(selectedRuntimePlan);
        var availableFeatureIds = new HashSet<LanguageFeatureId>();
        var pendingFeatures = selectedCapabilityCatalog.LanguageFeatures.ToList();
        var changed = true;

        while (changed)
        {
            changed = false;

            foreach (var feature in pendingFeatures)
            {
                if (availableFeatureIds.Contains(feature.FeatureId))
                    continue;

                if (!selectedCapabilityCatalog.TryGetOwningProvider(feature.FeatureId, out var owner))
                    continue;

                if (!selectedCapabilityCatalog.ContainsProvider(owner.ProviderType))
                    continue;

                if (!feature.RequiredRuntimeComponentAliases.All(selectedAliases.Contains))
                    continue;

                if (!feature.RequiredFeatures.All(availableFeatureIds.Contains))
                    continue;

                changed = availableFeatureIds.Add(feature.FeatureId) || changed;
            }
        }

        return availableFeatureIds;
    }

    private static IReadOnlyList<string> BuildUnavailableReasons(
        LanguageFeatureDescriptor feature,
        CapabilityCatalog knownCapabilityCatalog,
        CapabilityCatalog selectedCapabilityCatalog,
        SelectedRuntimePlan selectedRuntimePlan,
        IReadOnlySet<LanguageFeatureId> availableFeatureIds)
    {
        var reasons = new List<string>();

        if (knownCapabilityCatalog.TryGetOwningProvider(feature.FeatureId, out var owner) &&
            !selectedCapabilityCatalog.ContainsProvider(owner.ProviderType))
        {
            reasons.Add($"Owning provider '{CapabilityProviderTypeResolver.GetTypeName(owner.ProviderType)}' is not selected.");
        }

        var selectedAliases = GetSelectedRuntimeComponentAliases(selectedRuntimePlan);
        var missingRuntimeAliases = feature.RequiredRuntimeComponentAliases
            .Where(x => !selectedAliases.Contains(x))
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
        if (missingRuntimeAliases.Count > 0)
            reasons.Add($"Required runtime components are not selected: {string.Join(", ", missingRuntimeAliases)}.");

        var missingFeatures = feature.RequiredFeatures
            .Where(x => !availableFeatureIds.Contains(x))
            .Select(static x => x.Value)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
        if (missingFeatures.Count > 0)
            reasons.Add($"Required features are not available: {string.Join(", ", missingFeatures)}.");

        if (reasons.Count == 0)
            reasons.Add("Feature is not selected.");

        return reasons;
    }

    private static HashSet<string> GetSelectedRuntimeComponentAliases(SelectedRuntimePlan selectedRuntimePlan)
    {
        return selectedRuntimePlan.OrderedModules
            .Concat(selectedRuntimePlan.EnabledOptimizers)
            .Concat(selectedRuntimePlan.EnabledBackends)
            .SelectMany(static x => x.AllAliases)
            .ToHashSet(StringComparer.Ordinal);
    }
}
