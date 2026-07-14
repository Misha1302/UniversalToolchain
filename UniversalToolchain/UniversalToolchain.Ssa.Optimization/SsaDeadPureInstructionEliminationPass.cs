using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

/// <summary>
/// Removes dead trusted pure instructions and then removes non-entry block
/// parameters whose incoming values are no longer observed.
/// </summary>
public sealed class SsaDeadPureInstructionEliminationPass : IIrOptimizationPass
{
    private readonly SsaDescriptorSet _descriptors;
    private readonly SemanticDescriptorSet _semanticDescriptors;

    public SsaDeadPureInstructionEliminationPass()
        : this(SsaCoreDescriptors.CoreOperations, SemanticDescriptorSet.Empty)
    {
    }

    public SsaDeadPureInstructionEliminationPass(
        SsaDescriptorSet descriptors,
        SemanticDescriptorSet? semanticDescriptors = null)
    {
        _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
        _semanticDescriptors = semanticDescriptors ?? SemanticDescriptorSet.Empty;
    }

    public IrStageId Id { get; } = new("ssa.optimization.dce.dead-pure-instructions");

    public IrKind InputKind => SsaIrKinds.Ssa;

    public IrKind OutputKind => SsaIrKinds.Ssa;

    public IrStageContract Contract { get; } = new(
        requiresFacts: [SsaFacts.StructuralVerification],
        producesFacts: [SsaOptimizationFacts.DeadPureInstructionsEliminated],
        preservesFacts: [SsaFacts.StructuralVerification]);

    public IrStageResult Run(IIrArtifact input, IrPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        var artifact = input.As<SsaArtifact>();
        return new IrStageResult(
            new SsaArtifact(RewriteModule(artifact.Module), artifact.ManagedCallableBindings));
    }

    private SsaModule RewriteModule(SsaModule module) =>
        new(module.Id, module.Functions.Select(RewriteFunction));

    private SsaFunction RewriteFunction(SsaFunction function)
    {
        var current = function;
        while (true)
        {
            var useDef = SsaUseDefMap.Build(current);
            var liveInstructions = ComputeLiveInstructions(current, useDef);
            var instructionsChanged = current.Blocks.Any(block =>
                block.Instructions.Any(instruction => !liveInstructions.Contains(instruction.Id)));
            var rewritten = new SsaFunction(
                current.Id,
                current.EntryBlockId,
                current.Blocks.Select(block => RewriteBlock(block, liveInstructions)),
                current.Parameters,
                current.ReturnType);
            var (pruned, parametersChanged) = RemoveUnusedBlockParameters(rewritten);

            if (!instructionsChanged && !parametersChanged)
                return pruned;

            current = pruned;
        }
    }

    private static SsaBlock RewriteBlock(
        SsaBlock block,
        IReadOnlySet<SsaOperationId> liveInstructions)
    {
        var instructions = block.Instructions
            .Where(instruction => liveInstructions.Contains(instruction.Id))
            .ToArray();

        return new SsaBlock(
            block.Id,
            block.Parameters,
            terminator: block.Terminator,
            instructions: instructions);
    }

    private static (SsaFunction Function, bool Changed) RemoveUnusedBlockParameters(
        SsaFunction function)
    {
        var nonRewritableTargets = function.Blocks
            .Select(static block => block.Terminator)
            .Where(static terminator =>
                terminator is not null &&
                terminator.Kind is not (SsaTerminatorKind.Jump or SsaTerminatorKind.Branch))
            .SelectMany(static terminator => terminator!.Transfers)
            .Select(static transfer => transfer.Target)
            .ToHashSet();
        var removals = function.Blocks
            .Where(block =>
                block.Id != function.EntryBlockId &&
                block.Parameters.Count != 0 &&
                !nonRewritableTargets.Contains(block.Id))
            .Select(block => new
            {
                block.Id,
                Indices = FindUnusedParameterIndices(block)
            })
            .Where(static item => item.Indices.Count != 0)
            .ToDictionary(static item => item.Id, static item => item.Indices);

        if (removals.Count == 0)
            return (function, false);

        return (
            new SsaFunction(
                function.Id,
                function.EntryBlockId,
                function.Blocks.Select(block => RewriteBlockParametersAndTransfers(block, removals)),
                function.Parameters,
                function.ReturnType),
            true);
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

    private static SsaBlock RewriteBlockParametersAndTransfers(
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

    private HashSet<SsaOperationId> ComputeLiveInstructions(
        SsaFunction function,
        SsaUseDefMap useDef)
    {
        var liveValues = new HashSet<SsaValueId>();
        var liveInstructions = new HashSet<SsaOperationId>();
        var worklist = new Queue<SsaValueId>();

        void MarkValue(SsaValueId valueId)
        {
            if (liveValues.Add(valueId))
                worklist.Enqueue(valueId);
        }

        foreach (var block in function.Blocks)
        {
            if (block.Terminator is not null)
            {
                foreach (var operand in block.Terminator.Operands)
                    MarkValue(operand);

                foreach (var transfer in block.Terminator.Transfers)
                foreach (var argument in transfer.Arguments)
                    MarkValue(argument);
            }

            foreach (var instruction in block.Instructions)
            {
                if (!HasObservableEffects(instruction))
                    continue;

                liveInstructions.Add(instruction.Id);
                foreach (var operand in instruction.Operands)
                    MarkValue(operand);
            }
        }

        while (worklist.Count != 0)
        {
            var valueId = worklist.Dequeue();
            if (!useDef.Definitions.TryGetValue(valueId, out var definition) ||
                definition.Instruction is null ||
                !liveInstructions.Add(definition.Instruction.Id))
            {
                continue;
            }

            foreach (var operand in definition.Instruction.Operands)
                MarkValue(operand);
        }

        return liveInstructions;
    }

    private bool HasObservableEffects(ISsaInstruction instruction) =>
        instruction switch
        {
            SsaOperation operation => HasObservableEffects(operation),
            SsaCall call => HasObservableEffects(call),
            _ => true
        };

    private bool HasObservableEffects(SsaOperation operation)
    {
        if (!_descriptors.TryGet(operation.OpId, out var descriptor))
            return true;

        return !descriptor.Effects.IsPure;
    }

    private bool HasObservableEffects(SsaCall call)
    {
        if (!_semanticDescriptors.TryGetCallable(call.Callee, out var descriptor))
            return true;

        return !descriptor.Effects.IsPure ||
               descriptor.Effects.Contains(SemanticEffectKind.MayThrow) ||
               descriptor.TrustLevel is not (SemanticTrustLevel.BuiltInTrusted or SemanticTrustLevel.VerifiedPlugin);
    }
}
