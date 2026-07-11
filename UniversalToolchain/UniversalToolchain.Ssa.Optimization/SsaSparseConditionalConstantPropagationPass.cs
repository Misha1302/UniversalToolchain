using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

/// <summary>
/// Conservative sparse conditional constant propagation for preview SSA.
///
/// This pass intentionally keeps the first slice small:
/// - tracks executable blocks and constants at SSA value granularity;
/// - evaluates only trusted deterministic pure single-result calls;
/// - materializes only proven constants at the original defining instruction;
/// - folds branches only when the condition is a proven bool constant;
/// - removes blocks proven unreachable by the executable-block lattice.
///
/// It deliberately does not perform general value substitution, GVN, CSE, LICM,
/// or algebraic rewrites such as x + 0 -> x.
/// </summary>
public sealed class SsaSparseConditionalConstantPropagationPass : IIrOptimizationPass
{
    private readonly SemanticDescriptorSet _descriptors;
    private readonly IConstantEvaluator _constantEvaluator;

    public SsaSparseConditionalConstantPropagationPass()
        : this(
            SemanticDescriptorSet.Empty,
            new SsaPreviewInt32ConstantEvaluator())
    {
    }

    public SsaSparseConditionalConstantPropagationPass(
        SemanticDescriptorSet descriptors,
        IConstantEvaluator constantEvaluator)
    {
        _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
        _constantEvaluator = constantEvaluator ?? throw new ArgumentNullException(nameof(constantEvaluator));
    }

    public IrStageId Id { get; } = new("ssa.optimization.sccp-lite");

    public IrKind InputKind => SsaIrKinds.Ssa;

    public IrKind OutputKind => SsaIrKinds.Ssa;

    public IrStageContract Contract { get; } = new(
        requiresFacts: [SsaFacts.StructuralVerification],
        producesFacts:
        [
            SsaOptimizationFacts.SparseConditionalConstantPropagated,
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
        return new IrStageResult(new SsaArtifact(module, artifact.ManagedCallableBindings));
    }

    private SsaFunction RewriteFunction(SsaFunction function)
    {
        if (function.Blocks.Count == 0 ||
            !function.Blocks.Any(block => block.Id == function.EntryBlockId))
        {
            return function;
        }

        var state = Analyze(function);
        return RewriteFunction(function, state);
    }

    private SccpState Analyze(SsaFunction function)
    {
        var state = new SccpState(function);
        state.MarkReachable(function.EntryBlockId);

        foreach (var parameter in function.Parameters)
            state.SetValue(parameter.Value.Id, SccpValue.Overdefined);

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var block in function.Blocks)
            {
                if (!state.IsReachable(block.Id))
                    continue;

                foreach (var parameter in block.Parameters)
                {
                    if (block.Id == function.EntryBlockId)
                        changed |= state.SetValue(parameter.Value.Id, SccpValue.Overdefined);
                }

                foreach (var instruction in block.Instructions)
                    changed |= VisitInstruction(instruction, state);

                changed |= VisitTerminator(block, state);
            }
        }

        return state;
    }

    private bool VisitInstruction(ISsaInstruction instruction, SccpState state)
    {
        if (instruction.Results.Count == 0)
            return false;

        if (instruction.Results.Count == 1 &&
            SsaConstantReader.TryRead(instruction, out var constant))
        {
            return state.SetValue(instruction.Results[0].Id, SccpValue.ForConstant(constant));
        }

        var evaluation = EvaluateInstruction(instruction, state);
        if (evaluation.Kind == InstructionEvaluationKind.Wait)
            return false;

        if (evaluation.Kind == InstructionEvaluationKind.Constant)
            return state.SetValue(instruction.Results[0].Id, SccpValue.ForConstant(evaluation.Value!));

        var changed = false;
        foreach (var result in instruction.Results)
            changed |= state.SetValue(result.Id, SccpValue.Overdefined);
        return changed;
    }

    private InstructionEvaluation EvaluateInstruction(ISsaInstruction instruction, SccpState state)
    {
        if (instruction is not SsaCall call ||
            instruction.Results.Count != 1 ||
            !_descriptors.TryGetCallable(call.Callee, out var descriptor) ||
            !CanEvaluateSafely(descriptor))
        {
            return InstructionEvaluation.Overdefined;
        }

        var arguments = new List<ConstantValue>(instruction.Operands.Count);
        foreach (var operand in instruction.Operands)
        {
            var lattice = state.GetValue(operand);
            if (lattice.Kind == SccpValueKind.Unknown)
                return InstructionEvaluation.Wait;

            if (lattice.Kind == SccpValueKind.Overdefined || lattice.Value is null)
                return InstructionEvaluation.Overdefined;

            arguments.Add(lattice.Value);
        }

        return _constantEvaluator.TryEvaluate(descriptor, arguments, out var result)
            ? InstructionEvaluation.ForConstant(result)
            : InstructionEvaluation.Overdefined;
    }

    private bool VisitTerminator(SsaBlock block, SccpState state)
    {
        if (block.Terminator is null)
            return false;

        if (block.Terminator.Kind == SsaTerminatorKind.Jump && block.Terminator.Transfers.Count == 1)
            return VisitTransfer(block.Terminator.Transfers[0], state);

        if (block.Terminator.Kind != SsaTerminatorKind.Branch ||
            block.Terminator.Operands.Count != 1 ||
            block.Terminator.Transfers.Count != 2)
        {
            return false;
        }

        var condition = state.GetValue(block.Terminator.Operands[0]);
        if (TryReadBool(condition, out var boolValue))
        {
            var selected = boolValue ? block.Terminator.Transfers[0] : block.Terminator.Transfers[1];
            return VisitTransfer(selected, state);
        }

        if (condition.Kind != SccpValueKind.Overdefined)
            return false;

        return VisitTransfer(block.Terminator.Transfers[0], state) |
               VisitTransfer(block.Terminator.Transfers[1], state);
    }

    private bool VisitTransfer(SsaBlockTransfer transfer, SccpState state)
    {
        var changed = state.MarkReachable(transfer.Target);
        if (!state.Blocks.TryGetValue(transfer.Target, out var target))
            return changed;

        var count = Math.Min(target.Parameters.Count, transfer.Arguments.Count);
        for (var index = 0; index < count; index++)
        {
            var argumentValue = state.GetValue(transfer.Arguments[index]);
            changed |= state.JoinValue(target.Parameters[index].Value.Id, argumentValue);
        }

        return changed;
    }

    private SsaFunction RewriteFunction(SsaFunction function, SccpState state)
    {
        var blocks = function.Blocks
            .Where(block => state.IsReachable(block.Id))
            .Select(block => RewriteBlock(block, state))
            .ToArray();

        return new SsaFunction(
            function.Id,
            function.EntryBlockId,
            blocks,
            function.Parameters,
            function.ReturnType);
    }

    private SsaBlock RewriteBlock(SsaBlock block, SccpState state) =>
        new(
            block.Id,
            block.Parameters,
            instructions: block.Instructions.Select(instruction => RewriteInstruction(instruction, state)),
            terminator: RewriteTerminator(block.Terminator, state));

    private static ISsaInstruction RewriteInstruction(ISsaInstruction instruction, SccpState state)
    {
        if (instruction.Results.Count != 1)
            return instruction;

        var value = state.GetValue(instruction.Results[0].Id);
        if (value.Kind != SccpValueKind.Constant || value.Value is null)
            return instruction;

        return SsaConstantMaterializer.TryCreate(instruction, value.Value) ?? instruction;
    }

    private static SsaTerminator? RewriteTerminator(SsaTerminator? terminator, SccpState state)
    {
        if (terminator is not { Kind: SsaTerminatorKind.Branch } ||
            terminator.Operands.Count != 1 ||
            terminator.Transfers.Count != 2 ||
            !TryReadBool(state.GetValue(terminator.Operands[0]), out var condition))
        {
            return terminator;
        }

        var selected = condition ? terminator.Transfers[0] : terminator.Transfers[1];
        return SsaTerminator.Jump(selected.Target, selected.Arguments);
    }

    private static bool CanEvaluateSafely(CallableDescriptor descriptor) =>
        descriptor.Effects.IsPure &&
        descriptor.Determinism == Determinism.Deterministic &&
        descriptor.TrustLevel is SemanticTrustLevel.BuiltInTrusted or SemanticTrustLevel.VerifiedPlugin;

    private static bool TryReadBool(SccpValue value, out bool result)
    {
        result = default;
        return value.Kind == SccpValueKind.Constant &&
               value.Value is not null &&
               value.Value.Type == SsaPreviewSemanticTypes.Bool &&
               bool.TryParse(value.Value.CanonicalValue, out result);
    }

    private sealed class SccpState
    {
        private readonly HashSet<SsaBlockId> _reachableBlocks = [];
        private readonly Dictionary<SsaValueId, SccpValue> _values = [];

        public SccpState(SsaFunction function)
        {
            Blocks = function.Blocks.ToDictionary(static x => x.Id);
        }

        public IReadOnlyDictionary<SsaBlockId, SsaBlock> Blocks { get; }

        public bool IsReachable(SsaBlockId blockId) => _reachableBlocks.Contains(blockId);

        public bool MarkReachable(SsaBlockId blockId) =>
            Blocks.ContainsKey(blockId) && _reachableBlocks.Add(blockId);

        public SccpValue GetValue(SsaValueId valueId) =>
            _values.TryGetValue(valueId, out var value) ? value : SccpValue.Unknown;

        public bool SetValue(SsaValueId valueId, SccpValue value)
        {
            var current = GetValue(valueId);
            var joined = SccpValue.Join(current, value);
            if (joined.Equals(current))
                return false;

            _values[valueId] = joined;
            return true;
        }

        public bool JoinValue(SsaValueId valueId, SccpValue value) => SetValue(valueId, value);
    }

    private enum SccpValueKind
    {
        Unknown,
        Constant,
        Overdefined
    }

    private readonly record struct SccpValue(SccpValueKind Kind, ConstantValue? Value)
    {
        public static SccpValue Unknown { get; } = new(SccpValueKind.Unknown, null);

        public static SccpValue Overdefined { get; } = new(SccpValueKind.Overdefined, null);

        public static SccpValue ForConstant(ConstantValue value) =>
            new(SccpValueKind.Constant, value ?? throw new ArgumentNullException(nameof(value)));

        public static SccpValue Join(SccpValue current, SccpValue incoming)
        {
            if (incoming.Kind == SccpValueKind.Unknown)
                return current;

            if (current.Kind == SccpValueKind.Unknown)
                return incoming;

            if (current.Kind == SccpValueKind.Overdefined || incoming.Kind == SccpValueKind.Overdefined)
                return Overdefined;

            return Equals(current.Value, incoming.Value) ? current : Overdefined;
        }
    }

    private enum InstructionEvaluationKind
    {
        Wait,
        Constant,
        Overdefined
    }

    private readonly record struct InstructionEvaluation(InstructionEvaluationKind Kind, ConstantValue? Value)
    {
        public static InstructionEvaluation Wait { get; } = new(InstructionEvaluationKind.Wait, null);

        public static InstructionEvaluation Overdefined { get; } = new(InstructionEvaluationKind.Overdefined, null);

        public static InstructionEvaluation ForConstant(ConstantValue value) =>
            new(InstructionEvaluationKind.Constant, value ?? throw new ArgumentNullException(nameof(value)));
    }
}
