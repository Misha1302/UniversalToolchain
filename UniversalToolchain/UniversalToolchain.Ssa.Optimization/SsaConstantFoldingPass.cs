using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

public sealed class SsaConstantFoldingPass : IIrOptimizationPass
{
    private readonly SemanticDescriptorSet _descriptors;
    private readonly IConstantEvaluator _constantEvaluator;

    public SsaConstantFoldingPass()
        : this(
            SemanticDescriptorSet.Empty,
            new SsaInt32ConstantEvaluator())
    {
    }

    public SsaConstantFoldingPass(
        SemanticDescriptorSet descriptors,
        IConstantEvaluator constantEvaluator)
    {
        _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
        _constantEvaluator = constantEvaluator ?? throw new ArgumentNullException(nameof(constantEvaluator));
    }

    public IrStageId Id { get; } = new("ssa.optimization.constant-folding.local");

    public IrKind InputKind => SsaIrKinds.Ssa;

    public IrKind OutputKind => SsaIrKinds.Ssa;

    public IrStageContract Contract { get; } = new(
        requiresFacts: [SsaFacts.StructuralVerification],
        producesFacts: [SsaOptimizationFacts.LocallyConstantFolded],
        preservesFacts: [SsaFacts.StructuralVerification]);

    public IrStageResult Run(IIrArtifact input, IrPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        var artifact = input.As<SsaArtifact>();
        var module = RewriteModule(artifact.Module);
        return new IrStageResult(new SsaArtifact(module, artifact.ManagedCallableBindings));
    }

    private SsaModule RewriteModule(SsaModule module) =>
        new(module.Id, module.Functions.Select(RewriteFunction));

    private SsaFunction RewriteFunction(SsaFunction function) =>
        new(
            function.Id,
            function.EntryBlockId,
            function.Blocks.Select(RewriteBlock),
            function.Parameters,
            function.ReturnType);

    private SsaBlock RewriteBlock(SsaBlock block)
    {
        var knownConstants = new Dictionary<SsaValueId, ConstantValue>();
        var instructions = new List<ISsaInstruction>(block.Instructions.Count);

        foreach (var instruction in block.Instructions)
        {
            var rewritten = TryFold(instruction, knownConstants) ?? instruction;

            instructions.Add(rewritten);
            RecordResultConstants(rewritten, knownConstants);
        }

        return new SsaBlock(block.Id, block.Parameters, terminator: block.Terminator, instructions: instructions);
    }

    private ISsaInstruction? TryFold(
        ISsaInstruction instruction,
        IReadOnlyDictionary<SsaValueId, ConstantValue> constants)
    {
        if (instruction.Results.Count != 1)
            return null;

        if (!TryGetDescriptor(instruction, out var descriptor) ||
            !CanEvaluateSafely(descriptor) ||
            !TryReadArguments(instruction, constants, out var arguments) ||
            !_constantEvaluator.TryEvaluate(descriptor, arguments, out var result))
        {
            return null;
        }

        return SsaConstantMaterializer.TryCreate(instruction, result);
    }

    private bool TryGetDescriptor(ISsaInstruction instruction, out CallableDescriptor descriptor)
    {
        descriptor = default!;

        return instruction is SsaCall call &&
               _descriptors.TryGetCallable(call.Callee, out descriptor);
    }

    private static bool CanEvaluateSafely(CallableDescriptor descriptor) =>
        descriptor.Effects.IsPure &&
        descriptor.Determinism == Determinism.Deterministic &&
        descriptor.TrustLevel is SemanticTrustLevel.BuiltInTrusted or SemanticTrustLevel.VerifiedPlugin;

    private static bool TryReadArguments(
        ISsaInstruction instruction,
        IReadOnlyDictionary<SsaValueId, ConstantValue> constants,
        out IReadOnlyList<ConstantValue> arguments)
    {
        var values = new List<ConstantValue>(instruction.Operands.Count);
        foreach (var operand in instruction.Operands)
        {
            if (!constants.TryGetValue(operand, out var constant))
            {
                arguments = [];
                return false;
            }

            values.Add(constant);
        }

        arguments = values;
        return true;
    }

    private static void RecordResultConstants(ISsaInstruction instruction, IDictionary<SsaValueId, ConstantValue> constants)
    {
        foreach (var result in instruction.Results)
            constants.Remove(result.Id);

        if (instruction.Results.Count == 1 &&
            SsaConstantReader.TryRead(instruction, out var constant))
        {
            constants[instruction.Results[0].Id] = constant;
        }
    }
}
