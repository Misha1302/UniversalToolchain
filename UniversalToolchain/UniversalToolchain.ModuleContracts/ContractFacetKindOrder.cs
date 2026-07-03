namespace UniversalToolchain.ModuleContracts;

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

    public static int GetSortKey(ContractFacetKind kind) =>
        _order.TryGetValue(kind, out var value)
            ? value
            : Thrower.InvalidOpEx<int>($"Contract facet kind '{kind}' has no deterministic order.");

    public static IReadOnlyList<ToolchainDiagnostic> ValidateComplete()
    {
        return Enum.GetValues<ContractFacetKind>()
            .Where(static kind => !_order.ContainsKey(kind))
            .Select(static kind => new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.MissingFacetKindOrder,
                ToolchainDiagnosticSeverity.Error,
                $"Contract facet kind '{kind}' has no deterministic order.",
                null,
                [new ToolchainDiagnosticHint("Add the facet kind to ContractFacetKindOrder before using it in contract tables.")]))
            .ToArray();
    }
}
