using System.Collections.ObjectModel;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

public sealed record SsaValueDefinition(
    SsaValue Value,
    SsaBlockId BlockId,
    int InstructionIndex,
    ISsaInstruction? Instruction);

public sealed record SsaValueUse(
    SsaValueId ValueId,
    SsaBlockId BlockId,
    int InstructionIndex,
    ISsaInstruction? Instruction,
    string UseKind);

public sealed class SsaControlFlowGraph
{
    public SsaControlFlowGraph(SsaFunction function)
    {
        ArgumentNullException.ThrowIfNull(function);

        Blocks = new ReadOnlyDictionary<SsaBlockId, SsaBlock>(
            function.Blocks.ToDictionary(static x => x.Id));

        var successors = new Dictionary<SsaBlockId, IReadOnlyList<SsaBlockId>>();
        var predecessors = Blocks.Keys.ToDictionary(static x => x, static _ => new List<SsaBlockId>());

        foreach (var block in function.Blocks)
        {
            var blockSuccessors = block.Terminator?.Transfers
                .Select(static x => x.Target)
                .Where(Blocks.ContainsKey)
                .Distinct()
                .Order()
                .ToArray() ?? [];

            successors[block.Id] = blockSuccessors;
            foreach (var successor in blockSuccessors)
                predecessors[successor].Add(block.Id);
        }

        Successors = new ReadOnlyDictionary<SsaBlockId, IReadOnlyList<SsaBlockId>>(successors);
        Predecessors = new ReadOnlyDictionary<SsaBlockId, IReadOnlyList<SsaBlockId>>(
            predecessors.ToDictionary(
                static x => x.Key,
                static x => (IReadOnlyList<SsaBlockId>)x.Value.Order().ToArray()));
    }

    public IReadOnlyDictionary<SsaBlockId, SsaBlock> Blocks { get; }

    public IReadOnlyDictionary<SsaBlockId, IReadOnlyList<SsaBlockId>> Successors { get; }

    public IReadOnlyDictionary<SsaBlockId, IReadOnlyList<SsaBlockId>> Predecessors { get; }
}

public sealed class SsaUseDefMap
{
    private SsaUseDefMap(
        IReadOnlyDictionary<SsaValueId, SsaValueDefinition> definitions,
        IReadOnlyDictionary<SsaValueId, IReadOnlyList<SsaValueUse>> uses)
    {
        Definitions = definitions;
        Uses = uses;
    }

    public IReadOnlyDictionary<SsaValueId, SsaValueDefinition> Definitions { get; }

    public IReadOnlyDictionary<SsaValueId, IReadOnlyList<SsaValueUse>> Uses { get; }

    public static SsaUseDefMap Build(SsaFunction function)
    {
        ArgumentNullException.ThrowIfNull(function);

        var definitions = new Dictionary<SsaValueId, SsaValueDefinition>();
        var uses = new Dictionary<SsaValueId, List<SsaValueUse>>();

        foreach (var parameter in function.Parameters)
        {
            definitions[parameter.Value.Id] = new SsaValueDefinition(
                parameter.Value,
                function.EntryBlockId,
                InstructionIndex: -1,
                Instruction: null);
        }

        foreach (var block in function.Blocks)
        {
            foreach (var parameter in block.Parameters)
            {
                definitions[parameter.Value.Id] = new SsaValueDefinition(
                    parameter.Value,
                    block.Id,
                    InstructionIndex: -1,
                    Instruction: null);
            }

            for (var index = 0; index < block.Instructions.Count; index++)
            {
                var instruction = block.Instructions[index];
                foreach (var result in instruction.Results)
                {
                    definitions[result.Id] = new SsaValueDefinition(
                        result,
                        block.Id,
                        index,
                        instruction);
                }

                foreach (var operand in instruction.Operands)
                    AddUse(uses, operand, block.Id, index, instruction, "instruction.operand");
            }

            if (block.Terminator is null)
                continue;

            foreach (var operand in block.Terminator.Operands)
                AddUse(uses, operand, block.Id, int.MaxValue, null, "terminator.operand");

            foreach (var transfer in block.Terminator.Transfers)
            foreach (var argument in transfer.Arguments)
                AddUse(uses, argument, block.Id, int.MaxValue, null, "terminator.transfer.argument");
        }

        return new SsaUseDefMap(
            new ReadOnlyDictionary<SsaValueId, SsaValueDefinition>(definitions),
            new ReadOnlyDictionary<SsaValueId, IReadOnlyList<SsaValueUse>>(
                uses.ToDictionary(
                    static x => x.Key,
                    static x => (IReadOnlyList<SsaValueUse>)x.Value.ToArray())));
    }

    private static void AddUse(
        IDictionary<SsaValueId, List<SsaValueUse>> uses,
        SsaValueId valueId,
        SsaBlockId blockId,
        int instructionIndex,
        ISsaInstruction? instruction,
        string useKind)
    {
        if (!uses.TryGetValue(valueId, out var valueUses))
        {
            valueUses = [];
            uses[valueId] = valueUses;
        }

        valueUses.Add(new SsaValueUse(valueId, blockId, instructionIndex, instruction, useKind));
    }
}
