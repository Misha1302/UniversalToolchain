namespace UniversalToolchain.ModuleContracts;

public sealed record AstOwnershipContract(
    AstNodeKind NodeKind,
    AstOwnershipMode Mode,
    ModuleId OwnerModule,
    IReadOnlyList<ModuleId> CooperatingModules);
