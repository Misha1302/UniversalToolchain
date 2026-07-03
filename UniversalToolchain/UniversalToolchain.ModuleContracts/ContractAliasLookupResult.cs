namespace UniversalToolchain.ModuleContracts;

public sealed record ContractAliasLookupResult(
    bool IsMatch,
    ContractId? Replacement,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics);
