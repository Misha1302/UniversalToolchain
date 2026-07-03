namespace UniversalToolchain.ModuleContracts;

public sealed record LoweringResult(
    Bytecode Bytecode,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics);
