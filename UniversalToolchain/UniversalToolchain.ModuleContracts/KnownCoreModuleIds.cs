namespace UniversalToolchain.ModuleContracts;

public static class KnownCoreModuleIds
{
    public static ModuleId CompilerFacts { get; } = new("core.compiler-facts");

    public static ModuleId BackendCapabilities { get; } = new("core.backend-capabilities");
}
