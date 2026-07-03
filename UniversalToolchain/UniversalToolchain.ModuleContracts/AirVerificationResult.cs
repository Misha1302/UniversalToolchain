namespace UniversalToolchain.ModuleContracts;

public sealed record AirVerificationResult(
    bool IsValid,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics);
