using UniversalToolchain.ModuleContracts;

namespace BasicInterpreter.Contracts;

public static class KnownInterpreterBackendCapabilities
{
    public static BackendCapabilityId UniversalIntrinsicsOnly { get; } = new("interpreter.backend.universal-intrinsics-only");

    public static BackendCapabilityId NoNativeCilIntrinsics { get; } = new("interpreter.backend.no-native-cil-intrinsics");
}
