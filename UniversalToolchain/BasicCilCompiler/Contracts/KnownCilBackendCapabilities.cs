using UniversalToolchain.ModuleContracts;

namespace BasicCilCompiler.Contracts;

public static class KnownCilBackendCapabilities
{
    public static BackendCapabilityId DynamicMethods { get; } = new("cil.backend.dynamic-methods");

    public static BackendCapabilityId NativeNumericIntrinsics { get; } = new("cil.backend.native-numeric-intrinsics");

    public static BackendCapabilityId NativeComparisonIntrinsics { get; } = new("cil.backend.native-comparison-intrinsics");

    public static BackendCapabilityId LocalSlotOptimization { get; } = new("cil.backend.local-slot-optimization");
}
