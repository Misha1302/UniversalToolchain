namespace UniversalToolchain.ModuleContracts;

public sealed record PipelineEffectValidationRequest(
    SelectedModuleContractTable ContractTable,
    CompilerPipelineStage Stage,
    CompilerFactState InputFacts,
    CompilerFactVerifierRegistry VerifierRegistry,
    IReadOnlyList<ModuleId>? PipelineOrder = null);
