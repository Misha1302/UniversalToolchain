namespace UniversalToolchain.ModuleContracts;

public sealed record BackendCapabilityContract(
    BackendCapabilityId CapabilityId,
    IReadOnlyList<IntrinsicSymbolId> SupportedIntrinsics);
