using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

public sealed class SsaBranchFoldingAndCleanupPass : IIrOptimizationPass
{
    public IrStageId Id { get; } = new("ssa.optimization.branch-folding-and-cleanup");

    public IrKind InputKind => SsaIrKinds.Ssa;

    public IrKind OutputKind => SsaIrKinds.Ssa;

    public IrStageContract Contract { get; } = new(
        requiresFacts: [SsaFacts.StructuralVerification],
        producesFacts:
        [
            SsaOptimizationFacts.BranchesFolded,
            SsaOptimizationFacts.UnreachableBlocksEliminated
        ],
        preservesFacts: [SsaFacts.StructuralVerification]);

    public IrStageResult Run(IIrArtifact input, IrPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        var artifact = input.As<SsaArtifact>();
        var module = new SsaModule(artifact.Module.Id, artifact.Module.Functions.Select(RewriteFunction));
        return new IrStageResult(new SsaArtifact(module));
    }

    private static SsaFunction RewriteFunction(SsaFunction function)
    {
        var foldedBlocks = function.Blocks
            .Select(RewriteBlockTerminator)
            .ToArray();

        var foldedFunction = new SsaFunction(
            function.Id,
            function.EntryBlockId,
            foldedBlocks,
            function.Parameters,
            function.ReturnType);

        return RemoveUnreachableBlocks(foldedFunction);
    }

    private static SsaBlock RewriteBlockTerminator(SsaBlock block)
    {
        if (block.Terminator is not { Kind: SsaTerminatorKind.Branch } terminator ||
            terminator.Operands.Count != 1 ||
            terminator.Transfers.Count != 2 ||
            !TryReadLocalBoolConstant(block, terminator.Operands[0], out var condition))
        {
            return block;
        }

        var selectedTransfer = condition ? terminator.Transfers[0] : terminator.Transfers[1];
        return new SsaBlock(
            block.Id,
            block.Parameters,
            terminator: SsaTerminator.Jump(selectedTransfer.Target, selectedTransfer.Arguments),
            instructions: block.Instructions);
    }

    private static bool TryReadLocalBoolConstant(SsaBlock block, SsaValueId valueId, out bool value)
    {
        value = default;
        for (var index = block.Instructions.Count - 1; index >= 0; index--)
        {
            var instruction = block.Instructions[index];
            if (instruction.Results.Count != 1 ||
                instruction.Results[0].Id != valueId)
            {
                continue;
            }

            if (!SsaConstantReader.TryRead(instruction, out var constant) ||
                constant.Type != SsaPreviewSemanticTypes.Bool)
            {
                return false;
            }

            return bool.TryParse(constant.CanonicalValue, out value);
        }

        return false;
    }

    private static SsaFunction RemoveUnreachableBlocks(SsaFunction function)
    {
        var blocks = function.Blocks.ToDictionary(static x => x.Id);
        if (!blocks.ContainsKey(function.EntryBlockId))
            return function;

        var reachable = ComputeReachable(function.EntryBlockId, blocks);
        if (reachable.Count == blocks.Count)
            return function;

        return new SsaFunction(
            function.Id,
            function.EntryBlockId,
            function.Blocks.Where(block => reachable.Contains(block.Id)),
            function.Parameters,
            function.ReturnType);
    }

    private static HashSet<SsaBlockId> ComputeReachable(
        SsaBlockId entryBlockId,
        IReadOnlyDictionary<SsaBlockId, SsaBlock> blocks)
    {
        var reachable = new HashSet<SsaBlockId>();
        var worklist = new Queue<SsaBlockId>();
        worklist.Enqueue(entryBlockId);

        while (worklist.Count != 0)
        {
            var current = worklist.Dequeue();
            if (!reachable.Add(current) ||
                !blocks.TryGetValue(current, out var block) ||
                block.Terminator is null)
            {
                continue;
            }

            foreach (var transfer in block.Terminator.Transfers)
            {
                if (blocks.ContainsKey(transfer.Target))
                    worklist.Enqueue(transfer.Target);
            }
        }

        return reachable;
    }
}
