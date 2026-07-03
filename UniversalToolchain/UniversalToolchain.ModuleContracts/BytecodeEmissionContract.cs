namespace UniversalToolchain.ModuleContracts;

public sealed record BytecodeEmissionContract(
    AstNodeKind SourceNode,
    IReadOnlyList<BytecodeTagId> MayEmitTags,
    IReadOnlyList<BytecodePatternId> MayEmitPatterns,
    StackEffect DeclaredStackEffect,
    SideEffectPolicy SideEffects);
