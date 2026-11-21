namespace UniversalToolchain.ModuleContracts;

/// <summary>
///     The identifiers a facet <em>declares</em> — as opposed to identifiers it merely mentions.
/// </summary>
/// <remarks>
///     <para>
///         This exists because "which identifiers does this facet own" has two different answers depending on the
///         question being asked, and conflating them produces false errors.
///     </para>
///     <para>
///         <see cref="ModuleContractFacetIdentifiers.OwnedIdentifiers" /> answers the namespace question: every id this
///         facet is responsible for must sit inside the module's reserved namespace, and that includes bytecode tags and
///         patterns, because a module that emits them is claiming them.
///     </para>
///     <para>
///         Uniqueness asks a different question: can this id appear twice in this facet without contradiction? For a
///         declaration list the answer is no — listing one fact twice in one place is a mistake. For bytecode emissions
///         the answer is yes: <c>MayEmitTags</c> is a capability of one emission ("this node may emit the <c>call</c>
///         tag"), and two different nodes emitting the same tag is ordinary, not a duplicate declaration. Counting them
///         as declarations would reject correct contracts.
///     </para>
/// </remarks>
internal static class ModuleContractFacetDeclarations
{
    public static IEnumerable<string> DeclaredIdentifiers(this IModuleContractFacet facet)
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
            // Bytecode is intentionally absent: MayEmitTags/MayEmitPatterns describe per-emission capabilities, so a
            // repeated tag there is a normal contract, not a duplicate declaration.
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
