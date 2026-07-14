using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

/// <summary>
/// Removes unused non-entry block parameters and the corresponding arguments
/// from every incoming control-flow transfer.
/// </summary>
public sealed class SsaDeadBlockArgumentEliminationPass : IIrOptimizationPass
{
    public IrStageId Id { get; } = new("ssa.optimization.dead-block-argument-elimination");

    public IrKind InputKind => SsaIrKinds.Ssa;

    public IrKind OutputKind => SsaIrKinds.Ssa;

    public IrStageContract Contract { get; } = new(
        requiresFacts: [SsaFacts.StructuralVerification],
        preservesFacts: [SsaFacts.StructuralVerification]);

    public IrStageResult Run(IIrArtifact input, IrPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        var artifact = input.As<SsaArtifact>();
        var module = new SsaModule(
            artifact.Module.Id,
            artifact.Module.Functions.Select(RewriteFunction));
        return new IrStageResult(
            new SsaArtifact(module, artifact.ManagedCallableBindings),
            context.Facts);
    }

    private static SsaFunction RewriteFunction(SsaFunction function)
    {
        var blocks = function.Blocks.ToArray();
        while (true)
        {
            var removals = blocks
                .Where(block => block.Id != function.EntryBlockId && block.Parameters.Count != 0)
                .Select(block => new
                {
                    block.Id,
                    Indices = FindUnusedParameterIndices(block)
                })
                .Where(static item => item.Indices.Count != 0)
                .ToDictionary(static item => item.Id, static item => item.Indices);

            if (removals.Count == 0)
            {
                return new SsaFunction(
                    function.Id,
                    function.EntryBlockId,
                    blocks,
                    function.Parameters,
                    function.ReturnType);
            }

            blocks = blocks
                .Select(block => RewriteBlock(block, removals))
                .ToArray();
        }
    }

    private static IReadOnlySet<int> FindUnusedParameterIndices(SsaBlock block)
    {
        var used = new HashSet<SsaValueId>(
            block.Instructions.SelectMany(static instruction => instruction.Operands));
        if (block.Terminator is not null)
        {
            used.UnionWith(block.Terminator.Operands);
            used.UnionWith(block.Terminator.Transfers.SelectMany(static transfer => transfer.Arguments));
        }

        return block.Parameters
            .Select(static (parameter, index) => (parameter, index))
            .Where(item => !used.Contains(item.parameter.Value.Id))
            .Select(static item => item.index)
            .ToHashSet();
    }

    private static SsaBlock RewriteBlock(
        SsaBlock block,
        IReadOnlyDictionary<SsaBlockId, IReadOnlySet<int>> removals)
    {
        var parameters = removals.TryGetValue(block.Id, out var removedParameters)
            ? block.Parameters.Where((_, index) => !removedParameters.Contains(index)).ToArray()
            : block.Parameters;

        return new SsaBlock(
            block.Id,
            parameters,
            instructions: block.Instructions,
            terminator: RewriteTerminator(block.Terminator, removals));
    }

    private static SsaTerminator? RewriteTerminator(
        SsaTerminator? terminator,
        IReadOnlyDictionary<SsaBlockId, IReadOnlySet<int>> removals)
    {
        if (terminator is null)
            return null;

        return terminator.Kind switch
        {
            SsaTerminatorKind.Return => SsaTerminator.Return(terminator.Operands),
            SsaTerminatorKind.Jump => RewriteJump(terminator.Transfers.Single(), removals),
            SsaTerminatorKind.Branch => RewriteBranch(terminator, removals),
            SsaTerminatorKind.Unreachable => SsaTerminator.Unreachable(),
            _ => terminator
        };
    }

    private static SsaTerminator RewriteJump(
        SsaBlockTransfer transfer,
        IReadOnlyDictionary<SsaBlockId, IReadOnlySet<int>> removals) =>
        SsaTerminator.Jump(transfer.Target, FilterArguments(transfer, removals));

    private static SsaTerminator RewriteBranch(
        SsaTerminator terminator,
        IReadOnlyDictionary<SsaBlockId, IReadOnlySet<int>> removals)
    {
        var first = terminator.Transfers[0];
        var second = terminator.Transfers[1];
        return SsaTerminator.Branch(
            terminator.Operands.Single(),
            first.Target,
            FilterArguments(first, removals),
            second.Target,
            FilterArguments(second, removals));
    }

    private static IReadOnlyList<SsaValueId> FilterArguments(
        SsaBlockTransfer transfer,
        IReadOnlyDictionary<SsaBlockId, IReadOnlySet<int>> removals)
    {
        if (!removals.TryGetValue(transfer.Target, out var removedIndices))
            return transfer.Arguments;

        return transfer.Arguments
            .Where((_, index) => !removedIndices.Contains(index))
            .ToArray();
    }
}
