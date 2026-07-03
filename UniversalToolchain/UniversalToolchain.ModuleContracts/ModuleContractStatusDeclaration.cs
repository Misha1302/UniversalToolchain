namespace UniversalToolchain.ModuleContracts;

public sealed record ModuleContractStatusDeclaration(
    ModuleId ModuleId,
    ModuleContractCompatibilityStatus Status);
