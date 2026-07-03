namespace UniversalToolchain.ModuleContracts;

public sealed record CompilerFactOwnershipContract(
    CompilerFactId FactId,
    ModuleId OwnerModule);
