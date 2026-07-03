namespace UniversalToolchain.ModuleContracts;

public sealed record ObservedBytecodeEmission(
    ModuleId ProducerModule,
    AstNodeKind SourceNode,
    IReadOnlyList<BytecodeTagId> Tags,
    IReadOnlyList<BytecodePatternId> Patterns,
    StackEffect? ObservedStackEffect = null);
