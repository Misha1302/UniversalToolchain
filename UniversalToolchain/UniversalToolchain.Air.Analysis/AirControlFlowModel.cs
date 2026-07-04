using System.Collections.ObjectModel;
using IntermediateRepresentationAbstractions;

namespace UniversalToolchain.Air.Analysis;

public enum AirControlFlowEdgeKind
{
    Fallthrough,
    Jump,
    ConditionTrue,
    ConditionFalse
}

public enum AirBlockTerminatorKind
{
    Fallthrough,
    Jump,
    ConditionalJump,
    End,
    Invalid
}

public sealed record AirControlFlowEdge(
    AirBlockId Source,
    AirBlockId Target,
    AirControlFlowEdgeKind Kind);

public sealed class AirBlockTerminator
{
    public AirBlockTerminator(
        AirBlockTerminatorKind kind,
        Instruction? instruction = null,
        IEnumerable<AirControlFlowEdge>? successors = null,
        string? diagnostic = null)
    {
        Kind = kind;
        Instruction = instruction;
        Successors = new ReadOnlyCollection<AirControlFlowEdge>((successors ?? []).OrderBy(static x => x.Target).ThenBy(static x => x.Kind).ToList());
        Diagnostic = diagnostic;
    }

    public AirBlockTerminatorKind Kind { get; }

    public Instruction? Instruction { get; }

    public IReadOnlyList<AirControlFlowEdge> Successors { get; }

    public string? Diagnostic { get; }
}

public sealed class AirBasicBlock
{
    public AirBasicBlock(
        AirBlockId id,
        int startInstructionIndex,
        int endInstructionIndexExclusive,
        IEnumerable<Instruction> instructions,
        AirBlockTerminator terminator,
        IEnumerable<AirControlFlowEdge>? predecessors = null)
    {
        Id = id;
        StartInstructionIndex = startInstructionIndex;
        EndInstructionIndexExclusive = endInstructionIndexExclusive;
        Instructions = new ReadOnlyCollection<Instruction>(instructions.ToList());
        Terminator = terminator;
        Predecessors = new ReadOnlyCollection<AirControlFlowEdge>((predecessors ?? []).OrderBy(static x => x.Source).ThenBy(static x => x.Kind).ToList());
    }

    public AirBlockId Id { get; }

    public int StartInstructionIndex { get; }

    public int EndInstructionIndexExclusive { get; }

    public IReadOnlyList<Instruction> Instructions { get; }

    public AirBlockTerminator Terminator { get; }

    public IReadOnlyList<AirControlFlowEdge> Predecessors { get; }
}

public sealed class AirControlFlowGraph
{
    public AirControlFlowGraph(AirBlockId entryBlockId, IEnumerable<AirBasicBlock> blocks)
    {
        EntryBlockId = entryBlockId;
        Blocks = new ReadOnlyCollection<AirBasicBlock>(blocks.OrderBy(static x => x.StartInstructionIndex).ThenBy(static x => x.Id).ToList());
        BlocksById = Blocks.ToDictionary(static x => x.Id);
    }

    public AirBlockId EntryBlockId { get; }

    public IReadOnlyList<AirBasicBlock> Blocks { get; }

    public IReadOnlyDictionary<AirBlockId, AirBasicBlock> BlocksById { get; }
}

public sealed class AirControlFlowBuildResult
{
    public AirControlFlowBuildResult(AirControlFlowGraph graph, IEnumerable<string>? diagnostics = null)
    {
        Graph = graph;
        Diagnostics = new ReadOnlyCollection<string>((diagnostics ?? []).ToList());
    }

    public AirControlFlowGraph Graph { get; }

    public IReadOnlyList<string> Diagnostics { get; }
}
