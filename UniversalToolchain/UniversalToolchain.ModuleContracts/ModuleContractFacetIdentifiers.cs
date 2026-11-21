namespace UniversalToolchain.ModuleContracts;

/// <summary>
///     The single definition of "identifiers this facet declares as its own".
/// </summary>
/// <remarks>
///     Two validators need this set: namespace ownership and per-facet uniqueness. Keeping one copy is deliberate -
///     two enumerations would drift, and a validator that silently used a narrower set would report success for
///     identifiers it never looked at. Every case below therefore returns the identifiers the facet <em>owns</em>,
///     not every identifier it mentions: <c>MayEmitTags</c>, <c>MayConsume</c> and cooperating modules describe other
///     modules' identifiers, and treating those as owned would turn legal contracts into duplicates.
/// </remarks>
internal static class ModuleContractFacetIdentifiers
{
    public static IEnumerable<string> OwnedIdentifiers(this IModuleContractFacet facet)
    {
        facet = facet.ArgNotNull();

        switch (facet)
        {
            case ISyntaxContractFacet syntax:
                foreach (var node in syntax.ParserNodes)
                    yield return node.Produces.Value;
                break;
            case IAstContractFacet ast:
                foreach (var ownership in ast.AstOwnership.Where(x => x.OwnerModule == facet.ModuleId))
                    yield return ownership.NodeKind.Value;
                break;
            case IBytecodeContractFacet bytecode:
                foreach (var emission in bytecode.BytecodeEmissions)
                {
                    foreach (var tag in emission.MayEmitTags)
                        yield return tag.Value;
                    foreach (var pattern in emission.MayEmitPatterns)
                        yield return pattern.Value;
                }
                break;
            case ICompilerFactOwnershipFacet facts:
                foreach (var fact in facts.Facts.Where(x => x.OwnerModule == facet.ModuleId))
                    yield return fact.FactId.Value;
                break;
            case IPipelineEffectContractFacet effects:
                foreach (var effect in effects.Effects)
                    yield return effect.EffectId.Value;
                break;
            case IVerifierContractFacet verifier:
                foreach (var rule in verifier.Rules)
                    yield return rule.RuleId.Value;
                break;
            case IBackendCapabilityFacet backend:
                foreach (var capability in backend.Capabilities)
                    yield return capability.CapabilityId.Value;
                break;
        }
    }
}
