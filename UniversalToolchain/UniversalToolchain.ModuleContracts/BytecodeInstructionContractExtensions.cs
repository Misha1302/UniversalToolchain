namespace UniversalToolchain.ModuleContracts;

public static class BytecodeInstructionContractExtensions
{
    public static BytecodeInstruction WithContract(
        this BytecodeInstruction instruction,
        ModuleId producerModule,
        AstNodeKind sourceNode,
        BytecodePatternId pattern,
        params BytecodeTagId[] semanticTags) =>
        instruction.WithContracts(
            producerModule,
            sourceNode,
            [pattern],
            semanticTags);

    public static BytecodeInstruction WithContracts(
        this BytecodeInstruction instruction,
        ModuleId producerModule,
        AstNodeKind sourceNode,
        IEnumerable<BytecodePatternId> patterns,
        IEnumerable<BytecodeTagId>? semanticTags = null)
    {
        instruction = instruction.ArgNotNull();
        patterns = patterns.ArgNotNull();

        instruction.Tags.Add(BytecodeContractMetadata.ProducerModule(producerModule));
        instruction.Tags.Add(BytecodeContractMetadata.SourceNode(sourceNode));

        foreach (var pattern in patterns)
            instruction.Tags.Add(BytecodeContractMetadata.Pattern(pattern));

        if (semanticTags != null)
        {
            foreach (var tag in semanticTags)
                instruction.Tags.Add(BytecodeContractMetadata.SemanticTag(tag));
        }

        return instruction;
    }
}
