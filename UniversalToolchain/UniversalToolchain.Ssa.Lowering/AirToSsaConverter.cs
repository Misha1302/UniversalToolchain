using IntermediateRepresentationAbstractions;
using System.Globalization;
using System.Reflection;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;

namespace UniversalToolchain.Ssa.Lowering;

public sealed class AirToSsaConverter : IIrConverter
{
    private readonly AirControlFlowGraphBuilder _cfgBuilder;
    private readonly AirStackAnalyzer _stackAnalyzer;
    private readonly StructuralAirVerifier _airVerifier;
    private readonly StructuralSsaVerifier _ssaVerifier;
    private readonly AirIntrinsicDescriptorSet _intrinsicDescriptors;
    private readonly IReadOnlyDictionary<string, CallableId> _intrinsicCallables;
    private readonly SemanticDescriptorSet _semanticDescriptors;
    private readonly bool _allowManagedCallables;
    private readonly IReadOnlyList<ISsaManagedCallableProjection> _managedCallableProjections;

    public AirToSsaConverter()
        : this(
            new AirControlFlowGraphBuilder(),
            new AirStackAnalyzer(AirIntrinsicDescriptorSet.Empty),
            new StructuralAirVerifier(
                new AirControlFlowGraphBuilder(),
                new AirStackAnalyzer(AirIntrinsicDescriptorSet.Empty)),
            new StructuralSsaVerifier(SsaCoreDescriptors.CoreOperations),
            AirIntrinsicDescriptorSet.Empty,
            new Dictionary<string, CallableId>(StringComparer.Ordinal),
            SemanticDescriptorSet.Empty,
            allowManagedCallables: false)
    {
    }

    public AirToSsaConverter(
        AirControlFlowGraphBuilder cfgBuilder,
        AirStackAnalyzer stackAnalyzer,
        StructuralAirVerifier airVerifier,
        StructuralSsaVerifier ssaVerifier)
        : this(
            cfgBuilder,
            stackAnalyzer,
            airVerifier,
            ssaVerifier,
            AirIntrinsicDescriptorSet.Empty,
            new Dictionary<string, CallableId>(StringComparer.Ordinal),
            SemanticDescriptorSet.Empty,
            allowManagedCallables: false)
    {
    }

    public AirToSsaConverter(
        AirControlFlowGraphBuilder cfgBuilder,
        AirStackAnalyzer stackAnalyzer,
        StructuralAirVerifier airVerifier,
        StructuralSsaVerifier ssaVerifier,
        AirIntrinsicDescriptorSet intrinsicDescriptors,
        IReadOnlyDictionary<string, CallableId> intrinsicCallables)
        : this(
            cfgBuilder,
            stackAnalyzer,
            airVerifier,
            ssaVerifier,
            intrinsicDescriptors,
            intrinsicCallables,
            SemanticDescriptorSet.Empty,
            allowManagedCallables: false)
    {
    }

    public AirToSsaConverter(
        AirControlFlowGraphBuilder cfgBuilder,
        AirStackAnalyzer stackAnalyzer,
        StructuralAirVerifier airVerifier,
        StructuralSsaVerifier ssaVerifier,
        AirIntrinsicDescriptorSet intrinsicDescriptors,
        IReadOnlyDictionary<string, CallableId> intrinsicCallables,
        SemanticDescriptorSet semanticDescriptors,
        bool allowManagedCallables,
        IEnumerable<ISsaManagedCallableProjection>? managedCallableProjections = null)
    {
        _cfgBuilder = cfgBuilder;
        _stackAnalyzer = stackAnalyzer;
        _airVerifier = airVerifier;
        _ssaVerifier = ssaVerifier;
        _intrinsicDescriptors = intrinsicDescriptors ?? throw new ArgumentNullException(nameof(intrinsicDescriptors));
        _intrinsicCallables = intrinsicCallables ?? throw new ArgumentNullException(nameof(intrinsicCallables));
        _semanticDescriptors = semanticDescriptors ?? throw new ArgumentNullException(nameof(semanticDescriptors));
        _allowManagedCallables = allowManagedCallables;
        _managedCallableProjections = (managedCallableProjections ?? []).ToArray();
    }

    public IrStageId Id { get; } = new("air.to-ssa.minimal");

    public IrKind InputKind => AirIrKinds.Air;

    public IrKind OutputKind => SsaIrKinds.Ssa;

    public IrStageContract Contract { get; } = new(producesFacts: [SsaFacts.StructuralVerification]);

    public IrStageResult Run(IIrArtifact input, IrPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(input);

        var airArtifact = input as AirArtifact;
        if (airArtifact is null)
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.artifact", $"Expected AIR artifact, got artifact kind '{input.Kind}'.")]);
        }

        var airVerification = _airVerifier.Verify(input, context);
        if (!airVerification.IsSuccess)
            ThrowDiagnostics(airVerification.Diagnostics);

        var cfg = _cfgBuilder.Build(airArtifact!.Program.Instructions).Graph;
        var stackAnalysis = _stackAnalyzer.Analyze(cfg);
        if (stackAnalysis.Diagnostics.Count != 0)
            ThrowDiagnostics(stackAnalysis.Diagnostics.Select(static x => Diagnostic("air.to-ssa.stack", x)));

        var lowering = new LoweringState(cfg, stackAnalysis);
        var module = new SsaModule(
            new SsaModuleId("air.module"),
            [LowerFunction(cfg, stackAnalysis, lowering)]);
        var artifact = new SsaArtifact(
            module,
            new SsaManagedCallableBindingSet(lowering.ManagedCallableBindings));

        var ssaVerifier = lowering.ManagedCallableDescriptors.Count == 0
            ? _ssaVerifier
            : new StructuralSsaVerifier(
                SsaCoreDescriptors.ConstantMaterialization,
                MergeSemanticDescriptors(
                    _semanticDescriptors,
                    lowering.ManagedCallableDescriptors));
        var ssaVerification = ssaVerifier.Verify(artifact, context);
        if (!ssaVerification.IsSuccess)
            ThrowDiagnostics(ssaVerification.Diagnostics);

        return new IrStageResult(artifact, new IrFactSet([SsaFacts.StructuralVerification]));
    }

    private SsaFunction LowerFunction(
        AirControlFlowGraph cfg,
        AirStackAnalysisResult stackAnalysis,
        LoweringState lowering)
    {
        var returnType = DetermineReturnType(cfg, stackAnalysis);
        var blocks = cfg.Blocks.Select(block => LowerBlock(block, stackAnalysis, lowering)).ToArray();
        return new SsaFunction(
            new SsaFunctionId("air.entry"),
            lowering.MapBlock(cfg.EntryBlockId),
            blocks,
            returnType: returnType);
    }

    private static SsaTypeId? DetermineReturnType(AirControlFlowGraph cfg, AirStackAnalysisResult stackAnalysis)
    {
        SsaTypeId? returnType = null;
        foreach (var block in cfg.Blocks.Where(static x => x.Terminator.Kind == AirBlockTerminatorKind.End))
        {
            if (!stackAnalysis.ExitStates.TryGetValue(block.Id, out var exitState))
                continue;

            if (exitState.Types.Count > 1)
            {
                ThrowDiagnostics(
                [
                    Diagnostic(
                        "air.to-ssa.return-arity",
                        $"AIR block '{block.Id}' exits with {exitState.Types.Count} stack values; only zero or one return value is supported.")
                ]);
            }

            if (exitState.Types.Count == 0)
                continue;

            var type = MapType(exitState.Types[0]);
            if (returnType is not null && returnType.Value != type)
            {
                ThrowDiagnostics(
                [
                    Diagnostic(
                        "air.to-ssa.return-type",
                        $"AIR end blocks produce incompatible return types '{returnType}' and '{type}'.")
                ]);
            }

            returnType = type;
        }

        return returnType;
    }

    private SsaBlock LowerBlock(
        AirBasicBlock block,
        AirStackAnalysisResult stackAnalysis,
        LoweringState lowering)
    {
        if (!stackAnalysis.EntryStates.TryGetValue(block.Id, out var entryState))
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.unreachable", $"AIR block '{block.Id}' has no stack entry state.")]);
        }

        var ssaBlockId = lowering.MapBlock(block.Id);
        var parameters = entryState!.Types
            .Select((type, index) => new SsaBlockParameter(new SsaValue(
                new SsaValueId($"%{ssaBlockId.Value}_arg{index}"),
                MapType(type))))
            .ToArray();
        var stack = parameters.Select(static x => x.Value.Id).ToList();
        var instructions = new List<ISsaInstruction>();

        for (var offset = 0; offset < block.Instructions.Count; offset++)
        {
            var instruction = block.Instructions[offset];
            var isTerminatorInstruction = offset == block.Instructions.Count - 1 &&
                                          instruction.UOpCode is UOpCode.Jmp or UOpCode.JmpIf or UOpCode.JmpIfNot;
            if (isTerminatorInstruction)
                break;

            LowerInstruction(
                block,
                instruction,
                lowering,
                stack,
                instructions,
                _intrinsicDescriptors,
                _intrinsicCallables,
                _allowManagedCallables,
                _managedCallableProjections,
                _semanticDescriptors);
        }

        return new SsaBlock(
            ssaBlockId,
            parameters,
            terminator: LowerTerminator(block, lowering, stack),
            instructions: instructions);
    }

    private static void LowerInstruction(
        AirBasicBlock block,
        Instruction instruction,
        LoweringState lowering,
        List<SsaValueId> stack,
        List<ISsaInstruction> instructions,
        AirIntrinsicDescriptorSet intrinsicDescriptors,
        IReadOnlyDictionary<string, CallableId> intrinsicCallables,
        bool allowManagedCallables,
        IReadOnlyList<ISsaManagedCallableProjection> managedCallableProjections,
        SemanticDescriptorSet semanticDescriptors)
    {
        switch (instruction.UOpCode)
        {
            case UOpCode.Nop:
            case UOpCode.Label:
            case UOpCode.Annotate:
                return;
            case UOpCode.Push:
                LowerPush(block, instruction, lowering, stack, instructions);
                return;
            case UOpCode.Drop:
                if (stack.Count == 0)
                    ThrowDiagnostics([Diagnostic("air.to-ssa.stack-underflow", $"AIR Drop in block '{block.Id}' consumes an empty SSA stack.")]);
                stack.RemoveAt(stack.Count - 1);
                return;
            case UOpCode.Intrinsic:
                LowerIntrinsic(
                    block,
                    instruction,
                    lowering,
                    stack,
                    instructions,
                    intrinsicDescriptors,
                    intrinsicCallables,
                    allowManagedCallables,
                    managedCallableProjections,
                    semanticDescriptors);
                return;
            default:
                ThrowDiagnostics([Diagnostic("air.to-ssa.opcode", $"AIR opcode '{instruction.UOpCode}' in block '{block.Id}' is not supported.")]);
                return;
        }
    }

    private static void LowerPush(
        AirBasicBlock block,
        Instruction instruction,
        LoweringState lowering,
        List<SsaValueId> stack,
        List<ISsaInstruction> instructions)
    {
        if (instruction.Operands.Count != 1)
            ThrowDiagnostics([Diagnostic("air.to-ssa.push", $"AIR Push in block '{block.Id}' must have exactly one operand.")]);

        var value = instruction.Operands[0];
        if (value is AirExternalValueReference external)
        {
            LowerExternalValue(block, external, lowering, stack, instructions);
            return;
        }

        var result = new SsaValue(lowering.NextValue(), value switch
        {
            bool => SsaTypes.Bool,
            int => SsaTypes.Int32,
            double => SsaTypes.Float64,
            _ => throw new AirToSsaConversionException(
            [
                Diagnostic(
                    "air.to-ssa.push-type",
                    $"AIR Push in block '{block.Id}' has unsupported value type '{value?.GetType().FullName ?? "<null>"}'.")
            ])
        });

        var opId = value switch
        {
            bool => SsaOperations.ConstantBool,
            int => SsaOperations.ConstantInt32,
            double => SsaOperations.ConstantFloat64,
            _ => throw new AirToSsaConversionException(
            [
                Diagnostic(
                    "air.to-ssa.push-type",
                    $"AIR Push in block '{block.Id}' has unsupported value type '{value?.GetType().FullName ?? "<null>"}'.")
            ])
        };
        instructions.Add(new SsaOperation(
            lowering.NextOperation(),
            opId,
            results: [result],
            attributes: new SsaAttributeBag([new SsaAttribute(SsaAttributeKeys.ConstantValue, FormatConstantValue(value!))])));
        stack.Add(result.Id);
    }

    private static void LowerExternalValue(
        AirBasicBlock block,
        AirExternalValueReference external,
        LoweringState lowering,
        List<SsaValueId> stack,
        List<ISsaInstruction> instructions)
    {
        var (type, operation) = external.ValueType == typeof(int)
            ? (SsaTypes.Int32, SsaOperations.LoadExternalInt32)
            : external.ValueType == typeof(bool)
                ? (SsaTypes.Bool, SsaOperations.LoadExternalBool)
                : external.ValueType == typeof(double)
                    ? (SsaTypes.Float64, SsaOperations.LoadExternalFloat64)
                    : throw new AirToSsaConversionException(
                    [
                        Diagnostic(
                            "air.to-ssa.external-type",
                            $"AIR external load in block '{block.Id}' has unsupported value type '{external.ValueType.FullName}'.")
                    ]);

        var result = new SsaValue(lowering.NextValue(), type);
        instructions.Add(new SsaOperation(
            lowering.NextOperation(),
            operation,
            results: [result],
            attributes: new SsaAttributeBag(
            [
                new SsaAttribute(
                    SsaAttributeKeys.ExternalSlot,
                    external.Slot.ToString(CultureInfo.InvariantCulture))
            ])));
        stack.Add(result.Id);
    }

    private static void LowerIntrinsic(
        AirBasicBlock block,
        Instruction instruction,
        LoweringState lowering,
        List<SsaValueId> stack,
        List<ISsaInstruction> instructions,
        AirIntrinsicDescriptorSet intrinsicDescriptors,
        IReadOnlyDictionary<string, CallableId> intrinsicCallables,
        bool allowManagedCallables,
        IReadOnlyList<ISsaManagedCallableProjection> managedCallableProjections,
        SemanticDescriptorSet semanticDescriptors)
    {
        if (instruction.Operands.Count == 0 || instruction.Operands[0] is not string intrinsicId)
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.intrinsic", $"AIR Intrinsic in block '{block.Id}' must start with a string intrinsic identifier.")]);
            return;
        }

        if (allowManagedCallables &&
            TryLowerManagedIntrinsic(
                block,
                instruction,
                lowering,
                stack,
                instructions,
                intrinsicId,
                managedCallableProjections,
                semanticDescriptors))
            return;

        if (!intrinsicDescriptors.TryGet(intrinsicId, out var descriptor) ||
            !intrinsicCallables.TryGetValue(intrinsicId, out var callable))
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.intrinsic.rule-missing", $"AIR Intrinsic '{intrinsicId}' in block '{block.Id}' has no registered SSA lowering rule in the active SSA route profile.")]);
            return;
        }

        if (instruction.Operands.Count - 1 != descriptor.DataOperandCount)
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.intrinsic-shape", $"AIR Intrinsic '{intrinsicId}' in block '{block.Id}' has {instruction.Operands.Count - 1} data operands; expected {descriptor.DataOperandCount}.")]);
        }

        if (stack.Count < descriptor.ParameterTypes.Count)
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.stack-underflow", $"AIR Intrinsic '{intrinsicId}' in block '{block.Id}' consumes {descriptor.ParameterTypes.Count} operands from a stack of {stack.Count}.")]);
        }

        var operands = stack.Skip(stack.Count - descriptor.ParameterTypes.Count).ToArray();
        stack.RemoveRange(stack.Count - descriptor.ParameterTypes.Count, descriptor.ParameterTypes.Count);

        var results = descriptor.ResultTypes
            .Select(type => new SsaValue(lowering.NextValue(), MapType(type)))
            .ToArray();
        instructions.Add(new SsaCall(
            lowering.NextOperation(),
            callable,
            operands,
            results));
        stack.AddRange(results.Select(static x => x.Id));
    }

    private static bool TryLowerManagedIntrinsic(
        AirBasicBlock block,
        Instruction instruction,
        LoweringState lowering,
        List<SsaValueId> stack,
        List<ISsaInstruction> instructions,
        string intrinsicId,
        IReadOnlyList<ISsaManagedCallableProjection> managedCallableProjections,
        SemanticDescriptorSet semanticDescriptors)
    {
        if (intrinsicId == AirIntrinsicIds.CallCSharp)
            return LowerManagedMethodCall(
                block,
                instruction,
                lowering,
                stack,
                instructions,
                managedCallableProjections,
                semanticDescriptors);

        if (intrinsicId == AirIntrinsicIds.CallCSharpConstructor)
            return LowerManagedConstructorCall(block, instruction, lowering, stack, instructions);

        return false;
    }

    private static bool LowerManagedMethodCall(
        AirBasicBlock block,
        Instruction instruction,
        LoweringState lowering,
        List<SsaValueId> stack,
        List<ISsaInstruction> instructions,
        IReadOnlyList<ISsaManagedCallableProjection> managedCallableProjections,
        SemanticDescriptorSet semanticDescriptors)
    {
        if (!AirManagedCallIntrinsicDescriptorResolver.Instance.TryResolve(instruction, out var airDescriptor, out var airDiagnostic))
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.managed-call", $"AIR managed call in block '{block.Id}' is not supported. {airDiagnostic}")]);
        }

        if (!TryExtractManagedMethod(instruction, out var method, out var consumesInstanceReceiver, out var diagnostic))
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.managed-call", $"AIR managed call in block '{block.Id}' is not supported. {diagnostic}")]);
        }

        foreach (var projection in managedCallableProjections)
        {
            if (!projection.TryProject(method!, consumesInstanceReceiver, out var projectedCallable))
                continue;

            if (!semanticDescriptors.TryGetCallable(projectedCallable, out _))
            {
                ThrowDiagnostics([
                    Diagnostic(
                        "air.to-ssa.managed-call.projection.unregistered",
                        $"Managed-call projection mapped '{method}' to unregistered SSA callable '{projectedCallable}'.")
                ]);
            }

            LowerResolvedIntrinsic(
                block,
                lowering,
                stack,
                instructions,
                AirIntrinsicIds.CallCSharp,
                airDescriptor!,
                projectedCallable);
            return true;
        }

        if (!SsaManagedCallables.TryCreateMethod(method!, consumesInstanceReceiver, out var callable, out var semanticDescriptor, out diagnostic))
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.managed-call", $"AIR managed method '{method}' in block '{block.Id}' cannot be represented as SSA callable. {diagnostic}")]);
        }

        lowering.AddManagedCallableBinding(callable, semanticDescriptor, method!);
        LowerResolvedIntrinsic(block, lowering, stack, instructions, AirIntrinsicIds.CallCSharp, airDescriptor!, callable);
        return true;
    }

    private static bool LowerManagedConstructorCall(
        AirBasicBlock block,
        Instruction instruction,
        LoweringState lowering,
        List<SsaValueId> stack,
        List<ISsaInstruction> instructions)
    {
        if (!AirManagedCallIntrinsicDescriptorResolver.Instance.TryResolve(instruction, out var airDescriptor, out var airDiagnostic))
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.managed-ctor", $"AIR managed constructor call in block '{block.Id}' is not supported. {airDiagnostic}")]);
        }

        if (instruction.Operands.Count != 2 || instruction.Operands[1] is not ConstructorInfo constructor)
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.managed-ctor", $"AIR managed constructor call in block '{block.Id}' requires a ConstructorInfo operand.")]);
        }

        constructor = (ConstructorInfo)instruction.Operands[1];
        if (!SsaManagedCallables.TryCreateConstructor(constructor, out var callable, out var semanticDescriptor, out var diagnostic))
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.managed-ctor", $"AIR managed constructor '{constructor}' in block '{block.Id}' cannot be represented as SSA callable. {diagnostic}")]);
        }

        lowering.AddManagedCallableBinding(callable, semanticDescriptor, constructor);
        LowerResolvedIntrinsic(block, lowering, stack, instructions, AirIntrinsicIds.CallCSharpConstructor, airDescriptor!, callable);
        return true;
    }

    private static void LowerResolvedIntrinsic(
        AirBasicBlock block,
        LoweringState lowering,
        List<SsaValueId> stack,
        List<ISsaInstruction> instructions,
        string intrinsicId,
        AirIntrinsicDescriptor descriptor,
        CallableId callable)
    {
        if (stack.Count < descriptor.ParameterTypes.Count)
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.stack-underflow", $"AIR Intrinsic '{intrinsicId}' in block '{block.Id}' consumes {descriptor.ParameterTypes.Count} operands from a stack of {stack.Count}.")]);
        }

        var operands = stack.Skip(stack.Count - descriptor.ParameterTypes.Count).ToArray();
        stack.RemoveRange(stack.Count - descriptor.ParameterTypes.Count, descriptor.ParameterTypes.Count);

        var results = descriptor.ResultTypes
            .Select(type => new SsaValue(lowering.NextValue(), MapType(type)))
            .ToArray();
        instructions.Add(new SsaCall(
            lowering.NextOperation(),
            callable,
            operands,
            results));
        stack.AddRange(results.Select(static x => x.Id));
    }

    private static bool TryExtractManagedMethod(
        Instruction instruction,
        out MethodInfo method,
        out bool consumesInstanceReceiver,
        out string? diagnostic)
    {
        method = default!;
        consumesInstanceReceiver = false;
        diagnostic = null;

        if (instruction.Operands.Count != 2)
        {
            diagnostic = $"AIR intrinsic '{AirIntrinsicIds.CallCSharp}' expects one data operand.";
            return false;
        }

        if (instruction.Operands[1] is MethodInfo methodInfo)
        {
            method = methodInfo;
            consumesInstanceReceiver = !methodInfo.IsStatic;
            return true;
        }

        var operand = instruction.Operands[1];
        var operandType = operand?.GetType();
        var methodProperty = operandType?.GetProperty("Method", BindingFlags.Public | BindingFlags.Instance);
        if (methodProperty?.GetValue(operand) is not MethodInfo descriptorMethod)
        {
            diagnostic = $"AIR intrinsic '{AirIntrinsicIds.CallCSharp}' requires MethodInfo or a descriptor with MethodInfo Method property.";
            return false;
        }

        var receiverProperty = operandType!.GetProperty("Receiver", BindingFlags.Public | BindingFlags.Instance);
        var receiver = receiverProperty?.GetValue(operand);
        var receiverTypeName = receiver?.GetType().FullName ?? string.Empty;
        if (receiverTypeName.EndsWith("+Static", StringComparison.Ordinal) ||
            receiverTypeName.EndsWith(".Static", StringComparison.Ordinal))
        {
            method = descriptorMethod;
            consumesInstanceReceiver = !descriptorMethod.IsStatic;
            return true;
        }

        diagnostic = "Execution-scoped provider descriptors are runtime-bound and are not representable as backend-neutral SSA managed callables yet.";
        return false;
    }

    private static string FormatConstantValue(object value) =>
        value switch
        {
            double doubleValue => doubleValue.ToString("R", CultureInfo.InvariantCulture),
            _ => value.ToString()!
        };

    private static SsaTerminator LowerTerminator(
        AirBasicBlock block,
        LoweringState lowering,
        List<SsaValueId> stack)
    {
        return block.Terminator.Kind switch
        {
            AirBlockTerminatorKind.End => SsaTerminator.Return(stack),
            AirBlockTerminatorKind.Fallthrough or AirBlockTerminatorKind.Jump => LowerJump(block, lowering, stack),
            AirBlockTerminatorKind.ConditionalJump => LowerBranch(block, lowering, stack),
            _ => throw new AirToSsaConversionException(
            [
                Diagnostic("air.to-ssa.terminator", $"AIR block '{block.Id}' has unsupported terminator '{block.Terminator.Kind}'.")
            ])
        };
    }

    private static SsaTerminator LowerJump(AirBasicBlock block, LoweringState lowering, IReadOnlyList<SsaValueId> stack)
    {
        if (block.Terminator.Successors.Count != 1)
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.jump", $"AIR block '{block.Id}' jump/fallthrough has {block.Terminator.Successors.Count} successors; expected 1.")]);
        }

        return SsaTerminator.Jump(lowering.MapBlock(block.Terminator.Successors[0].Target), stack);
    }

    private static SsaTerminator LowerBranch(AirBasicBlock block, LoweringState lowering, List<SsaValueId> stack)
    {
        if (block.Terminator.Successors.Count != 2)
        {
            ThrowDiagnostics([Diagnostic("air.to-ssa.branch", $"AIR block '{block.Id}' conditional branch must have exactly two successors for SSA lowering.")]);
        }

        if (stack.Count == 0)
            ThrowDiagnostics([Diagnostic("air.to-ssa.branch-condition", $"AIR block '{block.Id}' conditional branch consumes an empty SSA stack.")]);

        var condition = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        var trueEdge = block.Terminator.Successors.Single(x => x.Kind == AirControlFlowEdgeKind.ConditionTrue);
        var falseEdge = block.Terminator.Successors.Single(x => x.Kind == AirControlFlowEdgeKind.ConditionFalse);

        return SsaTerminator.Branch(
            condition,
            lowering.MapBlock(trueEdge.Target),
            stack,
            lowering.MapBlock(falseEdge.Target),
            stack);
    }

    private static SsaTypeId MapType(AirValueTypeId type)
    {
        if (type == AirValueTypes.Bool)
            return SsaTypes.Bool;
        if (type == AirValueTypes.Int32)
            return SsaTypes.Int32;
        if (type == AirValueTypes.Float64)
            return SsaTypes.Float64;
        if (type == AirValueTypes.Object)
            return SsaTypes.Object;

        throw new AirToSsaConversionException([Diagnostic("air.to-ssa.type", $"Unsupported AIR stack type '{type}'.")]);
    }

    private static SemanticDescriptorSet MergeSemanticDescriptors(
        SemanticDescriptorSet baseDescriptors,
        IReadOnlyList<CallableDescriptor> additionalCallables)
    {
        var types = baseDescriptors.Types
            .GroupBy(static x => x.Id)
            .Select(static x => x.First())
            .ToArray();
        var callables = baseDescriptors.Callables
            .Concat(additionalCallables)
            .GroupBy(static x => x.Id)
            .Select(static x => x.First())
            .ToArray();

        return new SemanticDescriptorSet(types, callables);
    }

    private static void ThrowDiagnostics(IEnumerable<IrDiagnostic> diagnostics) =>
        throw new AirToSsaConversionException(diagnostics);

    private static IrDiagnostic Diagnostic(string code, string message) =>
        new(IrDiagnosticSeverity.Error, code, message);

    private sealed class LoweringState
    {
        private readonly Dictionary<AirBlockId, SsaBlockId> _blockIds;
        private readonly Dictionary<CallableId, SsaManagedCallableBinding> _managedCallableBindings = new();
        private int _nextOperation;
        private int _nextValue;

        public LoweringState(AirControlFlowGraph cfg, AirStackAnalysisResult stackAnalysis)
        {
            _blockIds = cfg.Blocks.ToDictionary(static x => x.Id, static x => new SsaBlockId(x.Id.Value));
            StackAnalysis = stackAnalysis;
        }

        public AirStackAnalysisResult StackAnalysis { get; }

        public IReadOnlyList<SsaManagedCallableBinding> ManagedCallableBindings => _managedCallableBindings.Values.ToArray();

        public IReadOnlyList<CallableDescriptor> ManagedCallableDescriptors =>
            _managedCallableBindings.Values.Select(static binding => binding.Descriptor).ToArray();

        public SsaBlockId MapBlock(AirBlockId blockId) => _blockIds[blockId];

        public SsaOperationId NextOperation() => new($"op{_nextOperation++:0000}");

        public SsaValueId NextValue() => new($"%v{_nextValue++:0000}");

        public void AddManagedCallableBinding(
            CallableId callable,
            CallableDescriptor descriptor,
            System.Reflection.MethodBase member)
        {
            var binding = new SsaManagedCallableBinding(callable, descriptor, member);
            if (_managedCallableBindings.TryGetValue(callable, out var existing))
            {
                if (!existing.IsEquivalentTo(binding))
                {
                    ThrowDiagnostics([
                        Diagnostic(
                            "air.to-ssa.managed-call.binding.conflict",
                            $"Managed callable '{callable}' resolved to incompatible execution-scoped bindings.")
                    ]);
                }

                return;
            }

            _managedCallableBindings.Add(callable, binding);
        }
    }
}
