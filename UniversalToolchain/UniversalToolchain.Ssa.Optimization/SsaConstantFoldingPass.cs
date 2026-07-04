using System.Globalization;
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
            new SsaPreviewInt32ConstantEvaluator())
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
        return new IrStageResult(new SsaArtifact(module));
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

        return CreateConstantInstruction(instruction, result);
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

    private static SsaOperation? CreateConstantInstruction(ISsaInstruction source, ConstantValue value)
    {
        if (source.Results.Count != 1 ||
            !SameType(source.Results[0].Type, value.Type))
        {
            return null;
        }

        if (source.Results[0].Type == SsaTypes.Int32 &&
            int.TryParse(value.CanonicalValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return ConstantInt32(source, intValue);
        }

        if (source.Results[0].Type == SsaTypes.Bool &&
            bool.TryParse(value.CanonicalValue, out var boolValue))
        {
            return ConstantBool(source, boolValue);
        }

        return null;
    }

    private static SsaOperation ConstantInt32(ISsaInstruction source, int value) =>
        new(
            source.Id,
            SsaOperations.ConstantInt32,
            results: source.Results,
            attributes: new SsaAttributeBag([new SsaAttribute(SsaAttributeKeys.ConstantValue, value.ToString(CultureInfo.InvariantCulture))]));

    private static SsaOperation ConstantBool(ISsaInstruction source, bool value) =>
        new(
            source.Id,
            SsaOperations.ConstantBool,
            results: source.Results,
            attributes: new SsaAttributeBag([new SsaAttribute(SsaAttributeKeys.ConstantValue, value.ToString())]));

    private static void RecordResultConstants(ISsaInstruction instruction, IDictionary<SsaValueId, ConstantValue> constants)
    {
        foreach (var result in instruction.Results)
            constants.Remove(result.Id);

        if (instruction is not SsaOperation operation ||
            operation.Results.Count != 1 ||
            !operation.Attributes.TryGet(SsaAttributeKeys.ConstantValue, out var attribute))
        {
            return;
        }

        var resultValue = operation.Results[0];
        if (operation.OpId == SsaOperations.ConstantInt32 &&
            resultValue.Type == SsaTypes.Int32 &&
            int.TryParse(attribute.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            constants[resultValue.Id] = new ConstantValue(SsaPreviewSemanticTypes.Int32, attribute.Value);
            return;
        }

        if (operation.OpId == SsaOperations.ConstantBool &&
            resultValue.Type == SsaTypes.Bool &&
            bool.TryParse(attribute.Value, out _))
        {
            constants[resultValue.Id] = new ConstantValue(SsaPreviewSemanticTypes.Bool, attribute.Value);
        }
    }

    private static bool SameType(SsaTypeId ssaType, SemanticTypeId semanticType) =>
        string.Equals(ssaType.Value, semanticType.Value, StringComparison.Ordinal);
}
