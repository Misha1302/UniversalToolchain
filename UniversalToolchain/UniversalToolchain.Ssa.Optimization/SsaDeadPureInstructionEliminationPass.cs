using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

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
        return new IrStageResult(new SsaArtifact(RewriteModule(artifact.Module), artifact.ManagedCallableBindings));
    }

    private SsaModule RewriteModule(SsaModule module) =>
        new(module.Id, module.Functions.Select(RewriteFunction));

    private SsaFunction RewriteFunction(SsaFunction function)
    {
        var useDef = SsaUseDefMap.Build(function);
        var liveInstructions = ComputeLiveInstructions(function, useDef);

        return new SsaFunction(
            function.Id,
            function.EntryBlockId,
            function.Blocks.Select(block => RewriteBlock(block, liveInstructions)),
            function.Parameters,
            function.ReturnType);
    }

    private SsaBlock RewriteBlock(SsaBlock block, IReadOnlySet<SsaOperationId> liveInstructions)
    {
        var instructions = block.Instructions
            .Where(instruction => liveInstructions.Contains(instruction.Id))
            .ToArray();

        return new SsaBlock(block.Id, block.Parameters, terminator: block.Terminator, instructions: instructions);
    }

    private HashSet<SsaOperationId> ComputeLiveInstructions(SsaFunction function, SsaUseDefMap useDef)
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
