using System.Globalization;
using System.Text;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

/// <summary>
/// Eliminates repeated trusted deterministic pure expressions within one SSA block.
///
/// Expression availability never crosses a block boundary. Result substitutions are
/// still applied function-wide so direct dominated uses of an eliminated result remain
/// structurally valid. Unknown instruction or terminator shapes disable the pass for the
/// containing function rather than risking an incomplete rewrite.
/// </summary>
public sealed class SsaLocalCommonSubexpressionEliminationPass : IIrOptimizationPass
{
    private readonly SemanticDescriptorSet _semanticDescriptors;

    public SsaLocalCommonSubexpressionEliminationPass()
        : this(SemanticDescriptorSet.Empty)
    {
    }

    public SsaLocalCommonSubexpressionEliminationPass(
        SemanticDescriptorSet semanticDescriptors)
    {
        _semanticDescriptors = semanticDescriptors
            ?? throw new ArgumentNullException(nameof(semanticDescriptors));
    }

    public IrStageId Id { get; } = new("ssa.optimization.cse.local");

    public IrKind InputKind => SsaIrKinds.Ssa;

    public IrKind OutputKind => SsaIrKinds.Ssa;

    public IrStageContract Contract { get; } = new(
        requiresFacts: [SsaFacts.StructuralVerification],
        producesFacts: [SsaOptimizationFacts.LocalCommonSubexpressionsEliminated],
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
            new SsaArtifact(module, artifact.ManagedCallableBindings));
    }

    private SsaFunction RewriteFunction(SsaFunction function)
    {
        if (!CanRewriteFunction(function))
            return function;

        var substitutions = new Dictionary<SsaValueId, SsaValueId>();
        var blocks = function.Blocks
            .Select(block => EliminateLocalDuplicates(block, substitutions))
            .ToArray();
        blocks = blocks
            .Select(block => RewriteBlockUses(block, substitutions))
            .ToArray();

        return new SsaFunction(
            function.Id,
            function.EntryBlockId,
            blocks,
            function.Parameters,
            function.ReturnType);
    }

    private static bool CanRewriteFunction(SsaFunction function) =>
        function.Blocks.All(block =>
            block.Instructions.All(static instruction =>
                instruction is SsaOperation or SsaCall) &&
            IsKnownTerminator(block.Terminator));

    private static bool IsKnownTerminator(SsaTerminator? terminator) =>
        terminator is null || terminator.Kind is
            SsaTerminatorKind.Return or
            SsaTerminatorKind.Jump or
            SsaTerminatorKind.Branch or
            SsaTerminatorKind.Unreachable;

    private SsaBlock EliminateLocalDuplicates(
        SsaBlock block,
        Dictionary<SsaValueId, SsaValueId> substitutions)
    {
        var available = new Dictionary<ExpressionKey, SsaValueId>();
        var instructions = new List<ISsaInstruction>(block.Instructions.Count);

        foreach (var instruction in block.Instructions)
        {
            var rewritten = RewriteInstructionUses(instruction, substitutions);
            if (!TryCreateExpressionKey(rewritten, out var key))
            {
                instructions.Add(rewritten);
                continue;
            }

            var result = rewritten.Results.Single().Id;
            if (!available.TryGetValue(key, out var existing))
            {
                available.Add(key, result);
                instructions.Add(rewritten);
                continue;
            }

            var canonical = Resolve(existing, substitutions);
            substitutions[result] = canonical;
        }

        return new SsaBlock(
            block.Id,
            block.Parameters,
            instructions: instructions,
            terminator: RewriteTerminatorUses(block.Terminator, substitutions));
    }

    private bool TryCreateExpressionKey(
        ISsaInstruction instruction,
        out ExpressionKey key)
    {
        key = default;
        if (instruction.Results.Count != 1)
            return false;

        if (instruction is SsaOperation operation)
            return TryCreateConstantKey(operation, out key);

        if (instruction is not SsaCall call ||
            !_semanticDescriptors.TryGetCallable(call.Callee, out var descriptor) ||
            !CanEliminateSafely(descriptor))
        {
            return false;
        }

        var operands = call.Operands.ToArray();
        if (descriptor.HasTrait(AlgebraicTraits.Commutative) && operands.Length == 2 &&
            operands[1].CompareTo(operands[0]) < 0)
        {
            (operands[0], operands[1]) = (operands[1], operands[0]);
        }

        key = new ExpressionKey(
            "call",
            call.Callee.Value,
            FingerprintOperands(operands),
            call.Results[0].Type.Value,
            FingerprintAttributes(call.Attributes));
        return true;
    }

    private static bool TryCreateConstantKey(
        SsaOperation operation,
        out ExpressionKey key)
    {
        key = default;
        if (!SsaConstantReader.TryRead(operation, out var constant))
            return false;

        key = new ExpressionKey(
            "constant",
            operation.OpId.Value,
            FingerprintPart(constant.CanonicalValue),
            operation.Results[0].Type.Value,
            FingerprintAttributes(operation.Attributes));
        return true;
    }

    private static bool CanEliminateSafely(CallableDescriptor descriptor) =>
        descriptor.Effects.IsPure &&
        descriptor.Determinism == Determinism.Deterministic &&
        descriptor.TrustLevel is
            SemanticTrustLevel.BuiltInTrusted or
            SemanticTrustLevel.VerifiedPlugin;

    private static SsaBlock RewriteBlockUses(
        SsaBlock block,
        IReadOnlyDictionary<SsaValueId, SsaValueId> substitutions) =>
        new(
            block.Id,
            block.Parameters,
            instructions: block.Instructions.Select(instruction =>
                RewriteInstructionUses(instruction, substitutions)),
            terminator: RewriteTerminatorUses(block.Terminator, substitutions));

    private static ISsaInstruction RewriteInstructionUses(
        ISsaInstruction instruction,
        IReadOnlyDictionary<SsaValueId, SsaValueId> substitutions)
    {
        var operands = instruction.Operands
            .Select(operand => Resolve(operand, substitutions))
            .ToArray();

        return instruction switch
        {
            SsaOperation operation => new SsaOperation(
                operation.Id,
                operation.OpId,
                operands,
                operation.Results,
                operation.Attributes),
            SsaCall call => new SsaCall(
                call.Id,
                call.Callee,
                operands,
                call.Results,
                call.Attributes),
            _ => instruction
        };
    }

    private static SsaTerminator? RewriteTerminatorUses(
        SsaTerminator? terminator,
        IReadOnlyDictionary<SsaValueId, SsaValueId> substitutions)
    {
        if (terminator is null)
            return null;

        return terminator.Kind switch
        {
            SsaTerminatorKind.Return => SsaTerminator.Return(
                terminator.Operands.Select(operand => Resolve(operand, substitutions))),
            SsaTerminatorKind.Jump => RewriteJump(terminator, substitutions),
            SsaTerminatorKind.Branch => RewriteBranch(terminator, substitutions),
            SsaTerminatorKind.Unreachable => SsaTerminator.Unreachable(),
            _ => terminator
        };
    }

    private static SsaTerminator RewriteJump(
        SsaTerminator terminator,
        IReadOnlyDictionary<SsaValueId, SsaValueId> substitutions)
    {
        var transfer = terminator.Transfers.Single();
        return SsaTerminator.Jump(
            transfer.Target,
            transfer.Arguments.Select(argument => Resolve(argument, substitutions)));
    }

    private static SsaTerminator RewriteBranch(
        SsaTerminator terminator,
        IReadOnlyDictionary<SsaValueId, SsaValueId> substitutions)
    {
        var first = terminator.Transfers[0];
        var second = terminator.Transfers[1];
        return SsaTerminator.Branch(
            Resolve(terminator.Operands.Single(), substitutions),
            first.Target,
            first.Arguments.Select(argument => Resolve(argument, substitutions)),
            second.Target,
            second.Arguments.Select(argument => Resolve(argument, substitutions)));
    }

    private static SsaValueId Resolve(
        SsaValueId value,
        IReadOnlyDictionary<SsaValueId, SsaValueId> substitutions)
    {
        var current = value;
        while (substitutions.TryGetValue(current, out var replacement))
            current = replacement;
        return current;
    }

    private static string FingerprintOperands(IEnumerable<SsaValueId> operands)
    {
        var builder = new StringBuilder();
        foreach (var operand in operands)
            AppendPart(builder, operand.Value);
        return builder.ToString();
    }

    private static string FingerprintAttributes(SsaAttributeBag attributes)
    {
        var builder = new StringBuilder();
        foreach (var attribute in attributes.Values)
        {
            AppendPart(builder, attribute.Key.Value);
            AppendPart(builder, attribute.Value);
        }
        return builder.ToString();
    }

    private static string FingerprintPart(string value)
    {
        var builder = new StringBuilder();
        AppendPart(builder, value);
        return builder.ToString();
    }

    private static void AppendPart(StringBuilder builder, string value)
    {
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value);
        builder.Append(';');
    }

    private readonly record struct ExpressionKey(
        string Kind,
        string Operation,
        string Operands,
        string ResultType,
        string Attributes);
}
