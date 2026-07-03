namespace UniversalToolchain.ModuleContracts;

public sealed record BytecodeVerificationResult(
    bool IsValid,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics);
