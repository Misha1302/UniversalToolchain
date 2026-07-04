using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;

namespace UniversalToolchain.Ssa.Emission;

public sealed class SsaToAirConverter : IIrConverter
{
    private readonly StructuralSsaVerifier _ssaVerifier;
    private readonly StructuralAirVerifier _airVerifier;
    private readonly SsaCallableLoweringPlanner _callLoweringPlanner;
    private readonly SemanticDescriptorSet _semanticDescriptors;

    public SsaToAirConverter()
        : this(
            new StructuralSsaVerifier(SsaCoreDescriptors.ConstantMaterialization),
            new StructuralAirVerifier(
                new AirControlFlowGraphBuilder(),
                new AirStackAnalyzer(AirIntrinsicDescriptorSet.Empty)),
            new SsaCallableLoweringPlanner(
                SemanticDescriptorSet.Empty,
                SsaCallableLoweringTargetSet.Empty,
                AirIntrinsicDescriptorSet.Empty),
            SemanticDescriptorSet.Empty)
    {
    }

    public SsaToAirConverter(StructuralSsaVerifier ssaVerifier, StructuralAirVerifier airVerifier)
        : this(
            ssaVerifier,
            airVerifier,
            new SsaCallableLoweringPlanner(
                SemanticDescriptorSet.Empty,
                SsaCallableLoweringTargetSet.Empty,
                AirIntrinsicDescriptorSet.Empty),
            SemanticDescriptorSet.Empty)
    {
    }

    public SsaToAirConverter(
        StructuralSsaVerifier ssaVerifier,
        StructuralAirVerifier airVerifier,
        SsaCallAirIntrinsicLoweringSet callLowerings,
        AirIntrinsicDescriptorSet airIntrinsics)
        : this(
            ssaVerifier,
            airVerifier,
            new SsaCallableLoweringPlanner(
                SsaPreviewSemanticDescriptors.ArithmeticInt32,
                callLowerings.ToTargetSet(),
                airIntrinsics),
            SsaPreviewSemanticDescriptors.ArithmeticInt32)
    {
    }

    public SsaToAirConverter(
        StructuralSsaVerifier ssaVerifier,
        StructuralAirVerifier airVerifier,
        SsaCallableLoweringTargetSet callLoweringTargets,
        AirIntrinsicDescriptorSet airIntrinsics)
        : this(
            ssaVerifier,
            airVerifier,
            new SsaCallableLoweringPlanner(
                SsaPreviewSemanticDescriptors.ArithmeticInt32,
                callLoweringTargets,
                airIntrinsics),
            SsaPreviewSemanticDescriptors.ArithmeticInt32)
    {
    }

    public SsaToAirConverter(
        StructuralSsaVerifier ssaVerifier,
        StructuralAirVerifier airVerifier,
        SsaCallAirIntrinsicLoweringPlanner callLoweringPlanner)
        : this(
            ssaVerifier,
            airVerifier,
            callLoweringPlanner.AsCallablePlanner(),
            SsaPreviewSemanticDescriptors.ArithmeticInt32)
    {
    }

    public SsaToAirConverter(
        StructuralSsaVerifier ssaVerifier,
        StructuralAirVerifier airVerifier,
        SsaCallableLoweringPlanner callLoweringPlanner)
        : this(
            ssaVerifier,
            airVerifier,
            callLoweringPlanner,
            SemanticDescriptorSet.Empty)
    {
    }

    public SsaToAirConverter(
        StructuralSsaVerifier ssaVerifier,
        StructuralAirVerifier airVerifier,
        SsaCallableLoweringPlanner callLoweringPlanner,
        SemanticDescriptorSet semanticDescriptors)
    {
        _ssaVerifier = ssaVerifier ?? throw new ArgumentNullException(nameof(ssaVerifier));
        _airVerifier = airVerifier ?? throw new ArgumentNullException(nameof(airVerifier));
        _callLoweringPlanner = callLoweringPlanner ?? throw new ArgumentNullException(nameof(callLoweringPlanner));
        _semanticDescriptors = semanticDescriptors ?? throw new ArgumentNullException(nameof(semanticDescriptors));
    }

    public IrStageId Id { get; } = new("ssa.to-air.minimal");

    public IrKind InputKind => SsaIrKinds.Ssa;

    public IrKind OutputKind => AirIrKinds.Air;

    public IrStageContract Contract { get; } = new(
        requiresFacts: [SsaFacts.StructuralVerification],
        producesFacts: [AirFacts.StructuralVerification, AirFacts.ControlFlowGraph]);

    public IrStageResult Run(IIrArtifact input, IrPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(input);

        var ssaArtifact = input as SsaArtifact;
        if (ssaArtifact is null)
        {
            ThrowDiagnostics([Diagnostic("ssa.to-air.artifact", $"Expected SSA artifact, got artifact kind '{input.Kind}'.")]);
        }

        var managedDescriptorDiagnostics = new List<IrDiagnostic>();
        var managedDescriptors = CollectManagedCallableDescriptors(ssaArtifact!, managedDescriptorDiagnostics);
        if (managedDescriptorDiagnostics.Count != 0)
        {
            ThrowDiagnostics(
                managedDescriptorDiagnostics.Prepend(Diagnostic("ssa.to-air.input.invalid", "Input SSA contains invalid managed callable identifiers.")));
        }

        var semanticDescriptors = managedDescriptors.Count == 0
            ? _semanticDescriptors
            : MergeSemanticDescriptors(_semanticDescriptors, managedDescriptors);
        var ssaVerifier = managedDescriptors.Count == 0
            ? _ssaVerifier
            : new StructuralSsaVerifier(
                SsaCoreDescriptors.ConstantMaterialization,
                semanticDescriptors);
        var ssaVerification = ssaVerifier.Verify(input, context);
        if (!ssaVerification.IsSuccess)
        {
            ThrowDiagnostics(ssaVerification.Diagnostics.Prepend(Diagnostic("ssa.to-air.input.invalid", "Input SSA failed structural verification.")));
        }

        var function = SelectFunction(ssaArtifact!.Module);
        var air = new AbstractIR();
        var state = new EmissionState(function);
        var planner = _callLoweringPlanner.WithAdditionalCallables(managedDescriptors);

        foreach (var block in state.Blocks)
        {
            EmitBlock(air, block, state, planner);
        }

        var artifact = new AirArtifact(air);
        var airVerification = _airVerifier.Verify(artifact, context);
        if (!airVerification.IsSuccess)
        {
            ThrowDiagnostics(airVerification.Diagnostics.Prepend(Diagnostic("ssa.to-air.output.invalid", "Emitted AIR failed structural verification.")));
        }

        return new IrStageResult(
            artifact,
            new IrFactSet([AirFacts.StructuralVerification, AirFacts.ControlFlowGraph]));
    }

    private static SsaFunction SelectFunction(SsaModule module)
    {
        if (module.Functions.Count != 1)
        {
            ThrowDiagnostics(
            [
                Diagnostic(
                    "ssa.to-air.function-count",
                    $"SSA to AIR emission supports exactly one function, got {module.Functions.Count}.")
            ]);
        }

        var function = module.Functions.Single();
        if (function.Parameters.Count != 0)
        {
            ThrowDiagnostics(
            [
                Diagnostic(
                    "ssa.to-air.function-parameters",
                    $"SSA function '{function.Id}' has {function.Parameters.Count} parameters; AIR has no matching parameter load in the minimal emitter.")
            ]);
        }

        return function;
    }

    private static void EmitBlock(
        AbstractIR air,
        SsaBlock block,
        EmissionState state,
        SsaCallableLoweringPlanner callLoweringPlanner)
    {
        air.SetLabel(state.LabelFor(block.Id));

        var stack = block.Parameters.Select(static x => x.Value.Id).ToList();
        foreach (var instruction in block.Instructions)
        {
            switch (instruction)
            {
                case SsaOperation operation:
                    EmitOperation(air, block, operation, state, stack);
                    break;
                case SsaCall call:
                    EmitCall(air, block, call, state, stack, callLoweringPlanner);
                    break;
                default:
                    ThrowDiagnostics(
                    [
                        Diagnostic(
                            "ssa.to-air.instruction.unsupported",
                            $"SSA instruction '{instruction.Id}' in block '{block.Id}' has unsupported shape '{instruction.GetType().Name}'.")
                    ]);
                    break;
            }
        }

        EmitTerminator(air, block, state, stack);
    }

    private static void EmitOperation(
        AbstractIR air,
        SsaBlock block,
        SsaOperation operation,
        EmissionState state,
        List<SsaValueId> stack)
    {
        if (operation.OpId == SsaOperations.ConstantInt32)
        {
            if (state.IsUnusedPureResult(operation))
                return;

            air.Push(ReadInt32Constant(block, operation));
            stack.Add(operation.Results.Single().Id);
            return;
        }

        if (operation.OpId == SsaOperations.ConstantBool)
        {
            if (state.IsUnusedPureResult(operation))
                return;

            air.Push(ReadBoolConstant(block, operation));
            stack.Add(operation.Results.Single().Id);
            return;
        }

        if (operation.OpId == SsaOperations.ConstantFloat64)
        {
            if (state.IsUnusedPureResult(operation))
                return;

            air.Push(ReadFloat64Constant(block, operation));
            stack.Add(operation.Results.Single().Id);
            return;
        }

        ThrowDiagnostics(
        [
            Diagnostic(
                "ssa.to-air.operation.unsupported",
                $"SSA operation '{operation.OpId}' in block '{block.Id}' cannot be emitted by the minimal AIR emitter.")
        ]);
    }

    private static void EmitCall(
        AbstractIR air,
        SsaBlock block,
        SsaCall call,
        EmissionState state,
        List<SsaValueId> stack,
        SsaCallableLoweringPlanner callLoweringPlanner)
    {
        if (!callLoweringPlanner.TrySelect(call, out var plan, out var failure))
        {
            ThrowDiagnostics(
            [
                Diagnostic(
                    failure.Code,
                    $"{failure.Message} Block '{block.Id}'.")
            ]);
            return;
        }

        switch (plan.Target.Kind)
        {
            case SsaCallableLoweringTargetKind.AirIntrinsic:
                EmitAirIntrinsicCall(air, block, call, state, stack, plan);
                return;
            case SsaCallableLoweringTargetKind.ManagedCall:
                EmitManagedCall(air, block, call, state, stack, plan);
                return;
            default:
                ThrowDiagnostics(
                [
                    Diagnostic(
                        "ssa.to-air.call-lowering.target",
                        $"SSA call '{call.Id}' to '{call.Callee}' in block '{block.Id}' selected unsupported lowering target '{plan.Target.Kind}'.")
                ]);
                return;
        }
    }

    private static void EmitAirIntrinsicCall(
        AbstractIR air,
        SsaBlock block,
        SsaCall call,
        EmissionState state,
        List<SsaValueId> stack,
        SsaCallableLoweringPlan plan)
    {
        var intrinsic = plan.Intrinsic ?? throw new InvalidOperationException("AIR intrinsic lowering plan must include an intrinsic descriptor.");
        if (call.Operands.Count != intrinsic.ParameterTypes.Count)
        {
            ThrowDiagnostics(
            [
                Diagnostic(
                    "ssa.to-air.call-operand-count",
                    $"SSA call '{call.Id}' to '{call.Callee}' in block '{block.Id}' lowers to AIR intrinsic '{intrinsic.Id}' with {intrinsic.ParameterTypes.Count} operands, but the call has {call.Operands.Count}.")
            ]);
        }

        if (call.Results.Count != intrinsic.ResultTypes.Count)
        {
            ThrowDiagnostics(
            [
                Diagnostic(
                    "ssa.to-air.call-result-count",
                    $"SSA call '{call.Id}' to '{call.Callee}' in block '{block.Id}' lowers to AIR intrinsic '{intrinsic.Id}' with {intrinsic.ResultTypes.Count} results, but the call has {call.Results.Count}.")
            ]);
        }

        VerifyCallResultTypes(block, call, intrinsic);
        RequireTopOfStack(block, $"call {call.Callee}", stack, call.Operands);

        air.Intrinsic(intrinsic.Id);
        stack.RemoveRange(stack.Count - call.Operands.Count, call.Operands.Count);

        if (state.IsUnusedSingleResult(call))
        {
            air.Drop();
            return;
        }

        foreach (var result in call.Results)
            stack.Add(result.Id);
    }

    private static void EmitManagedCall(
        AbstractIR air,
        SsaBlock block,
        SsaCall call,
        EmissionState state,
        List<SsaValueId> stack,
        SsaCallableLoweringPlan plan)
    {
        VerifyManagedCallShape(block, call, plan.Callable);
        RequireTopOfStack(block, $"managed call {call.Callee}", stack, call.Operands);

        switch (plan.ManagedMember)
        {
            case System.Reflection.MethodInfo method:
                air.AppendInstructions(new List<Instruction>
                {
                    new(UOpCode.Intrinsic, [AirIntrinsicIds.CallCSharp, method])
                });
                break;
            case System.Reflection.ConstructorInfo constructor:
                air.AppendInstructions(new List<Instruction>
                {
                    new(UOpCode.Intrinsic, [AirIntrinsicIds.CallCSharpConstructor, constructor])
                });
                break;
            default:
                ThrowDiagnostics([Diagnostic("ssa.to-air.managed-call.member", $"SSA managed call '{call.Id}' resolved to unsupported member kind '{plan.ManagedMember?.GetType().Name ?? "<null>"}'.")]);
                break;
        }

        stack.RemoveRange(stack.Count - call.Operands.Count, call.Operands.Count);

        if (state.IsUnusedSingleResult(call))
        {
            air.Drop();
            return;
        }

        foreach (var result in call.Results)
            stack.Add(result.Id);
    }

    private static void VerifyManagedCallShape(SsaBlock block, SsaCall call, CallableDescriptor descriptor)
    {
        if (call.Operands.Count != descriptor.Signature.ParameterTypes.Count)
        {
            ThrowDiagnostics(
            [
                Diagnostic(
                    "ssa.to-air.managed-call-operand-count",
                    $"SSA managed call '{call.Id}' to '{call.Callee}' in block '{block.Id}' expects {descriptor.Signature.ParameterTypes.Count} operands, but the call has {call.Operands.Count}.")
            ]);
        }

        if (call.Results.Count != descriptor.Signature.ResultTypes.Count)
        {
            ThrowDiagnostics(
            [
                Diagnostic(
                    "ssa.to-air.managed-call-result-count",
                    $"SSA managed call '{call.Id}' to '{call.Callee}' in block '{block.Id}' expects {descriptor.Signature.ResultTypes.Count} results, but the call has {call.Results.Count}.")
            ]);
        }

        for (var index = 0; index < call.Results.Count; index++)
        {
            var expectedType = descriptor.Signature.ResultTypes[index];
            if (call.Results[index].Type.Value != expectedType.Value)
            {
                ThrowDiagnostics(
                [
                    Diagnostic(
                        "ssa.to-air.managed-call-result-type",
                        $"SSA managed call '{call.Id}' result {index} in block '{block.Id}' has type '{call.Results[index].Type}', but managed callable '{call.Callee}' produces '{expectedType}'.")
                ]);
            }
        }
    }

    private static void VerifyCallResultTypes(SsaBlock block, SsaCall call, AirIntrinsicDescriptor intrinsic)
    {
        for (var index = 0; index < call.Results.Count; index++)
        {
            var expectedType = MapType(intrinsic.ResultTypes[index]);
            if (call.Results[index].Type != expectedType)
            {
                ThrowDiagnostics(
                [
                    Diagnostic(
                        "ssa.to-air.call-result-type",
                        $"SSA call '{call.Id}' result {index} in block '{block.Id}' has type '{call.Results[index].Type}', but AIR intrinsic '{intrinsic.Id}' produces '{expectedType}'.")
                ]);
            }
        }
    }

    private static void EmitTerminator(AbstractIR air, SsaBlock block, EmissionState state, List<SsaValueId> stack)
    {
        if (block.Terminator is null)
            ThrowDiagnostics([Diagnostic("ssa.to-air.terminator.missing", $"SSA block '{block.Id}' has no terminator.")]);

        switch (block.Terminator!.Kind)
        {
            case SsaTerminatorKind.Return:
                EmitReturn(block, state, stack);
                return;
            case SsaTerminatorKind.Jump:
                EmitJump(air, block, block.Terminator, state, stack);
                return;
            case SsaTerminatorKind.Branch:
                EmitBranch(air, block, block.Terminator, state, stack);
                return;
            case SsaTerminatorKind.Unreachable:
                ThrowDiagnostics([Diagnostic("ssa.to-air.terminator.unreachable", $"SSA block '{block.Id}' uses unreachable terminator; AIR has no equivalent in the minimal emitter.")]);
                return;
            default:
                ThrowDiagnostics([Diagnostic("ssa.to-air.terminator.unsupported", $"SSA block '{block.Id}' has unsupported terminator '{block.Terminator.Kind}'.")]);
                return;
        }
    }

    private static void EmitReturn(SsaBlock block, EmissionState state, IReadOnlyList<SsaValueId> stack)
    {
        if (state.NextBlock(block.Id) is not null)
        {
            ThrowDiagnostics(
            [
                Diagnostic(
                    "ssa.to-air.return.position",
                    $"SSA return block '{block.Id}' is not the final emitted block; AIR can only end at the end of the instruction stream in the minimal emitter.")
            ]);
        }

        RequireStack(block, "return", stack, block.Terminator!.Operands);
    }

    private static void EmitJump(
        AbstractIR air,
        SsaBlock block,
        SsaTerminator terminator,
        EmissionState state,
        IReadOnlyList<SsaValueId> stack)
    {
        var transfer = terminator.Transfers.Single();
        RequireStack(block, "jump", stack, transfer.Arguments);

        if (state.NextBlock(block.Id)?.Id == transfer.Target)
            return;

        air.Jmp(state.LabelFor(transfer.Target));
    }

    private static void EmitBranch(
        AbstractIR air,
        SsaBlock block,
        SsaTerminator terminator,
        EmissionState state,
        IReadOnlyList<SsaValueId> stack)
    {
        if (terminator.Transfers.Count != 2 || terminator.Operands.Count != 1)
        {
            ThrowDiagnostics([Diagnostic("ssa.to-air.branch.shape", $"SSA branch in block '{block.Id}' must have one condition and two transfers.")]);
        }

        var condition = terminator.Operands.Single();
        var first = terminator.Transfers[0];
        var second = terminator.Transfers[1];
        if (!first.Arguments.SequenceEqual(second.Arguments))
        {
            ThrowDiagnostics(
            [
                Diagnostic(
                    "ssa.to-air.branch.arguments",
                    $"SSA branch in block '{block.Id}' passes different block arguments on true and false edges; AIR conditional jumps keep one shared stack state.")
            ]);
        }

        var requiredStack = first.Arguments.Concat([condition]).ToArray();
        RequireStack(block, "branch", stack, requiredStack);

        var nextBlock = state.NextBlock(block.Id);
        if (nextBlock is null)
        {
            ThrowDiagnostics([Diagnostic("ssa.to-air.branch.position", $"SSA branch block '{block.Id}' has no fallthrough block in the emitted AIR order.")]);
        }

        if (nextBlock!.Id == second.Target)
        {
            air.JmpIf(state.LabelFor(first.Target));
            return;
        }

        if (nextBlock.Id == first.Target)
        {
            air.JmpIfNot(state.LabelFor(second.Target));
            return;
        }

        ThrowDiagnostics(
        [
            Diagnostic(
                "ssa.to-air.branch.fallthrough",
                $"SSA branch in block '{block.Id}' targets '{first.Target}' and '{second.Target}', but next emitted block is '{nextBlock.Id}'.")
        ]);
    }

    private static int ReadInt32Constant(SsaBlock block, SsaOperation operation)
    {
        var value = ReadConstant(block, operation);
        if (int.TryParse(value, out var result))
            return result;

        ThrowDiagnostics([Diagnostic("ssa.to-air.constant.i32", $"SSA constant '{operation.Id}' in block '{block.Id}' has invalid Int32 value '{value}'.")]);
        return default;
    }

    private static bool ReadBoolConstant(SsaBlock block, SsaOperation operation)
    {
        var value = ReadConstant(block, operation);
        if (bool.TryParse(value, out var result))
            return result;

        ThrowDiagnostics([Diagnostic("ssa.to-air.constant.bool", $"SSA constant '{operation.Id}' in block '{block.Id}' has invalid Bool value '{value}'.")]);
        return default;
    }

    private static double ReadFloat64Constant(SsaBlock block, SsaOperation operation)
    {
        var value = ReadConstant(block, operation);
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            return result;

        ThrowDiagnostics([Diagnostic("ssa.to-air.constant.f64", $"SSA constant '{operation.Id}' in block '{block.Id}' has invalid Float64 value '{value}'.")]);
        return default;
    }

    private static string ReadConstant(SsaBlock block, SsaOperation operation)
    {
        if (operation.Operands.Count != 0 || operation.Results.Count != 1)
        {
            ThrowDiagnostics(
            [
                Diagnostic(
                    "ssa.to-air.constant.shape",
                    $"SSA constant '{operation.Id}' in block '{block.Id}' must have zero operands and one result.")
            ]);
        }

        if (operation.Attributes.TryGet(SsaAttributeKeys.ConstantValue, out var attribute))
            return attribute.Value;

        ThrowDiagnostics([Diagnostic("ssa.to-air.constant.attribute", $"SSA constant '{operation.Id}' in block '{block.Id}' has no constant value attribute.")]);
        return string.Empty;
    }

    private static void RequireStack(
        SsaBlock block,
        string terminatorKind,
        IReadOnlyList<SsaValueId> actual,
        IReadOnlyList<SsaValueId> expected)
    {
        if (actual.SequenceEqual(expected))
            return;

        ThrowDiagnostics(
        [
            Diagnostic(
                "ssa.to-air.stack-shape.unsupported",
                $"SSA block '{block.Id}' {terminatorKind} requires stack [{Format(expected)}], but minimal AIR emission produced [{Format(actual)}].")
        ]);
    }

    private static void RequireTopOfStack(
        SsaBlock block,
        string operationKind,
        IReadOnlyList<SsaValueId> actual,
        IReadOnlyList<SsaValueId> expectedTop)
    {
        if (actual.Count >= expectedTop.Count &&
            actual.Skip(actual.Count - expectedTop.Count).SequenceEqual(expectedTop))
        {
            return;
        }

        ThrowDiagnostics(
        [
            Diagnostic(
                "ssa.to-air.stack-shape.unsupported",
                $"SSA block '{block.Id}' {operationKind} requires top-of-stack [{Format(expectedTop)}], but minimal AIR emission produced [{Format(actual)}].")
        ]);
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

        ThrowDiagnostics([Diagnostic("ssa.to-air.type.unsupported", $"AIR intrinsic result type '{type}' has no SSA type mapping.")]);
        return default;
    }

    private static IReadOnlyList<CallableDescriptor> CollectManagedCallableDescriptors(
        SsaArtifact artifact,
        List<IrDiagnostic> diagnostics)
    {
        var descriptors = new Dictionary<CallableId, CallableDescriptor>();
        foreach (var call in artifact.Module.Functions.SelectMany(static function => function.Blocks).SelectMany(static block => block.Calls))
        {
            if (!SsaManagedCallables.IsManagedCallable(call.Callee))
                continue;

            if (!SsaManagedCallables.TryResolve(call.Callee, out var resolution, out var diagnostic))
            {
                diagnostics.Add(Diagnostic(
                    "ssa.to-air.managed-call.resolve",
                    $"SSA managed call '{call.Id}' to '{call.Callee}' cannot be resolved. {diagnostic}"));
                continue;
            }

            descriptors.TryAdd(resolution.Descriptor.Id, resolution.Descriptor);
        }

        return descriptors.Values.ToArray();
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

    private static string Format(IEnumerable<SsaValueId> ids) => string.Join(", ", ids.Select(static x => x.Value));

    private static void ThrowDiagnostics(IEnumerable<IrDiagnostic> diagnostics) =>
        throw new SsaToAirEmissionException(diagnostics);

    private static IrDiagnostic Diagnostic(string code, string message) =>
        new(IrDiagnosticSeverity.Error, code, message);

    private sealed class EmissionState
    {
        private readonly IReadOnlyDictionary<SsaBlockId, Guid> _labels;
        private readonly IReadOnlyDictionary<SsaBlockId, int> _blockIndexes;
        private readonly IReadOnlyDictionary<SsaValueId, int> _useCounts;

        public EmissionState(SsaFunction function)
        {
            Blocks = OrderBlocks(function);
            _labels = Blocks.ToDictionary(static x => x.Id, static x => CreateStableGuid("ssa-block:" + x.Id.Value));
            _blockIndexes = Blocks.Select((block, index) => (block, index)).ToDictionary(static x => x.block.Id, static x => x.index);
            _useCounts = CountUses(Blocks);
        }

        public IReadOnlyList<SsaBlock> Blocks { get; }

        public Guid LabelFor(SsaBlockId blockId) => _labels[blockId];

        public SsaBlock? NextBlock(SsaBlockId blockId)
        {
            var index = _blockIndexes[blockId] + 1;
            return index < Blocks.Count ? Blocks[index] : null;
        }

        public bool IsUnusedPureResult(SsaOperation operation) =>
            operation.Results.Count == 1 && !_useCounts.ContainsKey(operation.Results.Single().Id);

        public bool IsUnusedSingleResult(ISsaInstruction instruction) =>
            instruction.Results.Count == 1 && !_useCounts.ContainsKey(instruction.Results.Single().Id);

        private static IReadOnlyList<SsaBlock> OrderBlocks(SsaFunction function)
        {
            var blocksById = function.Blocks.ToDictionary(static x => x.Id);
            var ordered = new List<SsaBlock>(function.Blocks.Count);
            var visited = new HashSet<SsaBlockId>();
            var queued = new HashSet<SsaBlockId>();
            var pending = new Queue<SsaBlockId>();

            Enqueue(function.EntryBlockId);
            while (pending.Count > 0)
            {
                var blockId = pending.Dequeue();
                if (!blocksById.TryGetValue(blockId, out var block) || !visited.Add(blockId))
                    continue;

                ordered.Add(block);
                foreach (var target in PreferredSuccessors(block))
                    Enqueue(target);
            }

            foreach (var block in function.Blocks)
            {
                if (visited.Add(block.Id))
                    ordered.Add(block);
            }

            return ordered;

            void Enqueue(SsaBlockId blockId)
            {
                if (!visited.Contains(blockId) && queued.Add(blockId))
                    pending.Enqueue(blockId);
            }
        }

        private static IEnumerable<SsaBlockId> PreferredSuccessors(SsaBlock block)
        {
            if (block.Terminator is null)
                yield break;

            if (block.Terminator.Kind == SsaTerminatorKind.Branch && block.Terminator.Transfers.Count == 2)
            {
                yield return block.Terminator.Transfers[1].Target;
                yield return block.Terminator.Transfers[0].Target;
                yield break;
            }

            if (block.Terminator.Kind == SsaTerminatorKind.Jump && block.Terminator.Transfers.Count == 1)
                yield return block.Terminator.Transfers[0].Target;
        }

        private static IReadOnlyDictionary<SsaValueId, int> CountUses(IEnumerable<SsaBlock> blocks)
        {
            var counts = new Dictionary<SsaValueId, int>();
            foreach (var block in blocks)
            {
                foreach (var instruction in block.Instructions)
                {
                    foreach (var operand in instruction.Operands)
                        AddUse(counts, operand);
                }

                if (block.Terminator is null)
                    continue;

                foreach (var operand in block.Terminator.Operands)
                    AddUse(counts, operand);

                foreach (var transfer in block.Terminator.Transfers)
                {
                    foreach (var argument in transfer.Arguments)
                        AddUse(counts, argument);
                }
            }

            return counts;
        }

        private static void AddUse(Dictionary<SsaValueId, int> counts, SsaValueId valueId)
        {
            counts[valueId] = counts.TryGetValue(valueId, out var count) ? count + 1 : 1;
        }

        private static Guid CreateStableGuid(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return new Guid(bytes.Take(16).ToArray());
        }
    }
}
