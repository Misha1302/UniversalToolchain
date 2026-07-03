namespace UniversalToolchain.ModuleContracts;

public sealed record PipelineEffectValidationResult(
    CompilerFactState OutputFacts,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics,
    IReadOnlyList<ReverificationRequest> ReverificationRequests);
