namespace UniversalToolchain.ModuleContracts;

/// <summary>
///     The deterministic sort order of contract facet kinds, and the check that every kind in use has one.
/// </summary>
/// <remarks>
///     <para>
///         The order exists so a contract table is reproducible: facets are sorted by module, then kind, then type
///         name. A kind missing from the table would make that ordering undefined, so it is reported rather than
///         silently sorted by accident.
///     </para>
///     <para>
///         <see cref="Validate" /> judges the facets that are actually present, and the earlier
///         <c>ValidateComplete()</c> did not. It enumerated <see cref="Enum.GetValues{TEnum}" /> and reported every
///         value absent from the table - which reads like the same check and is not: every declared value is in the
///         table, so it returned empty for every possible input. That is a tautology, and it meant
///         <c>UT-CONTRACT-009</c> could not be emitted by any table at all.
///     </para>
///     <para>
///         The tautology hid a second defect. The only input that could have reached the code is a facet whose kind
///         is outside the enum, and that input never got there: ordering called a throwing <c>GetSortKey</c> several
///         lines earlier, so two such facets produced a raw <see cref="InvalidOperationException" />, and one such
///         facet produced nothing at all because a one-element sort never invokes the key selector. Ordering now uses
///         <see cref="GetSortKeyOrLast" />, which cannot throw, so the diagnostic is what an operator actually sees.
///     </para>
/// </remarks>
public static class ContractFacetKindOrder
{
    private static readonly IReadOnlyDictionary<ContractFacetKind, int> _order =
        new Dictionary<ContractFacetKind, int>
        {
            [ContractFacetKind.Syntax] = 100,
            [ContractFacetKind.Ast] = 200,
            [ContractFacetKind.Bytecode] = 300,
            [ContractFacetKind.Air] = 400,
            [ContractFacetKind.CompilerFacts] = 500,
            [ContractFacetKind.PipelineEffects] = 600,
            [ContractFacetKind.Verifier] = 700,
            [ContractFacetKind.BackendCapability] = 800
        };

    /// <summary>Sort key for kinds with no declared order, so ordering stays total and stable.</summary>
    public const int UnknownKindSortKey = int.MaxValue;

    public static bool IsKnown(ContractFacetKind kind) => _order.ContainsKey(kind);

    /// <summary>
    ///     Sort key that never throws: a kind without a declared order sorts after every ordered kind.
    /// </summary>
    /// <remarks>
    ///     Ordering must not throw on the input <see cref="Validate" /> is meant to describe. Throwing here would
    ///     replace a typed diagnostic with an exception from inside a sort, and a one-element sequence would not throw
    ///     at all, so the same mistake would be invisible or fatal depending on how many facets happened to be present.
    /// </remarks>
    public static int GetSortKeyOrLast(ContractFacetKind kind) =>
        _order.TryGetValue(kind, out var value) ? value : UnknownKindSortKey;

    /// <summary>
    ///     Reports every facet whose kind has no deterministic order.
    /// </summary>
    /// <remarks>
    ///     Takes the facets rather than the enum because the enum cannot fail: every declared value is in the table,
    ///     so an enum-driven check is a tautology that returns empty for all input. The facets are the only input that
    ///     can carry an unorderable kind.
    /// </remarks>
    public static IReadOnlyList<ToolchainDiagnostic> Validate(IEnumerable<IModuleContractFacet> facets)
    {
        facets = facets.ArgNotNull();

        return facets
            .Where(static facet => !IsKnown(facet.Kind))
            .Select(static facet => new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.MissingFacetKindOrder,
                ToolchainDiagnosticSeverity.Error,
                $"Module '{facet.ModuleId}' declares facet kind '{facet.Kind}', which has no deterministic order.",
                null,
                [new ToolchainDiagnosticHint("Add the facet kind to ContractFacetKindOrder before using it in contract tables.")]))
            .OrderBy(static x => x.Message, StringComparer.Ordinal)
            .ToArray();
    }
}
