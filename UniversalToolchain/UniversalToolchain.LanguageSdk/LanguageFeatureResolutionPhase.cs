using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.LanguageSdk;

internal sealed class LanguageFeatureResolutionPhase(LanguagePackageRegistry registry)
{
    private readonly LanguagePackageRegistry _registry = registry ?? throw new ArgumentNullException(nameof(registry));

    public IReadOnlyList<ResolvedLanguageFeature> Resolve(
        LanguageDefinition definition,
        List<LanguageDiagnostic> diagnostics)
    {
        var states = new Dictionary<LanguageFeatureId, VisitState>();
        var resolved = new List<ResolvedLanguageFeature>();
        foreach (var selected in definition.SelectedFeatures.OrderBy(static x => x.Value, StringComparer.Ordinal))
            Visit(selected, definition, states, resolved, diagnostics, []);
        ValidateCompatibility(definition, resolved, diagnostics);
        return resolved;
    }

    private void Visit(
        LanguageFeatureId id,
        LanguageDefinition definition,
        Dictionary<LanguageFeatureId, VisitState> states,
        List<ResolvedLanguageFeature> output,
        List<LanguageDiagnostic> diagnostics,
        IReadOnlyList<LanguageFeatureId> chain)
    {
        if (states.TryGetValue(id, out var state))
        {
            if (state == VisitState.Visited)
                return;
            if (state == VisitState.Visiting)
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL1003", "planning",
                    $"Feature dependency cycle: {string.Join(" -> ", chain.Select(static x => x.Value).Append(id.Value))}.",
                    id.Value, "Break the dependency cycle."));
            }
            return;
        }

        if (!_registry.TryGetFeatureRegistration(id, out var package, out var feature, out var packageIdentity))
        {
            diagnostics.Add(LanguagePlanningDiagnostics.Error(
                "UTL1001", "planning", $"Required feature '{id.Value}' is not registered.",
                chain.Count == 0 ? null : chain[^1].Value,
                "Register a package that owns the missing feature."));
            return;
        }
        if (package.ToolchainApiVersion != definition.ToolchainApiVersion)
        {
            diagnostics.Add(LanguagePlanningDiagnostics.Error(
                "UTL1501", "planning",
                $"Feature package '{package.Id.Value}' targets Toolchain API {package.ToolchainApiVersion.Major}, language targets {definition.ToolchainApiVersion.Major}.",
                package.Id.Value,
                "Use a feature package compatible with the language Toolchain API."));
            return;
        }

        states[id] = VisitState.Visiting;
        var nextChain = chain.Append(id).ToArray();
        foreach (var dependency in feature.Requires.OrderBy(static x => x.Value, StringComparer.Ordinal))
            Visit(dependency, definition, states, output, diagnostics, nextChain);
        states[id] = VisitState.Visited;
        if (!output.Any(x => x.Feature.Id == id))
            output.Add(new ResolvedLanguageFeature(packageIdentity, feature));
    }

    private static void ValidateCompatibility(
        LanguageDefinition definition,
        IReadOnlyList<ResolvedLanguageFeature> features,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var resolvedIds = features.Select(static x => x.Feature.Id).ToHashSet();
        foreach (var item in features)
        {
            foreach (var conflict in item.Feature.Conflicts)
            {
                if (resolvedIds.Contains(conflict))
                {
                    diagnostics.Add(LanguagePlanningDiagnostics.Error(
                        "UTL1002", "planning",
                        $"Feature '{item.Feature.Id.Value}' conflicts with selected feature '{conflict.Value}'.",
                        item.PackageId.Value,
                        "Remove one of the conflicting features."));
                }
            }
            foreach (var backend in definition.Backends)
            {
                if (item.Feature.SupportedBackends.Count != 0 && !item.Feature.SupportedBackends.Contains(backend))
                {
                    diagnostics.Add(LanguagePlanningDiagnostics.Error(
                        "UTL1203", "planning",
                        $"Backend '{backend.Value}' is not supported by feature '{item.Feature.Id.Value}'.",
                        item.PackageId.Value,
                        "Select a supported backend or remove the feature."));
                }
            }
        }
    }

    private enum VisitState
    {
        Visiting,
        Visited
    }
}
