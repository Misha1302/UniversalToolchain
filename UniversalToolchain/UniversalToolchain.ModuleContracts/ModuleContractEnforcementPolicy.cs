namespace UniversalToolchain.ModuleContracts;

public sealed class ModuleContractEnforcementPolicy
{
    public ModuleContractEnforcementPolicy(
        IEnumerable<ModuleContractStatusDeclaration> explicitStatuses,
        bool requireNewModulesDeclared)
    {
        explicitStatuses = explicitStatuses.ArgNotNull();

        ExplicitStatuses = explicitStatuses
            .OrderBy(static x => x.ModuleId.Value, StringComparer.Ordinal)
            .ToDictionary(static x => x.ModuleId, static x => x.Status);
        RequireNewModulesDeclared = requireNewModulesDeclared;
    }

    public IReadOnlyDictionary<ModuleId, ModuleContractCompatibilityStatus> ExplicitStatuses { get; }

    public bool RequireNewModulesDeclared { get; }

    public static ModuleContractEnforcementPolicy AllowUndeclared { get; } = new([], false);

    public static ModuleContractEnforcementPolicy EnforceNewModules(
        IEnumerable<ModuleContractStatusDeclaration> acceptedUndeclaredModules) =>
        new(acceptedUndeclaredModules, true);

    public bool TryGetExplicitStatus(
        ModuleId moduleId,
        out ModuleContractCompatibilityStatus status) =>
        ExplicitStatuses.TryGetValue(moduleId, out status);
}
