using IntermediateRepresentationAbstractions;

namespace UniversalToolchain.Air.Analysis;

public sealed class AirControlFlowGraphBuilder
{
    public AirControlFlowBuildResult Build(IReadOnlyList<Instruction> instructions)
    {
        ArgumentNullException.ThrowIfNull(instructions);

        if (instructions.Count == 0)
        {
            var emptyEntry = new AirBlockId("b0000");
            var emptyBlock = new AirBasicBlock(
                emptyEntry,
                startInstructionIndex: 0,
                endInstructionIndexExclusive: 0,
                instructions: [],
                terminator: new AirBlockTerminator(AirBlockTerminatorKind.End));
            return new AirControlFlowBuildResult(new AirControlFlowGraph(emptyEntry, [emptyBlock]));
        }

        var diagnostics = new List<string>();
        var labels = BuildLabelIndex(instructions, diagnostics);
        var leaders = BuildLeaders(instructions, labels, diagnostics);
        var blockStarts = leaders.Order().ToArray();
        var blockIdsByStart = blockStarts.ToDictionary(static x => x, x => CreateBlockId(x, instructions[x]));

        var blocksWithoutPredecessors = new List<AirBasicBlock>();
        var allSuccessors = new List<AirControlFlowEdge>();
        for (var i = 0; i < blockStarts.Length; i++)
        {
            var start = blockStarts[i];
            var end = i + 1 < blockStarts.Length ? blockStarts[i + 1] : instructions.Count;
            var id = blockIdsByStart[start];
            var body = instructions.Skip(start).Take(end - start).ToList();
            var terminator = BuildTerminator(id, body, end, instructions.Count, blockIdsByStart, labels, diagnostics);
            allSuccessors.AddRange(terminator.Successors);

            blocksWithoutPredecessors.Add(new AirBasicBlock(
                id,
                start,
                end,
                body,
                terminator));
        }

        var predecessorsByTarget = allSuccessors
            .GroupBy(static x => x.Target)
            .ToDictionary(static x => x.Key, static x => (IReadOnlyList<AirControlFlowEdge>)x.OrderBy(static y => y.Source).ThenBy(static y => y.Kind).ToArray());

        var blocks = blocksWithoutPredecessors
            .Select(block => new AirBasicBlock(
                block.Id,
                block.StartInstructionIndex,
                block.EndInstructionIndexExclusive,
                block.Instructions,
                block.Terminator,
                predecessorsByTarget.TryGetValue(block.Id, out var predecessors) ? predecessors : []))
            .ToArray();

        return new AirControlFlowBuildResult(new AirControlFlowGraph(blocks[0].Id, blocks), diagnostics);
    }

    private static Dictionary<object, int> BuildLabelIndex(IReadOnlyList<Instruction> instructions, List<string> diagnostics)
    {
        var labels = new Dictionary<object, int>();
        for (var index = 0; index < instructions.Count; index++)
        {
            var instruction = instructions[index];
            if (instruction.UOpCode != UOpCode.Label)
                continue;

            if (instruction.Operands.Count != 1)
            {
                diagnostics.Add($"Instruction {index} has malformed Label operand count {instruction.Operands.Count}; expected 1.");
                continue;
            }

            var label = instruction.Operands[0];
            if (!labels.TryAdd(label, index))
                diagnostics.Add($"Duplicate AIR label '{label}' at instruction {index}.");
        }

        return labels;
    }

    private static SortedSet<int> BuildLeaders(
        IReadOnlyList<Instruction> instructions,
        IReadOnlyDictionary<object, int> labels,
        List<string> diagnostics)
    {
        var leaders = new SortedSet<int> { 0 };
        for (var index = 0; index < instructions.Count; index++)
        {
            var instruction = instructions[index];
            if (instruction.UOpCode == UOpCode.Label)
            {
                leaders.Add(index);
                continue;
            }

            if (!instruction.UOpCode.IsAnyJump())
                continue;

            if (TryReadJumpTarget(instruction, index, diagnostics, out var target) &&
                labels.TryGetValue(target!, out var targetIndex))
            {
                leaders.Add(targetIndex);
            }

            if (index + 1 < instructions.Count)
                leaders.Add(index + 1);
        }

        return leaders;
    }

    private static AirBlockTerminator BuildTerminator(
        AirBlockId blockId,
        IReadOnlyList<Instruction> body,
        int endIndex,
        int instructionCount,
        IReadOnlyDictionary<int, AirBlockId> blockIdsByStart,
        IReadOnlyDictionary<object, int> labels,
        List<string> diagnostics)
    {
        if (body.Count == 0)
            return new AirBlockTerminator(AirBlockTerminatorKind.End);

        var last = body[^1];
        if (last.UOpCode == UOpCode.Jmp)
        {
            if (!TryResolveTarget(last, endIndex - 1, blockIdsByStart, labels, diagnostics, out var target))
            {
                return new AirBlockTerminator(
                    AirBlockTerminatorKind.Invalid,
                    last,
                    diagnostic: $"Instruction {endIndex - 1} jumps to an unknown label.");
            }

            return new AirBlockTerminator(
                AirBlockTerminatorKind.Jump,
                last,
                [new AirControlFlowEdge(blockId, target, AirControlFlowEdgeKind.Jump)]);
        }

        if (last.UOpCode is UOpCode.JmpIf or UOpCode.JmpIfNot)
        {
            var successors = new List<AirControlFlowEdge>();
            if (TryResolveTarget(last, endIndex - 1, blockIdsByStart, labels, diagnostics, out var target))
            {
                var targetKind = last.UOpCode == UOpCode.JmpIf
                    ? AirControlFlowEdgeKind.ConditionTrue
                    : AirControlFlowEdgeKind.ConditionFalse;
                successors.Add(new AirControlFlowEdge(blockId, target, targetKind));
            }

            if (endIndex < instructionCount && blockIdsByStart.TryGetValue(endIndex, out var fallthrough))
            {
                var fallthroughKind = last.UOpCode == UOpCode.JmpIf
                    ? AirControlFlowEdgeKind.ConditionFalse
                    : AirControlFlowEdgeKind.ConditionTrue;
                successors.Add(new AirControlFlowEdge(blockId, fallthrough, fallthroughKind));
            }

            return new AirBlockTerminator(AirBlockTerminatorKind.ConditionalJump, last, successors);
        }

        if (endIndex < instructionCount && blockIdsByStart.TryGetValue(endIndex, out var nextBlock))
        {
            return new AirBlockTerminator(
                AirBlockTerminatorKind.Fallthrough,
                successors: [new AirControlFlowEdge(blockId, nextBlock, AirControlFlowEdgeKind.Fallthrough)]);
        }

        return new AirBlockTerminator(AirBlockTerminatorKind.End);
    }

    private static bool TryResolveTarget(
        Instruction instruction,
        int instructionIndex,
        IReadOnlyDictionary<int, AirBlockId> blockIdsByStart,
        IReadOnlyDictionary<object, int> labels,
        List<string> diagnostics,
        out AirBlockId target)
    {
        target = default;
        if (!TryReadJumpTarget(instruction, instructionIndex, diagnostics, out var targetLabel))
            return false;

        if (!labels.TryGetValue(targetLabel!, out var targetIndex) ||
            !blockIdsByStart.TryGetValue(targetIndex, out target))
        {
            diagnostics.Add($"Instruction {instructionIndex} jumps to unknown AIR label '{targetLabel}'.");
            return false;
        }

        return true;
    }

    private static bool TryReadJumpTarget(
        Instruction instruction,
        int instructionIndex,
        List<string> diagnostics,
        out object? target)
    {
        target = null;
        if (instruction.Operands.Count != 1)
        {
            diagnostics.Add($"Instruction {instructionIndex} has malformed {instruction.UOpCode} operand count {instruction.Operands.Count}; expected 1.");
            return false;
        }

        target = instruction.Operands[0];
        return true;
    }

    private static AirBlockId CreateBlockId(int startInstructionIndex, Instruction firstInstruction)
    {
        if (firstInstruction.UOpCode == UOpCode.Label && firstInstruction.Operands.Count == 1)
            return new AirBlockId("label_" + Sanitize(firstInstruction.Operands[0].ToString() ?? "unknown"));

        return new AirBlockId($"b{startInstructionIndex:0000}");
    }

    private static string Sanitize(string value)
    {
        var chars = value.Select(static c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        var sanitized = new string(chars).Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
    }
}
