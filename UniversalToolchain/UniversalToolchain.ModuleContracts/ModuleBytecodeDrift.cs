namespace UniversalToolchain.ModuleContracts;

public sealed record ModuleBytecodeDrift(
    ModuleId ModuleId,
    IReadOnlyList<BytecodeTagId> ObservedUndeclaredTags,
    IReadOnlyList<BytecodePatternId> ObservedUndeclaredPatterns,
    IReadOnlyList<BytecodeTagId> DeclaredUnobservedTags,
    IReadOnlyList<BytecodePatternId> DeclaredUnobservedPatterns)
{
    public bool HasDrift =>
        ObservedUndeclaredTags.Count > 0
        || ObservedUndeclaredPatterns.Count > 0
        || DeclaredUnobservedTags.Count > 0
        || DeclaredUnobservedPatterns.Count > 0;
}
