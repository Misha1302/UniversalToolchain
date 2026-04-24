using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Features.Abstractions;

namespace UniversalToolchain.Features.Core;

public sealed class DialectFeatureExplanationProjector
{
    private readonly ILanguageFeatureCatalog _catalog;

    public DialectFeatureExplanationProjector(ILanguageFeatureCatalog catalog)
    {
        catalog = catalog.ArgNotNull();

        _catalog = catalog;
    }

    public DialectFeatureExplanation Project(DialectFrameworkCompositionResult composition)
    {
        composition = composition.ArgNotNull();

        if (composition.BuildPlan == null)
        {
            Thrower.Argument(nameof(composition), "Dialect composition result must contain a build plan.");
        }

        if (composition.RuntimeSelection is not SelectedRuntimePlan runtimeSelection)
        {
            Thrower.Argument(nameof(composition), "Dialect composition result must contain a selected runtime plan.");
        }

        var featureDescriptors = _catalog.GetFeatures()
            .OrderBy(static x => x.FeatureId.Value, StringComparer.Ordinal)
            .ToArray();
        var selectedAliases = BuildSelectedAliasSet(runtimeSelection);
        var evaluations = new Dictionary<LanguageFeatureId, FeatureEvaluation>();

        foreach (var descriptor in featureDescriptors)
        {
            EvaluateFeature(descriptor, selectedAliases, evaluations, []);
        }

        var availableFeatures = featureDescriptors
            .Select(static x => x.FeatureId)
            .Where(evaluations.ContainsKey)
            .Select(x => evaluations[x])
            .Where(static x => x.IsAvailable)
            .Select(static x => new AvailableLanguageFeature(x.Descriptor))
            .ToArray();
        var unavailableFeatures = featureDescriptors
            .Select(static x => x.FeatureId)
            .Where(evaluations.ContainsKey)
            .Select(x => evaluations[x])
            .Where(static x => !x.IsAvailable)
            .Select(static x => new UnavailableLanguageFeature(x.Descriptor, x.Reasons))
            .ToArray();
        var availableSymbols = availableFeatures
            .SelectMany(static x => x.Descriptor.ProvidedSymbols)
            .OrderBy(static x => x.Kind.ToString(), StringComparer.Ordinal)
            .ThenBy(static x => x.Name, StringComparer.Ordinal)
            .ThenBy(static x => x.Signature, StringComparer.Ordinal)
            .ThenBy(static x => x.Description, StringComparer.Ordinal)
            .ToArray();
        var backendSupport = runtimeSelection.EnabledBackends
            .Select(backend => new DialectFeatureBackendSupport(
                backend.CanonicalAlias,
                availableFeatures
                    .Where(feature => SupportsBackend(feature.Descriptor, backend))
                    .Select(static x => x.Descriptor.FeatureId)
                    .ToArray()))
            .ToArray();

        return new DialectFeatureExplanation(
            composition.BuildPlan.Name,
            availableFeatures,
            unavailableFeatures,
            availableSymbols,
            backendSupport);
    }

    private FeatureEvaluation EvaluateFeature(
        LanguageFeatureDescriptor descriptor,
        IReadOnlySet<string> selectedAliases,
        IDictionary<LanguageFeatureId, FeatureEvaluation> evaluations,
        IReadOnlyCollection<LanguageFeatureId> evaluationPath)
    {
        if (evaluations.TryGetValue(descriptor.FeatureId, out var cached))
        {
            return cached;
        }

        if (evaluationPath.Contains(descriptor.FeatureId))
        {
            var cycleReason = $"Feature dependency cycle detected at '{descriptor.FeatureId.Value}'.";
            var cycleEvaluation = new FeatureEvaluation(descriptor, false, [cycleReason]);
            evaluations[descriptor.FeatureId] = cycleEvaluation;
            return cycleEvaluation;
        }

        var reasons = new List<string>();

        foreach (var requiredAlias in descriptor.RequiredRuntimeComponentAliases.OrderBy(static x => x, StringComparer.Ordinal))
        {
            if (!selectedAliases.Contains(requiredAlias))
            {
                reasons.Add($"Required runtime component alias '{requiredAlias}' is not selected.");
            }
        }

        var nextPath = evaluationPath.Append(descriptor.FeatureId).ToArray();
        foreach (var requiredFeatureId in descriptor.RequiredFeatures.OrderBy(static x => x.Value, StringComparer.Ordinal))
        {
            if (!_catalog.TryGetFeature(requiredFeatureId, out var requiredFeatureDescriptor) || requiredFeatureDescriptor == null)
            {
                reasons.Add($"Required feature '{requiredFeatureId.Value}' is not present in the catalog.");
                continue;
            }

            var requiredFeature = EvaluateFeature(requiredFeatureDescriptor, selectedAliases, evaluations, nextPath);
            if (!requiredFeature.IsAvailable)
            {
                reasons.Add($"Required feature '{requiredFeatureId.Value}' is not available.");
            }
        }

        var evaluation = new FeatureEvaluation(
            descriptor,
            reasons.Count == 0,
            reasons.ToArray());
        evaluations[descriptor.FeatureId] = evaluation;
        return evaluation;
    }

    private static IReadOnlySet<string> BuildSelectedAliasSet(SelectedRuntimePlan runtimeSelection)
    {
        var aliases = new SortedSet<string>(StringComparer.Ordinal);

        AddAliases(runtimeSelection.OrderedModules, aliases);
        AddAliases(runtimeSelection.EnabledOptimizers, aliases);
        AddAliases(runtimeSelection.EnabledBackends, aliases);

        return aliases;
    }

    private static void AddAliases(IEnumerable<RuntimeComponentManifestEntry> entries, ISet<string> aliases)
    {
        foreach (var alias in entries
                     .SelectMany(static x => x.AllAliases)
                     .OrderBy(static x => x, StringComparer.Ordinal))
        {
            aliases.Add(alias);
        }
    }

    private static bool SupportsBackend(LanguageFeatureDescriptor descriptor, RuntimeComponentManifestEntry backend)
    {
        return backend.AllAliases.Any(alias => descriptor.SupportedBackendAliases.Contains(alias, StringComparer.Ordinal));
    }

    private sealed record FeatureEvaluation(
        LanguageFeatureDescriptor Descriptor,
        bool IsAvailable,
        IReadOnlyList<string> Reasons);
}
