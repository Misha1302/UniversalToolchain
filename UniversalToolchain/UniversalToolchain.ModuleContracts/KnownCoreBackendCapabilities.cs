namespace UniversalToolchain.ModuleContracts;

public static class KnownCoreBackendCapabilities
{
    public static BackendCapabilityId UniversalCall { get; } = new("core.backend.universal-call");

    public static BackendCapabilityId ObjectConstruction { get; } = new("core.backend.object-construction");

    public static BackendCapabilityId LocalVariables { get; } = new("core.backend.local-variables");

    public static BackendCapabilityId ExternalBindings { get; } = new("core.backend.external-bindings");

    public static BackendCapabilityId MutableState { get; } = new("core.backend.mutable-state");

    public static BackendCapabilityId UnconditionalBranches { get; } = new("core.backend.unconditional-branches");

    public static BackendCapabilityId ConditionalBranches { get; } = new("core.backend.conditional-branches");

    public static BackendCapabilityId BooleanValues { get; } = new("core.backend.boolean-values");

    public static BackendCapabilityId NumericValues { get; } = new("core.backend.numeric-values");

    public static BackendCapabilityId Comparisons { get; } = new("core.backend.comparisons");

    public static BackendCapabilityId Assignments { get; } = new("core.backend.assignments");

    public static BackendCapabilityId RuntimeProviderCalls { get; } = new("core.backend.runtime-provider-calls");

    public static BackendCapabilityId FunctionCalls { get; } = new("core.backend.function-calls");

    public static BackendCapabilityId TypedIntrinsics { get; } = new("core.backend.typed-intrinsics");

    public static BackendCapabilityFacet CreateFacet() =>
        new(
            KnownCoreModuleIds.BackendCapabilities,
            [
                new BackendCapabilityContract(UniversalCall, []),
                new BackendCapabilityContract(ObjectConstruction, []),
                new BackendCapabilityContract(LocalVariables, []),
                new BackendCapabilityContract(ExternalBindings, []),
                new BackendCapabilityContract(MutableState, []),
                new BackendCapabilityContract(UnconditionalBranches, []),
                new BackendCapabilityContract(ConditionalBranches, []),
                new BackendCapabilityContract(BooleanValues, []),
                new BackendCapabilityContract(NumericValues, []),
                new BackendCapabilityContract(Comparisons, []),
                new BackendCapabilityContract(Assignments, []),
                new BackendCapabilityContract(RuntimeProviderCalls, []),
                new BackendCapabilityContract(FunctionCalls, []),
                new BackendCapabilityContract(TypedIntrinsics, [])
            ]);
}
