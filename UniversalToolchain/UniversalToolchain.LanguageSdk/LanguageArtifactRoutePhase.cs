using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.LanguageSdk;

internal static class LanguageArtifactRoutePhase
{
    public static IReadOnlyList<LanguageArtifactRoute> Build(
        LanguageDefinition definition,
        IReadOnlyList<ResolvedLanguageContribution> contributions,
        ResolvedLanguageContribution runtimeProvider,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var routes = new List<LanguageArtifactRoute>();
        foreach (var backend in definition.Backends.OrderBy(static x => x.Value, StringComparer.Ordinal))
        {
            var backendCapability = LanguageCapabilities.Backend(backend);
            var backendOwners = contributions
                .Where(item => item.Contribution.ProvidesCapabilities.Contains(backendCapability))
                .ToArray();
            if (backendOwners.Length != 1)
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2203", "planning",
                    $"Backend '{backend.Value}' must have exactly one selected contribution owner, but {backendOwners.Length} were selected.",
                    backend.Value,
                    "Select one backend capability provider explicitly."));
                continue;
            }

            var backendOwner = backendOwners[0].Contribution;
            LanguageArtifactContract target;
            if (backendOwner.BackendInputContract is { } backendInputContract)
            {
                target = backendInputContract;
            }
            else if (!runtimeProvider.Contribution.RuntimeInputContracts.TryGetValue(backend, out target))
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2303", "planning",
                    $"Neither backend contribution '{backendOwner.Id.Value}' nor runtime provider '{runtimeProvider.Contribution.RuntimeProviderId!.Value.Value}' declares an execution input artifact for backend '{backend.Value}'.",
                    backendOwner.Id.Value,
                    "Declare the backend execution input contract on the backend contribution."));
                continue;
            }

            var transformations = contributions
                .Where(item => item.Contribution.Transformation != null)
                .Where(item => item.Contribution.SupportedBackends.Count == 0 || item.Contribution.SupportedBackends.Contains(backend))
                .ToArray();
            var conversionEdges = transformations
                .Where(static item => !item.Contribution.Transformation!.IsPass)
                .Select(static item => new RouteEdge(item.Contribution.Id, item.Contribution.Transformation!))
                .ToArray();
            var baseSteps = FindBestRoute(definition.EntryArtifact, target, conversionEdges);
            if (baseSteps == null)
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2201", "planning",
                    $"No type-compatible artifact route exists from '{definition.EntryArtifact}' to '{target}' for backend '{backend.Value}'.",
                    runtimeProvider.Contribution.Id.Value,
                    "Register compatible typed artifact-transformer contributions or correct their contracts."));
                continue;
            }

            var steps = InsertPasses(
                definition,
                definition.EntryArtifact,
                baseSteps,
                transformations.Where(static item => item.Contribution.Transformation!.IsPass).ToArray(),
                backend,
                diagnostics);
            if (steps == null)
                continue;
            if (!ValidateDefinitionRouteOrder(definition, steps, backend, diagnostics))
                continue;
            routes.Add(new LanguageArtifactRoute(backend, definition.EntryArtifact, target, steps));
        }
        return routes;
    }

    private static IReadOnlyList<LanguageArtifactRouteStep>? InsertPasses(
        LanguageDefinition definition,
        LanguageArtifactContract source,
        IReadOnlyList<LanguageArtifactRouteStep> baseSteps,
        IReadOnlyList<ResolvedLanguageContribution> passes,
        BackendId backend,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var result = new List<LanguageArtifactRouteStep>();
        var remaining = passes.ToDictionary(static item => item.Contribution.Id);
        var current = source;

        foreach (var step in baseSteps)
        {
            if (!AppendPassesForContract(definition, current, remaining, result, backend, diagnostics))
                return null;
            result.Add(step);
            current = step.TargetContract;
        }
        if (!AppendPassesForContract(definition, current, remaining, result, backend, diagnostics))
            return null;
        if (remaining.Count != 0)
        {
            var unplaced = string.Join(", ", remaining.Values
                .OrderBy(static item => item.Contribution.Id.Value, StringComparer.Ordinal)
                .Select(static item => $"{item.Contribution.Id.Value} ({item.Contribution.Transformation!.SourceContract})"));
            diagnostics.Add(LanguagePlanningDiagnostics.Error(
                "UTL2204", "planning",
                $"Selected artifact passes cannot be placed on the route for backend '{backend.Value}': {unplaced}.",
                backend.Value,
                "Remove the pass, restrict its supported backends, or provide a route containing its artifact contract."));
            return null;
        }
        return result;
    }

    private static bool AppendPassesForContract(
        LanguageDefinition definition,
        LanguageArtifactContract contract,
        IDictionary<LanguageContributionId, ResolvedLanguageContribution> remaining,
        ICollection<LanguageArtifactRouteStep> output,
        BackendId backend,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var candidates = remaining.Values
            .Where(item => item.Contribution.SupportedBackends.Count == 0 || item.Contribution.SupportedBackends.Contains(backend))
            .Where(item => LanguageArtifactRoute.ContractsConnect(contract, item.Contribution.Transformation!.SourceContract) &&
                           LanguageArtifactRoute.ContractsConnect(item.Contribution.Transformation.TargetContract, contract))
            .ToDictionary(static item => item.Contribution.Id);
        if (candidates.Count == 0)
            return true;

        var predecessors = candidates.Keys.ToDictionary(
            static id => id,
            static _ => new HashSet<LanguageContributionId>());
        foreach (var item in candidates.Values)
        {
            foreach (var after in item.Contribution.AfterContributions.Where(candidates.ContainsKey))
                predecessors[item.Contribution.Id].Add(after);
            foreach (var before in item.Contribution.BeforeContributions.Where(candidates.ContainsKey))
                predecessors[before].Add(item.Contribution.Id);
        }
        foreach (var constraint in definition.ContributionOrderConstraints.Where(constraint =>
                     candidates.ContainsKey(constraint.Source) && candidates.ContainsKey(constraint.Target)))
        {
            if (constraint.Kind == LanguageContributionOrderKind.Before)
                predecessors[constraint.Target].Add(constraint.Source);
            else
                predecessors[constraint.Source].Add(constraint.Target);
        }

        var emitted = new HashSet<LanguageContributionId>();
        while (emitted.Count != candidates.Count)
        {
            var ready = candidates.Values
                .Where(item => !emitted.Contains(item.Contribution.Id))
                .Where(item => predecessors[item.Contribution.Id].All(emitted.Contains))
                .OrderBy(static item => item.Contribution.Order)
                .ThenBy(static item => item.Contribution.Id.Value, StringComparer.Ordinal)
                .FirstOrDefault();
            if (ready == null)
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2202", "planning",
                    $"Artifact passes for contract '{contract}' contain an ordering cycle.",
                    contract.Kind.Value,
                    "Remove cyclic descriptor-level or definition-level Before/After/Requires constraints."));
                return false;
            }

            var transformation = ready.Contribution.Transformation!;
            output.Add(new LanguageArtifactRouteStep(
                ready.Contribution.Id,
                transformation.SourceContract,
                transformation.TargetContract,
                transformation.Cost));
            emitted.Add(ready.Contribution.Id);
            remaining.Remove(ready.Contribution.Id);
        }
        return true;
    }

    private static bool ValidateDefinitionRouteOrder(
        LanguageDefinition definition,
        IReadOnlyList<LanguageArtifactRouteStep> steps,
        BackendId backend,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        if (definition.ContributionOrderConstraints.Count == 0)
            return true;

        var indexes = steps
            .Select(static (step, index) => (step.ContributionId, Index: index))
            .ToDictionary(static item => item.ContributionId, static item => item.Index);
        foreach (var constraint in definition.ContributionOrderConstraints)
        {
            if (!indexes.TryGetValue(constraint.Source, out var sourceIndex) ||
                !indexes.TryGetValue(constraint.Target, out var targetIndex))
                continue;

            var satisfied = constraint.Kind == LanguageContributionOrderKind.Before
                ? sourceIndex < targetIndex
                : targetIndex < sourceIndex;
            if (satisfied)
                continue;

            diagnostics.Add(LanguagePlanningDiagnostics.Error(
                "UTL2205", "planning",
                $"Executable route for backend '{backend.Value}' violates definition-level order: '{constraint.Source.Value}' {constraint.Kind} '{constraint.Target.Value}'.",
                constraint.Source.Value,
                "Change the definition order or provide a route whose executable contributions can satisfy it."));
            return false;
        }
        return true;
    }

    private static IReadOnlyList<LanguageArtifactRouteStep>? FindBestRoute(
        LanguageArtifactContract source,
        LanguageArtifactContract target,
        IReadOnlyList<RouteEdge> edges)
    {
        var best = new Dictionary<LanguageArtifactContract, RouteState>
        {
            [source] = new RouteState(0, string.Empty, [])
        };
        var pending = new HashSet<LanguageArtifactContract> { source };
        while (pending.Count != 0)
        {
            var current = pending
                .OrderBy(node => best[node].Cost)
                .ThenBy(node => best[node].Signature, StringComparer.Ordinal)
                .ThenBy(static node => node.ToString(), StringComparer.Ordinal)
                .First();
            pending.Remove(current);
            var currentState = best[current];
            foreach (var edge in edges
                         .Where(edge => LanguageArtifactRoute.ContractsConnect(current, edge.Transformation.SourceContract))
                         .OrderBy(static edge => edge.ContributionId.Value, StringComparer.Ordinal))
            {
                var next = edge.Transformation.TargetContract;
                var signature = string.IsNullOrEmpty(currentState.Signature)
                    ? edge.ContributionId.Value
                    : currentState.Signature + "|" + edge.ContributionId.Value;
                var candidate = new RouteState(
                    currentState.Cost + edge.Transformation.Cost,
                    signature,
                    currentState.Steps.Append(new LanguageArtifactRouteStep(
                        edge.ContributionId,
                        edge.Transformation.SourceContract,
                        edge.Transformation.TargetContract,
                        edge.Transformation.Cost)).ToArray());
                if (!best.TryGetValue(next, out var existing) ||
                    candidate.Cost < existing.Cost ||
                    candidate.Cost == existing.Cost && StringComparer.Ordinal.Compare(candidate.Signature, existing.Signature) < 0)
                {
                    best[next] = candidate;
                    pending.Add(next);
                }
            }
        }

        return best
            .Where(pair => LanguageArtifactRoute.ContractsConnect(pair.Key, target))
            .OrderBy(static pair => pair.Value.Cost)
            .ThenBy(static pair => pair.Value.Signature, StringComparer.Ordinal)
            .Select(static pair => pair.Value.Steps)
            .FirstOrDefault();
    }

    private sealed record RouteEdge(
        LanguageContributionId ContributionId,
        ArtifactTransformationDescriptor Transformation);

    private sealed record RouteState(
        int Cost,
        string Signature,
        IReadOnlyList<LanguageArtifactRouteStep> Steps);
}
