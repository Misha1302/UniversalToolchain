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
        var hasConstantPool = air.Instructions.Any(static x => x.UOpCode == UOpCode.Push);
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
            il.Ret();
            return;
        }

        Thrower.AssertAlways(typesStack.Count > 0, "Expected return value on stack");
        var actualReturnType = typesStack[^1];
        if (actualReturnType.IsValueType && !returnType.IsValueType)
            il.Box(actualReturnType);

        il.Ret();
    }

    private Type GetReturnType(IAbstractIR air)
    {
        var stack = _typeSimulator.Simulate(air.Instructions);
        return stack.Count > 0 ? stack[^1] : typeof(void);
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
                var id = instruction.Operands[0].Get<Guid>();
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
                var value = instruction.Operands[0];
                PushValue(context, value);
                stack.Push(value.GetType());
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
        var labelId = instruction.Operands[0].Get<Guid>();
        branchAction(context.Il, context.InstructionLabels[labelId]);
    }

    private static void MarkLabel(
        CompilationContext context,
        Instruction instruction,
        List<Type> stack,
        Dictionary<Guid, List<Type>> labelStacks
    )
    {
        var labelId = instruction.Operands[0].Get<Guid>();

        if (labelStacks.TryGetValue(labelId, out var savedStack))
        {
            stack.Clear();
            stack.AddRange(savedStack);
        }

        context.Il.MarkLabel(context.InstructionLabels[labelId]);
    }

    private static void PushValue(CompilationContext context, object value)
    {
        var type = value.GetType();
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
