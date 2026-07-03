namespace UniversalToolchain.ModuleContracts;

public sealed record AirEmissionContract(
    BytecodePatternId SourcePattern,
    IReadOnlyList<AirPatternId> MayEmitPatterns,
    IReadOnlyList<IntrinsicSymbolId> MayEmitIntrinsics,
    IReadOnlyList<BackendCapabilityId> RequiredCapabilities);
