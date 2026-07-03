namespace UniversalToolchain.ModuleContracts;

public sealed class BytecodeObservedEmissionReader : IBytecodeObservedEmissionReader
{
    public IReadOnlyList<ObservedBytecodeEmission> Read(Bytecode bytecode)
    {
        return ReadWithDiagnostics(bytecode).ObservedEmissions;
    }

    public BytecodeObservedEmissionReadResult ReadWithDiagnostics(Bytecode bytecode)
    {
        bytecode = bytecode.ArgNotNull();

        var diagnostics = new List<ToolchainDiagnostic>();
        var emissions = new List<ObservedBytecodeEmission>();
        foreach (var instruction in bytecode.Instructions)
        {
            diagnostics.AddRange(BytecodeContractMetadata.Validate(instruction));
            var emission = ReadInstruction(instruction);
            if (emission != null)
                emissions.Add(emission);
        }

        return new BytecodeObservedEmissionReadResult(emissions, diagnostics);
    }

    private static ObservedBytecodeEmission? ReadInstruction(BytecodeInstruction instruction)
    {
        var patterns = BytecodeContractMetadata.ReadPatterns(instruction);
        var tags = BytecodeContractMetadata.ReadSemanticTags(instruction);
        if (patterns.Count == 0 && tags.Count == 0)
            return null;

        if (!BytecodeContractMetadata.TryReadProducerModule(instruction, out var producerModule))
            return null;

        if (!BytecodeContractMetadata.TryReadSourceNode(instruction, out var sourceNode))
            sourceNode = KnownCoreAstNodeKinds.Unknown;

        return new ObservedBytecodeEmission(
            producerModule,
            sourceNode,
            tags,
            patterns);
    }
}

public sealed record BytecodeObservedEmissionReadResult(
    IReadOnlyList<ObservedBytecodeEmission> ObservedEmissions,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics);
