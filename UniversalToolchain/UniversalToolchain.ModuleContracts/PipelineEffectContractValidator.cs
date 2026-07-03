namespace UniversalToolchain.ModuleContracts;

public static class PipelineEffectContractValidator
{
    public static IReadOnlyList<ToolchainDiagnostic> Validate(SelectedModuleContractTable table)
    {
        table = table.ArgNotNull();

        var ownershipDiagnostics = ValidateOwnershipDeclarations(table);
        var effectDiagnostics = ValidateEffects(table);
        return ownershipDiagnostics
            .Concat(effectDiagnostics)
            .OrderBy(static x => x.Code, StringComparer.Ordinal)
            .ThenBy(static x => x.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<ToolchainDiagnostic> ValidateOwnershipDeclarations(SelectedModuleContractTable table)
    {
        var ownerships = table.CompilerFactOwnershipFacets
            .SelectMany(static facet => facet.Facts.Select(fact => (facet.ModuleId, fact)))
            .ToArray();

        foreach (var duplicate in ownerships
                     .GroupBy(static x => x.fact.FactId)
                     .Where(static x => x.Count() > 1))
        {
            yield return new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.DuplicateCompilerFactOwner,
                ToolchainDiagnosticSeverity.Error,
                $"Compiler fact '{duplicate.Key}' has multiple owners.",
                null,
                [new ToolchainDiagnosticHint("Declare each compiler fact in exactly one owning module contract facet.")]);
        }

        foreach (var item in ownerships.Where(static x => x.ModuleId != x.fact.OwnerModule))
        {
            yield return new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.ForeignCompilerFactProduction,
                ToolchainDiagnosticSeverity.Error,
                $"Module '{item.ModuleId}' declares ownership for compiler fact '{item.fact.FactId}' owned by '{item.fact.OwnerModule}'.",
                null,
                [new ToolchainDiagnosticHint("Move the fact ownership declaration to its owner module facet.")]);
        }
    }

    private static IEnumerable<ToolchainDiagnostic> ValidateEffects(SelectedModuleContractTable table)
    {
        var factOwners = table.CompilerFactOwnershipFacets
            .SelectMany(static facet => facet.Facts)
            .GroupBy(static x => x.FactId)
            .ToDictionary(static x => x.Key, static x => x.First().OwnerModule);

        foreach (var facet in table.PipelineEffectFacets)
        {
            foreach (var effect in facet.Effects)
            {
                foreach (var diagnostic in ValidateEffectShape(facet.ModuleId, effect))
                    yield return diagnostic;

                foreach (var fact in effect.Requires
                             .Concat(effect.Produces)
                             .Concat(effect.Preserves)
                             .Concat(effect.Invalidates)
                             .Distinct()
                             .Where(fact => !factOwners.ContainsKey(fact)))
                {
                    yield return new ToolchainDiagnostic(
                        ModuleContractDiagnosticCodes.UnknownCompilerFact,
                        ToolchainDiagnosticSeverity.Error,
                        $"Module '{facet.ModuleId}' references compiler fact '{fact}', but no selected fact ownership facet declares it.",
                        null,
                        [new ToolchainDiagnosticHint("Select the owning module contract descriptor, add a CompilerFactOwnershipFacet, or remove the undeclared fact reference.")]);
                }

                foreach (var producedFact in effect.Produces)
                {
                    if (!factOwners.TryGetValue(producedFact, out var owner))
                        continue;

                    if (owner == facet.ModuleId)
                        continue;

                    yield return new ToolchainDiagnostic(
                        ModuleContractDiagnosticCodes.ForeignCompilerFactProduction,
                        ToolchainDiagnosticSeverity.Error,
                        $"Module '{facet.ModuleId}' produces compiler fact '{producedFact}' owned by '{owner}'.",
                        null,
                        [new ToolchainDiagnosticHint("Only the owning module should produce a fact unless explicit delegation is introduced.")]);
                }

            }
        }
    }

    private static IEnumerable<ToolchainDiagnostic> ValidateEffectShape(ModuleId moduleId, PipelineEffectContract effect)
    {
        var producedAndInvalidated = effect.Produces.Intersect(effect.Invalidates).ToArray();
        foreach (var fact in producedAndInvalidated)
        {
            yield return new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.InvalidPipelineEffect,
                ToolchainDiagnosticSeverity.Error,
                $"Module '{moduleId}' pipeline effect '{effect.EffectId}' both produces and invalidates compiler fact '{fact}'.",
                null,
                [new ToolchainDiagnosticHint("Split the effect into ordered transitions or remove the contradictory fact declaration.")]);
        }

        var preservedAndInvalidated = effect.Preserves.Intersect(effect.Invalidates).ToArray();
        foreach (var fact in preservedAndInvalidated)
        {
            yield return new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.InvalidPipelineEffect,
                ToolchainDiagnosticSeverity.Error,
                $"Module '{moduleId}' pipeline effect '{effect.EffectId}' both preserves and invalidates compiler fact '{fact}'.",
                null,
                [new ToolchainDiagnosticHint("A fact cannot be preserved and invalidated by the same effect.")]);
        }
    }
}
