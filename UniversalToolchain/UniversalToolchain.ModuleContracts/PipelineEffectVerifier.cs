namespace UniversalToolchain.ModuleContracts;

public sealed class PipelineEffectVerifier
{
    public PipelineEffectValidationResult Validate(PipelineEffectValidationRequest request)
    {
        request = request.ArgNotNull();

        var available = request.InputFacts.Available.ToHashSet();
        var invalidated = request.InputFacts.Invalidated.ToHashSet();
        var diagnostics = new List<ToolchainDiagnostic>();
        var obligations = new HashSet<VerificationObligation>(
            request.PendingObligations ?? []);

        var effects = request.ContractTable.PipelineEffectFacets
            .SelectMany(static facet => facet.Effects.Select(effect => (facet.ModuleId, effect)))
            .Where(item => item.effect.Stage == request.Stage)
            .ToArray();
        if (effects.Length > 0 && (request.PipelineOrder == null || request.PipelineOrder.Count == 0))
        {
            diagnostics.Add(new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.MissingPipelineOrder,
                ToolchainDiagnosticSeverity.Error,
                $"Pipeline effect verification for '{request.Stage}' requires the actual selected pipeline order.",
                null,
                [new ToolchainDiagnosticHint("Pass the frontend/optimizer/backend order from the selected runtime/build plan instead of relying on module id sorting.")]));

            return new PipelineEffectValidationResult(
                new CompilerFactState(available, invalidated),
                diagnostics,
                []);
        }

        var duplicateOccurrences = (request.PipelineOrder ?? [])
            .GroupBy(static moduleId => moduleId)
            .Where(static group => group.Count() > 1)
            .OrderBy(static group => group.Key.Value, StringComparer.Ordinal)
            .ToArray();
        if (duplicateOccurrences.Length > 0)
        {
            diagnostics.AddRange(duplicateOccurrences.Select(group => new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.DuplicatePipelineModuleOccurrence,
                ToolchainDiagnosticSeverity.Error,
                $"Pipeline order for '{request.Stage}' contains module '{group.Key}' {group.Count()} times, but module-level effects cannot distinguish repeated occurrences.",
                null,
                [new ToolchainDiagnosticHint("Use a unique module/pass identity per occurrence or extend the contract model with explicit occurrence identities before repeating a module.")])));

            return new PipelineEffectValidationResult(
                new CompilerFactState(available, invalidated),
                diagnostics,
                []);
        }

        var orderIndex = BuildOrderIndex(request.PipelineOrder);
        effects = effects
            .OrderBy(item => orderIndex.TryGetValue(item.ModuleId, out var index) ? index : int.MaxValue)
            .ThenBy(static item => item.ModuleId.Value, StringComparer.Ordinal)
            .ThenBy(static item => item.effect.EffectId.Value, StringComparer.Ordinal)
            .ToArray();

        foreach (var item in effects)
        {
            var missingRequiredFacts = item.effect.Requires
                .Where(required => !available.Contains(required))
                .ToArray();

            if (missingRequiredFacts.Length > 0)
            {
                foreach (var required in missingRequiredFacts)
                {
                    diagnostics.Add(new ToolchainDiagnostic(
                        ModuleContractDiagnosticCodes.MissingRequiredCompilerFact,
                        ToolchainDiagnosticSeverity.Error,
                        $"Module '{item.ModuleId}' pipeline effect '{item.effect.EffectId}' requires compiler fact '{required}', but it is not available at '{request.Stage}'.",
                        null,
                        [new ToolchainDiagnosticHint("Select the producer module earlier in the build plan or add a stage seed for externally guaranteed facts.")]));
                }

                continue;
            }

            ApplyEffect(
                item.ModuleId,
                item.effect,
                request.Stage,
                available,
                invalidated,
                diagnostics,
                obligations,
                request.VerifierRegistry);
        }

        return new PipelineEffectValidationResult(
            new CompilerFactState(available, invalidated),
            diagnostics
                .OrderBy(static x => x.Code, StringComparer.Ordinal)
                .ThenBy(static x => x.Message, StringComparer.Ordinal)
                .ToArray(),
            obligations
                .OrderBy(static obligation => obligation.FirstEligibleBoundary)
                .ThenBy(static obligation => obligation.RuleId.Value, StringComparer.Ordinal)
                .ThenBy(static obligation => obligation.FactId.Value, StringComparer.Ordinal)
                .ToArray());
    }

    private static IReadOnlyDictionary<ModuleId, int> BuildOrderIndex(IReadOnlyList<ModuleId>? pipelineOrder)
    {
        if (pipelineOrder == null || pipelineOrder.Count == 0)
            return new Dictionary<ModuleId, int>();

        var order = new Dictionary<ModuleId, int>();
        for (var i = 0; i < pipelineOrder.Count; i++)
            order.Add(pipelineOrder[i], i);

        return order;
    }

    private static void ApplyEffect(
        ModuleId moduleId,
        PipelineEffectContract effect,
        CompilerPipelineStage creationBoundary,
        HashSet<CompilerFactId> available,
        HashSet<CompilerFactId> invalidated,
        List<ToolchainDiagnostic> diagnostics,
        HashSet<VerificationObligation> obligations,
        CompilerFactVerifierRegistry verifierRegistry)
    {
        foreach (var fact in effect.Preserves.Where(fact => !available.Contains(fact)))
        {
            diagnostics.Add(new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.MissingRequiredCompilerFact,
                ToolchainDiagnosticSeverity.Error,
                $"Module '{moduleId}' pipeline effect '{effect.EffectId}' promises to preserve compiler fact '{fact}', but it is not available at this point.",
                null,
                [new ToolchainDiagnosticHint("Move the producer earlier, add an explicit requirement for the preserved fact, or remove the invalid preservation claim.")]));
        }

        foreach (var fact in effect.Invalidates)
        {
            available.Remove(fact);
            invalidated.Add(fact);

            if (!verifierRegistry.TryGetRoute(fact, out var route))
            {
                diagnostics.Add(new ToolchainDiagnostic(
                    ModuleContractDiagnosticCodes.MissingCompilerFactVerifierRoute,
                    ToolchainDiagnosticSeverity.Error,
                    $"Module '{moduleId}' pipeline effect '{effect.EffectId}' invalidates compiler fact '{fact}', but no canonical verifier route is registered.",
                    null,
                    [new ToolchainDiagnosticHint(
                        "Register exactly one executable verifier route and canonical owner for the fact, or stop declaring the invalidation.")]));
                continue;
            }

            var firstEligibleBoundary = route.EarliestExecutableBoundary < creationBoundary
                ? creationBoundary
                : route.EarliestExecutableBoundary;
            obligations.Add(new VerificationObligation(
                fact,
                route.RuleId,
                route.CanonicalOwner,
                creationBoundary,
                firstEligibleBoundary));
        }

        foreach (var fact in effect.Produces)
        {
            available.Add(fact);
            invalidated.Remove(fact);
            obligations.RemoveWhere(obligation => obligation.FactId == fact);
        }
    }
}
