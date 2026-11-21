namespace UniversalToolchain.ModuleContracts;

/// <summary>
///     The single owner of duplicate-contract-id detection.
/// </summary>
/// <remarks>
///     <para>
///         This class was previously reachable only from a test: <see cref="ModuleContractTableBuilder.Build" /> called
///         the pipeline-effect and namespace validators but never this one, so <c>UT-CONTRACT-002</c> was documented as
///         a live diagnostic while a duplicate identifier passed through the production table build with no diagnostic
///         at all. Constants mentioned by an unreachable verifier still satisfy a documentation gate, which is exactly
///         how that went unnoticed. The table build now calls it, so the declared code is the emitted code.
///     </para>
///     <para>
///         Two entry points, one rule. <see cref="ValidateUniqueIds" /> judges an explicit list of
///         <see cref="ContractId" />; <see cref="ValidateUniqueDeclarations" /> judges the identifiers one facet declares.
///         Both share one grouping implementation on purpose - two copies would drift, and a gate that reported success
///         using a narrower set would look identical to a correct one.
///     </para>
/// </remarks>
public static class ContractIdRegistryValidator
{
    public static IReadOnlyList<ToolchainDiagnostic> ValidateUniqueIds(IEnumerable<ContractId> ids)
    {
        ids = ids.ArgNotNull();

        return ids
            .GroupBy(static x => x.FullName, StringComparer.Ordinal)
            .Where(static x => x.Count() > 1)
            .Select(static x => new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.DuplicateId,
                ToolchainDiagnosticSeverity.Error,
                $"Duplicate contract id '{x.Key}'.",
                null,
                [new ToolchainDiagnosticHint("Contract ids must be unique inside their namespace.")]))
            .OrderBy(static x => x.Message, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    ///     Reports identifiers declared more than once by a single facet.
    /// </summary>
    /// <remarks>
    ///     The scope is narrow twice over: <em>within one facet</em>, and over identifiers the facet <em>declares</em>
    ///     rather than every identifier it mentions.
    ///     <para>
    ///         Not across facets or modules, because the same identifier legitimately appears in two facets of one
    ///         module while saying different things about it - measured on the real corpus, <c>LabelsModule</c>
    ///         produces <c>wist.labels.ast.label</c> as a parser node and owns it as an AST node. Two modules claiming
    ///         one AST node exclusively is already <c>UT-MOD-OWNERSHIP-002</c>, and its resolution depends on the
    ///         lowerer, not on the table, so reporting it here as well would invent a second answer.
    ///     </para>
    ///     <para>
    ///         Not over bytecode emissions either: <c>MayEmitTags</c>/<c>MayEmitPatterns</c> are per-emission
    ///         capabilities, and two different nodes emitting the same tag is an ordinary contract.
    ///     </para>
    /// </remarks>
    public static IReadOnlyList<ToolchainDiagnostic> ValidateUniqueDeclarations(IModuleContractFacet facet)
    {
        facet = facet.ArgNotNull();
        var moduleId = facet.ModuleId;
        var kind = facet.Kind;

        return facet
            .DeclaredIdentifiers()
            .GroupBy(static x => x, StringComparer.Ordinal)
            .Where(static x => x.Count() > 1)
            .Select(x => new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.DuplicateId,
                ToolchainDiagnosticSeverity.Error,
                $"Module '{moduleId}' declares duplicate '{kind}' contract id '{x.Key}'.",
                null,
                [new ToolchainDiagnosticHint("Declare each contract id once per facet; an id may still appear in different facets that state different facts about it.")]))
            .OrderBy(static x => x.Message, StringComparer.Ordinal)
            .ToArray();
    }
}
