using System.Numerics;
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
            var passes = transformations
                .Where(static item => item.Contribution.Transformation!.IsPass)
                .OrderBy(static item => item.Contribution.Id.Value, StringComparer.Ordinal)
                .ToArray();
            var routeOrderConstraints = CreateRouteOrderConstraints(definition, transformations);

            var routeSearch = FindBestRoute(
                definition.EntryArtifact,
                target,
                conversionEdges,
                passes,
                routeOrderConstraints);
            if (routeSearch.Steps == null)
            {
                var orderUnconstrained = routeOrderConstraints.Count == 0
                    ? routeSearch
                    : FindBestRoute(definition.EntryArtifact, target, conversionEdges, passes, []);
                if (routeOrderConstraints.Count != 0 && orderUnconstrained.Steps != null)
                {
                    var orderDiagnostics = new List<LanguageDiagnostic>();
                    var orderSteps = InsertPasses(
                        definition,
                        definition.EntryArtifact,
                        orderUnconstrained.Steps,
                        passes,
                        backend,
                        orderDiagnostics);
                    if (orderSteps != null)
                    {
                        ValidateDescriptorRouteOrder(transformations, orderSteps, backend, orderDiagnostics);
                        ValidateDefinitionRouteOrder(definition, orderSteps, backend, orderDiagnostics);
                    }
                    foreach (var diagnostic in orderDiagnostics)
                        diagnostics.Add(diagnostic);
                    continue;
                }

                var unconstrained = FindBestRoute(
                    definition.EntryArtifact,
                    target,
                    conversionEdges,
                    [],
                    routeOrderConstraints);
                if (unconstrained.Steps != null && passes.Length != 0)
                {
                    diagnostics.Add(LanguagePlanningDiagnostics.Error(
                        "UTL2204", "planning",
                        $"Selected artifact passes cannot all be placed on any conversion route for backend '{backend.Value}'.",
                        backend.Value,
                        "Remove the pass, restrict its supported backends, or provide a route containing every mandatory pass contract."));
                }
                else
                {
                    diagnostics.Add(LanguagePlanningDiagnostics.Error(
                        "UTL2201", "planning",
                        $"No type-compatible artifact route exists from '{definition.EntryArtifact}' to '{target}' for backend '{backend.Value}'.",
                        runtimeProvider.Contribution.Id.Value,
                        "Register compatible typed artifact-transformer contributions or correct their contracts."));
                }
                continue;
            }
            if (routeSearch.IsAmbiguous)
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2207", "planning",
                    $"Backend '{backend.Value}' has multiple fully feasible minimum-cost artifact routes with no explicit semantic preference.",
                    backend.Value,
                    "Assign distinct route costs or introduce an explicit route-selection policy instead of relying on contribution IDs."));
                continue;
            }

            var steps = InsertPasses(
                definition,
                definition.EntryArtifact,
                routeSearch.Steps,
                passes,
                backend,
                diagnostics);
            if (steps == null)
                continue;
            if (!ValidateDescriptorRouteOrder(transformations, steps, backend, diagnostics))
                continue;
            if (!ValidateDefinitionRouteOrder(definition, steps, backend, diagnostics))
                continue;
            routes.Add(new LanguageArtifactRoute(backend, definition.EntryArtifact, target, steps));
        }
        return routes;
    }

    private static IReadOnlyList<RouteOrderConstraint> CreateRouteOrderConstraints(
        LanguageDefinition definition,
        IReadOnlyList<ResolvedLanguageContribution> transformations)
    {
        var conversions = transformations
            .Where(static item => !item.Contribution.Transformation!.IsPass)
            .ToDictionary(static item => item.Contribution.Id);
        var constraints = new HashSet<RouteOrderConstraint>();
        foreach (var item in conversions.Values)
        {
            foreach (var before in item.Contribution.BeforeContributions.Where(conversions.ContainsKey))
                constraints.Add(new RouteOrderConstraint(item.Contribution.Id, before));
            foreach (var after in item.Contribution.AfterContributions.Where(conversions.ContainsKey))
                constraints.Add(new RouteOrderConstraint(after, item.Contribution.Id));
        }
        foreach (var constraint in definition.ContributionOrderConstraints.Where(constraint =>
                     conversions.ContainsKey(constraint.Source) && conversions.ContainsKey(constraint.Target)))
        {
            if (constraint.Kind == LanguageContributionOrderKind.Before)
                constraints.Add(new RouteOrderConstraint(constraint.Source, constraint.Target));
            else
                constraints.Add(new RouteOrderConstraint(constraint.Target, constraint.Source));
        }
        return constraints
            .OrderBy(static constraint => constraint.Before.Value, StringComparer.Ordinal)
            .ThenBy(static constraint => constraint.After.Value, StringComparer.Ordinal)
            .ToArray();
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
                .ToArray();
            if (ready.Length == 0)
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2202", "planning",
                    $"Artifact passes for contract '{contract}' contain an ordering cycle.",
                    contract.Kind.Value,
                    "Remove cyclic descriptor-level or definition-level Before/After/Requires constraints."));
                return false;
            }

            var minimumOrder = ready.Min(static item => item.Contribution.Order);
            var minimumReady = ready
                .Where(item => item.Contribution.Order == minimumOrder)
                .OrderBy(static item => item.Contribution.Id.Value, StringComparer.Ordinal)
                .ToArray();
            if (minimumReady.Length != 1)
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2208", "planning",
                    $"Artifact passes for contract '{contract}' have multiple unrelated ready contributions with equal order {minimumOrder}: {string.Join(", ", minimumReady.Select(static item => item.Contribution.Id.Value))}.",
                    contract.Kind.Value,
                    "Declare Before/After ordering or assign distinct semantic Order values; contribution IDs do not resolve execution ambiguity."));
                return false;
            }

            var selected = minimumReady[0];
            var transformation = selected.Contribution.Transformation!;
            output.Add(new LanguageArtifactRouteStep(
                selected.Contribution.Id,
                transformation.SourceContract,
                transformation.TargetContract,
                transformation.Cost));
            emitted.Add(selected.Contribution.Id);
            remaining.Remove(selected.Contribution.Id);
        }
        return true;
    }

    private static bool ValidateDescriptorRouteOrder(
        IReadOnlyList<ResolvedLanguageContribution> transformations,
        IReadOnlyList<LanguageArtifactRouteStep> steps,
        BackendId backend,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var indexes = steps
            .Select(static (step, index) => (step.ContributionId, Index: index))
            .ToDictionary(static item => item.ContributionId, static item => item.Index);
        var contributions = transformations.ToDictionary(static item => item.Contribution.Id);
        foreach (var (contributionId, index) in indexes)
        {
            if (!contributions.TryGetValue(contributionId, out var resolved))
                continue;

            foreach (var before in resolved.Contribution.BeforeContributions)
            {
                if (!indexes.TryGetValue(before, out var beforeIndex) || index < beforeIndex)
                    continue;
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2206", "planning",
                    $"Executable route for backend '{backend.Value}' violates descriptor order: '{contributionId.Value}' Before '{before.Value}'.",
                    contributionId.Value,
                    "Change the contribution order or provide a route whose topology can satisfy it."));
                return false;
            }
            foreach (var after in resolved.Contribution.AfterContributions)
            {
                if (!indexes.TryGetValue(after, out var afterIndex) || index > afterIndex)
                    continue;
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2206", "planning",
                    $"Executable route for backend '{backend.Value}' violates descriptor order: '{contributionId.Value}' After '{after.Value}'.",
                    contributionId.Value,
                    "Change the contribution order or provide a route whose topology can satisfy it."));
                return false;
            }
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

    private static RouteSearchResult FindBestRoute(
        LanguageArtifactContract source,
        LanguageArtifactContract target,
        IReadOnlyList<RouteEdge> edges,
        IReadOnlyList<ResolvedLanguageContribution> requiredPasses,
        IReadOnlyList<RouteOrderConstraint> orderConstraints)
    {
        var requiredMask = requiredPasses.Count == 0
            ? BigInteger.Zero
            : (BigInteger.One << requiredPasses.Count) - BigInteger.One;
        var orderIds = orderConstraints
            .SelectMany(static constraint => new[] { constraint.Before, constraint.After })
            .Distinct()
            .OrderBy(static id => id.Value, StringComparer.Ordinal)
            .ToArray();
        var orderBits = orderIds
            .Select(static (id, index) => (id, index))
            .ToDictionary(static item => item.id, static item => item.index);
        var initialMask = PassMaskForContract(source, requiredPasses);
        var initialKey = new RouteSearchKey(source, initialMask, BigInteger.Zero);
        var best = new Dictionary<RouteSearchKey, RouteState>
        {
            [initialKey] = new RouteState(0L, string.Empty, [], false)
        };
        var pending = new HashSet<RouteSearchKey> { initialKey };
        while (pending.Count != 0)
        {
            var current = pending
                .OrderBy(key => best[key].Cost)
                .ThenBy(key => best[key].Signature, StringComparer.Ordinal)
                .ThenBy(static key => key.Contract.ToString(), StringComparer.Ordinal)
                .ThenBy(static key => key.CoveredPasses)
                .ThenBy(static key => key.SeenOrderContributions)
                .First();
            pending.Remove(current);
            var currentState = best[current];
            foreach (var edge in edges
                         .Where(edge => LanguageArtifactRoute.ContractsConnect(current.Contract, edge.Transformation.SourceContract))
                         .OrderBy(static edge => edge.ContributionId.Value, StringComparer.Ordinal))
            {
                if (ViolatesRouteOrder(
                        edge.ContributionId,
                        current.SeenOrderContributions,
                        orderConstraints,
                        orderBits))
                    continue;

                var nextContract = edge.Transformation.TargetContract;
                var nextMask = current.CoveredPasses | PassMaskForContract(nextContract, requiredPasses);
                var nextOrderMask = MarkOrderContributionSeen(
                    edge.ContributionId,
                    current.SeenOrderContributions,
                    orderBits);
                var nextKey = new RouteSearchKey(nextContract, nextMask, nextOrderMask);
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
                        edge.Transformation.Cost)).ToArray(),
                    currentState.IsAmbiguous);

                if (!best.TryGetValue(nextKey, out var existing) || candidate.Cost < existing.Cost)
                {
                    best[nextKey] = candidate;
                    pending.Add(nextKey);
                    continue;
                }
                if (candidate.Cost != existing.Cost)
                    continue;

                var differentPath = !StringComparer.Ordinal.Equals(candidate.Signature, existing.Signature);
                var mergedAmbiguity = existing.IsAmbiguous || candidate.IsAmbiguous || differentPath;
                var useCandidate = StringComparer.Ordinal.Compare(candidate.Signature, existing.Signature) < 0;
                var representative = useCandidate ? candidate : existing;
                if (representative.IsAmbiguous == mergedAmbiguity && !useCandidate)
                    continue;

                best[nextKey] = representative with { IsAmbiguous = mergedAmbiguity };
                pending.Add(nextKey);
            }
        }

        var goals = best
            .Where(pair => pair.Key.CoveredPasses == requiredMask &&
                           LanguageArtifactRoute.ContractsConnect(pair.Key.Contract, target))
            .ToArray();
        if (goals.Length == 0)
            return new RouteSearchResult(null, false);

        var minimumCost = goals.Min(static pair => pair.Value.Cost);
        var minimumGoals = goals
            .Where(pair => pair.Value.Cost == minimumCost)
            .OrderBy(static pair => pair.Value.Signature, StringComparer.Ordinal)
            .ToArray();
        var distinctSignatures = minimumGoals
            .Select(static pair => pair.Value.Signature)
            .Distinct(StringComparer.Ordinal)
            .Take(2)
            .Count();
        var ambiguous = distinctSignatures > 1 || minimumGoals.Any(static pair => pair.Value.IsAmbiguous);
        return new RouteSearchResult(minimumGoals[0].Value.Steps, ambiguous);
    }

    private static bool ViolatesRouteOrder(
        LanguageContributionId next,
        BigInteger seen,
        IReadOnlyList<RouteOrderConstraint> constraints,
        IReadOnlyDictionary<LanguageContributionId, int> orderBits)
    {
        foreach (var constraint in constraints.Where(constraint => constraint.Before == next))
        {
            if (!orderBits.TryGetValue(constraint.After, out var afterIndex))
                continue;
            if ((seen & (BigInteger.One << afterIndex)) != BigInteger.Zero)
                return true;
        }
        return false;
    }

    private static BigInteger MarkOrderContributionSeen(
        LanguageContributionId contribution,
        BigInteger seen,
        IReadOnlyDictionary<LanguageContributionId, int> orderBits)
    {
        if (!orderBits.TryGetValue(contribution, out var index))
            return seen;
        return seen | (BigInteger.One << index);
    }

    private static BigInteger PassMaskForContract(
        LanguageArtifactContract contract,
        IReadOnlyList<ResolvedLanguageContribution> passes)
    {
        var mask = BigInteger.Zero;
        for (var index = 0; index < passes.Count; index++)
        {
            var passContract = passes[index].Contribution.Transformation!.SourceContract;
            if (LanguageArtifactRoute.ContractsConnect(contract, passContract))
                mask |= BigInteger.One << index;
        }
        return mask;
    }

    private sealed record RouteEdge(
        LanguageContributionId ContributionId,
        ArtifactTransformationDescriptor Transformation);

    private readonly record struct RouteOrderConstraint(
        LanguageContributionId Before,
        LanguageContributionId After);

    private readonly record struct RouteSearchKey(
        LanguageArtifactContract Contract,
        BigInteger CoveredPasses,
        BigInteger SeenOrderContributions);

    private sealed record RouteState(
        long Cost,
        string Signature,
        IReadOnlyList<LanguageArtifactRouteStep> Steps,
        bool IsAmbiguous);

    private sealed record RouteSearchResult(
        IReadOnlyList<LanguageArtifactRouteStep>? Steps,
        bool IsAmbiguous);
}
