using System.Collections.ObjectModel;
using IntermediateRepresentationAbstractions;

namespace UniversalToolchain.Air.Analysis;

public sealed class AirStackState : IEquatable<AirStackState>
{
    private readonly ReadOnlyCollection<AirValueTypeId> _types;

    public AirStackState(IEnumerable<AirValueTypeId>? types = null)
    {
        _types = new ReadOnlyCollection<AirValueTypeId>((types ?? []).ToList());
    }

    public static AirStackState Empty { get; } = new();

    public IReadOnlyList<AirValueTypeId> Types => _types;

    public AirStackState Push(AirValueTypeId type) => new(_types.Concat([type]));

    public bool TryPop(out AirValueTypeId type, out AirStackState next)
    {
        if (_types.Count == 0)
        {
            type = default;
            next = this;
            return false;
        }

        type = _types[^1];
        next = new AirStackState(_types.Take(_types.Count - 1));
        return true;
    }

    public bool Equals(AirStackState? other) =>
        other is not null && _types.SequenceEqual(other._types);

    public override bool Equals(object? obj) => Equals(obj as AirStackState);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var type in _types)
            hash.Add(type);
        return hash.ToHashCode();
    }

    public override string ToString() => "[" + string.Join(", ", _types) + "]";
}

public sealed class AirStackAnalysisResult
{
    public AirStackAnalysisResult(
        IReadOnlyDictionary<AirBlockId, AirStackState> entryStates,
        IReadOnlyDictionary<AirBlockId, AirStackState> exitStates,
        IEnumerable<string>? diagnostics = null)
    {
        EntryStates = new ReadOnlyDictionary<AirBlockId, AirStackState>(new Dictionary<AirBlockId, AirStackState>(entryStates));
        ExitStates = new ReadOnlyDictionary<AirBlockId, AirStackState>(new Dictionary<AirBlockId, AirStackState>(exitStates));
        Diagnostics = new ReadOnlyCollection<string>((diagnostics ?? []).ToList());
    }

    public IReadOnlyDictionary<AirBlockId, AirStackState> EntryStates { get; }

    public IReadOnlyDictionary<AirBlockId, AirStackState> ExitStates { get; }

    public IReadOnlyList<string> Diagnostics { get; }
}

public sealed class AirStackAnalyzer
{
    private readonly IAirIntrinsicDescriptorResolver _intrinsicResolver;

    public AirStackAnalyzer(AirIntrinsicDescriptorSet? intrinsics = null)
        : this((IAirIntrinsicDescriptorResolver)(intrinsics ?? AirIntrinsicDescriptorSet.Empty))
    {
    }

    public AirStackAnalyzer(IAirIntrinsicDescriptorResolver? intrinsicResolver)
    {
        _intrinsicResolver = intrinsicResolver ?? AirIntrinsicDescriptorSet.Empty;
    }

    public AirStackAnalysisResult Analyze(AirControlFlowGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var diagnostics = new List<string>();
        var entryStates = new Dictionary<AirBlockId, AirStackState>
        {
            [graph.EntryBlockId] = AirStackState.Empty
        };
        var exitStates = new Dictionary<AirBlockId, AirStackState>();
        var pending = new Queue<AirBlockId>();
        pending.Enqueue(graph.EntryBlockId);

        while (pending.Count > 0)
        {
            var blockId = pending.Dequeue();
            if (!graph.BlocksById.TryGetValue(blockId, out var block) ||
                !entryStates.TryGetValue(blockId, out var entryState))
            {
                continue;
            }

            var exitState = Simulate(block, entryState, diagnostics, _intrinsicResolver);
            if (exitStates.TryGetValue(blockId, out var previousExit) && previousExit.Equals(exitState))
                continue;

            exitStates[blockId] = exitState;
            foreach (var edge in block.Terminator.Successors.OrderBy(static x => x.Target).ThenBy(static x => x.Kind))
            {
                if (MergeEntry(edge, exitState, entryStates, diagnostics))
                    pending.Enqueue(edge.Target);
            }
        }

        VerifyTerminalStates(graph, exitStates, diagnostics);
        return new AirStackAnalysisResult(entryStates, exitStates, diagnostics);
    }

    private static void VerifyTerminalStates(
        AirControlFlowGraph graph,
        IReadOnlyDictionary<AirBlockId, AirStackState> exitStates,
        List<string> diagnostics)
    {
        AirStackState? canonical = null;
        AirBlockId canonicalBlock = default;

        foreach (var block in graph.Blocks
                     .Where(static block => block.Terminator.Successors.Count == 0)
                     .OrderBy(static block => block.Id))
        {
            if (!exitStates.TryGetValue(block.Id, out var state))
                continue;

            if (state.Types.Count > 1)
            {
                diagnostics.Add(
                    $"AIR terminal block '{block.Id}' finishes with {state.Types.Count} evaluation-stack values {state}; expected zero or one.");
                continue;
            }

            if (canonical is null)
            {
                canonical = state;
                canonicalBlock = block.Id;
                continue;
            }

            if (!canonical.Equals(state))
            {
                diagnostics.Add(
                    $"AIR terminal blocks '{canonicalBlock}' and '{block.Id}' expose incompatible return-stack shapes: " +
                    $"{canonical} versus {state}.");
            }
        }
    }

    private static bool MergeEntry(
        AirControlFlowEdge edge,
        AirStackState incoming,
        Dictionary<AirBlockId, AirStackState> entryStates,
        List<string> diagnostics)
    {
        if (!entryStates.TryGetValue(edge.Target, out var current))
        {
            entryStates[edge.Target] = incoming;
            return true;
        }

        if (current.Equals(incoming))
            return false;

        diagnostics.Add($"Incompatible AIR stack state at edge '{edge.Source}' -> '{edge.Target}': current {current}, incoming {incoming}.");
        return false;
    }

    private static AirStackState Simulate(
        AirBasicBlock block,
        AirStackState entryState,
        List<string> diagnostics,
        IAirIntrinsicDescriptorResolver intrinsicResolver)
    {
        var state = entryState;
        for (var offset = 0; offset < block.Instructions.Count; offset++)
        {
            var instruction = block.Instructions[offset];
            var instructionIndex = block.StartInstructionIndex + offset;
            state = SimulateInstruction(block.Id, instructionIndex, instruction, state, diagnostics, intrinsicResolver);
        }

        return state;
    }

    private static AirStackState SimulateInstruction(
        AirBlockId blockId,
        int instructionIndex,
        Instruction instruction,
        AirStackState state,
        List<string> diagnostics,
        IAirIntrinsicDescriptorResolver intrinsicResolver)
    {
        switch (instruction.UOpCode)
        {
            case UOpCode.Nop:
            case UOpCode.Label:
            case UOpCode.Annotate:
                return state;
            case UOpCode.Push:
                return SimulatePush(blockId, instructionIndex, instruction, state, diagnostics);
            case UOpCode.Drop:
                return PopAny(blockId, instructionIndex, instruction, state, diagnostics);
            case UOpCode.Jmp:
                return state;
            case UOpCode.JmpIf:
            case UOpCode.JmpIfNot:
                return PopCondition(blockId, instructionIndex, instruction, state, diagnostics);
            case UOpCode.Intrinsic:
                return SimulateIntrinsic(blockId, instructionIndex, instruction, state, diagnostics, intrinsicResolver);
            default:
                diagnostics.Add($"AIR instruction {instructionIndex} in block '{blockId}' uses unsupported opcode '{instruction.UOpCode}'.");
                return state;
        }
    }

    private static AirStackState SimulateIntrinsic(
        AirBlockId blockId,
        int instructionIndex,
        Instruction instruction,
        AirStackState state,
        List<string> diagnostics,
        IAirIntrinsicDescriptorResolver intrinsicResolver)
    {
        if (!AirIntrinsicInvocationReader.TryRead(instruction, out var invocation, out var intrinsicId, out var payloadDiagnostic))
        {
            diagnostics.Add($"AIR Intrinsic at instruction {instructionIndex} in block '{blockId}' has an invalid payload. {payloadDiagnostic}");
            return state;
        }

        if (!intrinsicResolver.TryResolve(instruction, out var descriptor, out var diagnostic))
        {
            diagnostics.Add($"AIR instruction {instructionIndex} in block '{blockId}' uses unsupported Intrinsic '{intrinsicId}' for generic stack analysis. {diagnostic}");
            return state;
        }

        var dataOperandCount = invocation.DataOperands.Count;
        if (dataOperandCount != descriptor.DataOperandCount)
        {
            diagnostics.Add($"AIR Intrinsic '{intrinsicId}' at instruction {instructionIndex} in block '{blockId}' has {dataOperandCount} data operands; expected {descriptor.DataOperandCount}.");
            return state;
        }

        var next = state;
        for (var index = descriptor.ParameterTypes.Count - 1; index >= 0; index--)
        {
            var expectedType = descriptor.ParameterTypes[index];
            if (!next.TryPop(out var actualType, out next))
            {
                diagnostics.Add($"AIR Intrinsic '{intrinsicId}' at instruction {instructionIndex} in block '{blockId}' consumes an empty stack.");
                return state;
            }

            if (actualType != expectedType)
            {
                diagnostics.Add($"AIR Intrinsic '{intrinsicId}' at instruction {instructionIndex} in block '{blockId}' expects '{expectedType}' but consumes '{actualType}'.");
            }
        }

        foreach (var resultType in descriptor.ResultTypes)
            next = next.Push(resultType);

        return next;
    }

    private static AirStackState SimulatePush(
        AirBlockId blockId,
        int instructionIndex,
        Instruction instruction,
        AirStackState state,
        List<string> diagnostics)
    {
        if (instruction.Operands.Count != 1)
        {
            diagnostics.Add($"AIR Push at instruction {instructionIndex} in block '{blockId}' has {instruction.Operands.Count} operands; expected 1.");
            return state;
        }

        return instruction.Operands[0] switch
        {
            bool => state.Push(AirValueTypes.Bool),
            int => state.Push(AirValueTypes.Int32),
            double => state.Push(AirValueTypes.Float64),
            string => state.Push(AirValueTypes.Object),
            AirConstant constant when TryMapConstantType(constant.DeclaredType, out var constantType) => state.Push(constantType),
            AirExternalValueReference external when TryMapExternalType(external.ValueType, out var externalType) => state.Push(externalType),
            _ => UnsupportedPush(blockId, instructionIndex, instruction, state, diagnostics)
        };
    }

    private static bool TryMapConstantType(Type type, out AirValueTypeId airType)
    {
        if (TryMapExternalType(type, out airType))
            return true;

        if (!type.IsValueType || Nullable.GetUnderlyingType(type) is not null)
        {
            airType = AirValueTypes.Object;
            return true;
        }

        airType = default;
        return false;
    }

    private static bool TryMapExternalType(Type type, out AirValueTypeId airType)
    {
        if (type == typeof(bool))
        {
            airType = AirValueTypes.Bool;
            return true;
        }

        if (type == typeof(int))
        {
            airType = AirValueTypes.Int32;
            return true;
        }

        if (type == typeof(double))
        {
            airType = AirValueTypes.Float64;
            return true;
        }

        airType = default;
        return false;
    }

    private static AirStackState UnsupportedPush(
        AirBlockId blockId,
        int instructionIndex,
        Instruction instruction,
        AirStackState state,
        List<string> diagnostics)
    {
        diagnostics.Add($"AIR Push at instruction {instructionIndex} in block '{blockId}' has unsupported value type '{instruction.Operands[0]?.GetType().FullName ?? "<null>"}'.");
        return state;
    }

    private static AirStackState PopAny(
        AirBlockId blockId,
        int instructionIndex,
        Instruction instruction,
        AirStackState state,
        List<string> diagnostics)
    {
        if (instruction.Operands.Count != 0)
            diagnostics.Add($"AIR {instruction.UOpCode} at instruction {instructionIndex} in block '{blockId}' has operands; expected none.");

        if (!state.TryPop(out _, out var next))
        {
            diagnostics.Add($"AIR stack underflow at instruction {instructionIndex} in block '{blockId}'.");
            return state;
        }

        return next;
    }

    private static AirStackState PopCondition(
        AirBlockId blockId,
        int instructionIndex,
        Instruction instruction,
        AirStackState state,
        List<string> diagnostics)
    {
        if (!state.TryPop(out var type, out var next))
        {
            diagnostics.Add($"AIR conditional jump at instruction {instructionIndex} in block '{blockId}' consumes an empty stack.");
            return state;
        }

        if (type != AirValueTypes.Bool)
            diagnostics.Add($"AIR conditional jump at instruction {instructionIndex} in block '{blockId}' expects '{AirValueTypes.Bool}' but consumes '{type}'.");

        return next;
    }
}
