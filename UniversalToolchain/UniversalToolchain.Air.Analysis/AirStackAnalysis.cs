using System.Collections.ObjectModel;
using IntermediateRepresentationAbstractions;

namespace UniversalToolchain.Air.Analysis;

/// <summary>
/// Outcome of attempting to pop from an <see cref="AirStackState"/>.
/// </summary>
public enum AirStackPopResult
{
    /// <summary>A value was popped.</summary>
    Popped,

    /// <summary>The stack is known to be empty. This is a fact about the program.</summary>
    Underflow,

    /// <summary>
    /// The stack state is unknown because the analyzer met a construct it does not model. This is a fact
    /// about the analyzer, not about the program, and it must not be reported as an underflow.
    /// </summary>
    Unknown
}

public sealed class AirStackState : IEquatable<AirStackState>
{
    private readonly ReadOnlyCollection<AirValueTypeId> _types;

    public AirStackState(IEnumerable<AirValueTypeId>? types = null, bool isUnknown = false)
    {
        _types = new ReadOnlyCollection<AirValueTypeId>((types ?? []).ToList());
        IsUnknown = isUnknown;
    }

    public static AirStackState Empty { get; } = new();

    /// <summary>
    /// A state the analyzer could not compute, because it met a construct whose stack effect it does not
    /// model. Propagating an unknown state is the only honest option: continuing from the last known state
    /// would produce conclusions about a stack shape that never existed.
    /// </summary>
    public static AirStackState Unknown { get; } = new(isUnknown: true);

    public bool IsUnknown { get; }

    public IReadOnlyList<AirValueTypeId> Types => _types;

    public AirStackState Push(AirValueTypeId type) => IsUnknown ? this : new(_types.Concat([type]));

    /// <summary>
    /// Attempts to pop one value.
    /// </summary>
    /// <remarks>
    /// A failed pop has two very different causes, and conflating them fabricates diagnostics: an
    /// <see cref="AirStackPopResult.Underflow"/> is a fact about the program, while
    /// <see cref="AirStackPopResult.Unknown"/> is a fact about the analyzer's knowledge. The result is a
    /// three-valued outcome so that a caller cannot handle one case and silently misreport the other.
    /// </remarks>
    public AirStackPopResult TryPop(out AirValueTypeId type, out AirStackState next)
    {
        if (IsUnknown)
        {
            type = default;
            next = this;
            return AirStackPopResult.Unknown;
        }

        if (_types.Count == 0)
        {
            type = default;
            next = this;
            return AirStackPopResult.Underflow;
        }

        type = _types[^1];
        next = new AirStackState(_types.Take(_types.Count - 1));
        return AirStackPopResult.Popped;
    }

    public bool Equals(AirStackState? other) =>
        other is not null && IsUnknown == other.IsUnknown && _types.SequenceEqual(other._types);

    public override bool Equals(object? obj) => Equals(obj as AirStackState);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(IsUnknown);
        foreach (var type in _types)
            hash.Add(type);
        return hash.ToHashCode();
    }

    public override string ToString() => IsUnknown ? "[unknown]" : "[" + string.Join(", ", _types) + "]";
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

            // An unknown state is not a stack shape. Reporting it here would fabricate a conclusion the
            // analyzer did not compute; the cause was already reported where it occurred.
            if (state.IsUnknown)
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

        // Unknown is not a conflicting shape: the analyzer lost track, it did not observe a disagreement.
        // A known state still wins, so a real incompatibility elsewhere in the CFG keeps being reported.
        if (incoming.IsUnknown)
            return false;
        if (current.IsUnknown)
        {
            entryStates[edge.Target] = incoming;
            return true;
        }

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
                // An opcode outside the modelled set has an unknown stack effect, exactly like an
                // unmodelled intrinsic. Keeping the previous state would attribute its inputs to whatever
                // comes next.
                diagnostics.Add($"AIR instruction {instructionIndex} in block '{blockId}' uses unsupported opcode '{instruction.UOpCode}'.");
                return AirStackState.Unknown;
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
            return AirStackState.Unknown;
        }

        if (!intrinsicResolver.TryResolve(instruction, out var descriptor, out var diagnostic))
        {
            // The analyzer does not know this intrinsic's stack effect, so it cannot continue reasoning about
            // the stack. It reports the cause and marks the state unknown; the remaining instructions of the
            // block are still walked only to surface further *independent* causes, but no stack-shape
            // conclusion is drawn from a state the analyzer could not compute.
            diagnostics.Add($"AIR instruction {instructionIndex} in block '{blockId}' uses unsupported Intrinsic '{intrinsicId}' for generic stack analysis. {diagnostic}");
            return AirStackState.Unknown;
        }

        var dataOperandCount = invocation.DataOperands.Count;
        if (dataOperandCount != descriptor.DataOperandCount)
        {
            diagnostics.Add($"AIR Intrinsic '{intrinsicId}' at instruction {instructionIndex} in block '{blockId}' has {dataOperandCount} data operands; expected {descriptor.DataOperandCount}.");
            return AirStackState.Unknown;
        }

        var next = state;
        for (var index = descriptor.ParameterTypes.Count - 1; index >= 0; index--)
        {
            var expectedType = descriptor.ParameterTypes[index];
            var pop = next.TryPop(out var actualType, out next);
            if (pop == AirStackPopResult.Unknown)
            {
                // The stack shape is unknown because of an earlier unmodelled construct, so operand
                // consumption cannot be judged. The cause was already reported where it occurred.
                return AirStackState.Unknown;
            }

            if (pop == AirStackPopResult.Underflow)
            {
                diagnostics.Add($"AIR Intrinsic '{intrinsicId}' at instruction {instructionIndex} in block '{blockId}' consumes an empty stack.");
                return AirStackState.Unknown;
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
            return AirStackState.Unknown;
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
        return AirStackState.Unknown;
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

        var pop = state.TryPop(out _, out var next);
        if (pop == AirStackPopResult.Unknown)
            return AirStackState.Unknown;

        if (pop == AirStackPopResult.Underflow)
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
        var pop = state.TryPop(out var type, out var next);
        if (pop == AirStackPopResult.Unknown)
            return AirStackState.Unknown;

        if (pop == AirStackPopResult.Underflow)
        {
            diagnostics.Add($"AIR conditional jump at instruction {instructionIndex} in block '{blockId}' consumes an empty stack.");
            return state;
        }

        if (type != AirValueTypes.Bool)
            diagnostics.Add($"AIR conditional jump at instruction {instructionIndex} in block '{blockId}' expects '{AirValueTypes.Bool}' but consumes '{type}'.");

        return next;
    }
}
