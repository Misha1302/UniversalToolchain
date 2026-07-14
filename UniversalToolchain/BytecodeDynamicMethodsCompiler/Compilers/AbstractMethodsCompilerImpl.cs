namespace BytecodeDynamicMethodsCompiler.Compilers;

public class AbstractMethodsCompilerImpl : IAbstractIrCompiler<CilCompilationOutput>
{
    private readonly AbstractMethodsIntrinsicCompiler _intrinsicCompiler;
    private readonly CilAbstractIrTypeSimulator _typeSimulator;

    public AbstractMethodsCompilerImpl()
        : this(new CilIntrinsicRegistry())
    {
    }

    public AbstractMethodsCompilerImpl(CilIntrinsicRegistry registry)
    {
        registry = registry.ArgNotNull();

        _intrinsicCompiler = new AbstractMethodsIntrinsicCompiler(registry);
        _typeSimulator = new CilAbstractIrTypeSimulator(_intrinsicCompiler);
    }

    public static IReadOnlyList<string> SupportedIntrinsicIds { get; } = BuildSupportedIntrinsicIds();

    public IReadOnlyList<string> SupportedIntrinsics => _intrinsicCompiler.SupportedIntrinsics;

    public CilCompilationOutput Compile(IAbstractIR air, CompilationInput input)
    {
        air = air.ArgNotNull();
        input = input.ArgNotNull();

        var requirements = CilExecutionRequirementAnalyzer.Analyze(air);
        var returnType = GetReturnType(air);
        var externalBindingTypes = input.ExternalBindings.Select(static x => x.Type).ToArray();
        var hasConstantPool = air.Instructions.Any(static instruction =>
            instruction.UOpCode == UOpCode.Push &&
            instruction.Operands.Count == 1 &&
            AirPushOperand.GetValue(instruction.Operands[0]) is not null);
        var argsTypes = requirements.RequiresExecutionEnvironment
            ? new[] { typeof(IExecutionEnvironment) }.Concat(externalBindingTypes).ToArray()
            : externalBindingTypes;
        if (hasConstantPool)
            argsTypes = new[] { typeof(ArtifactConstantPool) }.Concat(argsTypes).ToArray();

        var constantPoolArgumentIndex = hasConstantPool ? 0 : (int?)null;
        var executionEnvironmentArgumentIndex = requirements.RequiresExecutionEnvironment
            ? hasConstantPool ? 1 : 0
            : (int?)null;
        var externalArgumentOffset = (hasConstantPool ? 1 : 0) + (requirements.RequiresExecutionEnvironment ? 1 : 0);
        var externalSlots = input.ExternalBindings
            .Select((binding, slot) => new { binding.Name, Slot = slot })
            .ToDictionary(static x => x.Name, static x => x.Slot);
        var method = new DynamicMethod("main", returnType, argsTypes);
        using var il = new GroboIL(method);

        var constantPoolValues = new List<object>();
        var context = new CompilationContext(
            il,
            externalSlots,
            constantPoolValues,
            externalArgumentOffset,
            constantPoolArgumentIndex,
            executionEnvironmentArgumentIndex);
        var labelStacks = InitializeLabels(context, air);

        var typesStack = new List<Type>();

        foreach (var instruction in air.Instructions)
            CompileInstruction(context, instruction, typesStack, labelStacks);

        EmitMethodReturn(il, returnType, typesStack);
        return new CilCompilationOutput(
            method,
            hasConstantPool ? new ArtifactConstantPool(constantPoolValues) : null);
    }

    private static void EmitMethodReturn(GroboIL il, Type returnType, IReadOnlyList<Type> typesStack)
    {
        if (returnType == typeof(void))
        {
            Thrower.AssertAlways(typesStack.Count == 0, "Void AIR artifact must finish with an empty evaluation stack.");
            il.Ret();
            return;
        }

        Thrower.AssertAlways(typesStack.Count == 1, "Value-returning AIR artifact must finish with exactly one evaluation-stack value.");
        var actualReturnType = typesStack[0];
        if (actualReturnType.IsValueType && !returnType.IsValueType)
            il.Box(actualReturnType);

        il.Ret();
    }

    private Type GetReturnType(IAbstractIR air)
    {
        var stack = _typeSimulator.Simulate(air.Instructions);
        return stack.Count switch
        {
            0 => typeof(void),
            1 => stack[0],
            _ => Thrower.InvalidOpEx<Type>(
                $"AIR artifact finishes with {stack.Count} evaluation-stack values; expected zero or one.")
        };
    }

    private static IReadOnlyList<string> BuildSupportedIntrinsicIds()
    {
        var registry = new CilIntrinsicRegistry();
        return registry.SupportedIntrinsics;
    }

    private Dictionary<Guid, List<Type>> InitializeLabels(CompilationContext context, IAbstractIR bytecode)
    {
        foreach (var instruction in bytecode.Instructions)
        {
            if (instruction.UOpCode == UOpCode.Label)
            {
                var id = GetRequiredLabelId(instruction);
                context.InstructionLabels[id] = context.Il.DefineLabel($"Instruction {id}");
            }
        }

        var labelStacks = new Dictionary<Guid, List<Type>>();
        _typeSimulator.Simulate(bytecode.Instructions, labelStacks);
        return labelStacks;
    }

    private void CompileInstruction(
        CompilationContext context,
        Instruction instruction,
        List<Type> stack,
        Dictionary<Guid, List<Type>> labelStacks
    )
    {
        switch (instruction.UOpCode)
        {
            case UOpCode.Nop:
                context.Il.Nop();
                break;
            case UOpCode.Push:
            {
                var operand = instruction.Operands[0];
                var declaredType = AirPushOperand.GetDeclaredType(operand);
                var value = AirPushOperand.GetValue(operand);
                if (value is null)
                    PushNull(context.Il, declaredType);
                else
                    PushValue(context, value, declaredType);
                stack.Push(declaredType);
                break;
            }
            case UOpCode.Drop:
                context.Il.Pop();
                stack.Pop();
                break;
            case UOpCode.Jmp:
                HandleJump(context, instruction, static (il, label) => il.Br(label));
                break;
            case UOpCode.JmpIf:
                stack.Pop();
                HandleJump(context, instruction, static (il, label) => il.Brtrue(label));
                break;
            case UOpCode.JmpIfNot:
                stack.Pop();
                HandleJump(context, instruction, static (il, label) => il.Brfalse(label));
                break;
            case UOpCode.Label:
                MarkLabel(context, instruction, stack, labelStacks);
                break;
            case UOpCode.Annotate:
                break;
            case UOpCode.Intrinsic:
                _intrinsicCompiler.Compile(context, instruction, stack);
                break;
            default:
                Thrower.InvalidOpEx();
                break;
        }
    }

    private static void HandleJump(
        CompilationContext context,
        Instruction instruction,
        Action<GroboIL, GroboIL.Label> branchAction
    )
    {
        var labelId = GetRequiredLabelId(instruction);
        branchAction(context.Il, context.InstructionLabels[labelId]);
    }

    private static void MarkLabel(
        CompilationContext context,
        Instruction instruction,
        List<Type> stack,
        Dictionary<Guid, List<Type>> labelStacks
    )
    {
        var labelId = GetRequiredLabelId(instruction);

        if (labelStacks.TryGetValue(labelId, out var savedStack))
        {
            stack.Clear();
            stack.AddRange(savedStack);
        }

        context.Il.MarkLabel(context.InstructionLabels[labelId]);
    }

    private static Guid GetRequiredLabelId(Instruction instruction)
    {
        if (instruction.Operands.Count == 1 && instruction.Operands[0] is Guid labelId)
            return labelId;

        return Thrower.InvalidOpEx<Guid>(
            $"AIR instruction '{instruction.UOpCode}' requires exactly one Guid label operand.");
    }

    private static void PushNull(GroboIL il, Type declaredType)
    {
        if (!declaredType.IsValueType)
        {
            il.Ldnull();
            return;
        }

        if (Nullable.GetUnderlyingType(declaredType) is null)
        {
            Thrower.InvalidOpEx(
                $"AIR null constant cannot use non-nullable value type '{declaredType}'.");
        }

        var local = il.DeclareLocal(declaredType);
        il.Ldloca(local);
        il.Initobj(declaredType);
        il.Ldloc(local);
    }

    private static void PushValue(CompilationContext context, object value, Type declaredType)
    {
        var type = declaredType;
        var loadMethod = typeof(ArtifactConstantPool)
            .GetMethod(nameof(ArtifactConstantPool.GetValue))
            .NotNull()
            .MakeGenericMethod(type);
        var index = context.AddConstant(value);

        Thrower.AssertAlways(context.ConstantPoolArgumentIndex.HasValue, "CIL constant pool argument is required.");
        context.Il.Ldarg(context.ConstantPoolArgumentIndex.Value);
        context.Il.Ldc_I4(index);
        context.Il.Call(loadMethod);
    }
}
