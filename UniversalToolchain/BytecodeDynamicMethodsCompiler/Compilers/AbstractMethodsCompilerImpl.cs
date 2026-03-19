namespace BytecodeDynamicMethodsCompiler.Compilers;

public class AbstractMethodsCompilerImpl : IAbstractIrCompiler<DynamicMethod>
{
    private readonly AbstractMethodsIntrinsicCompiler _intrinsicCompiler = new();

    public IReadOnlyList<string> SupportedIntrinsics => _intrinsicCompiler.SupportedIntrinsics;

    public DynamicMethod Compile(IAbstractIR air, CompilationInput input)
    {
        var returnType = GetReturnType(air);
        var argsTypes = input.ExternalBindings.Select(x => x.Type).ToArray();
        var externalSlots = input.ExternalBindings
            .Select((binding, slot) => new { binding.Name, Slot = slot })
            .ToDictionary(x => x.Name, x => x.Slot);
        var method = new DynamicMethod("main", returnType, argsTypes);
        using var il = new GroboIL(method);

        var context = new CompilationContext(il, externalSlots);
        InitializeLabels(context, air);

        var typesStack = new List<Type>();
        var labelStacks = new Dictionary<Guid, List<Type>>();

        foreach (var instruction in air.Instructions)
            CompileInstruction(context, instruction, typesStack, labelStacks);

        EmitMethodReturn(il, returnType, typesStack);
        return method;
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

    private static Type GetReturnType(IAbstractIR air)
    {
        var stack = new List<Type>();
        var labelStacks = new Dictionary<Guid, List<Type>>();

        foreach (var instruction in air.Instructions)
        {
            if (instruction.UOpCode == UOpCode.Label)
            {
                var labelId = instruction.Operands[0].Get<Guid>();
                if (labelStacks.TryGetValue(labelId, out var saved))
                {
                    stack.Clear();
                    stack.AddRange(saved);
                }
            }

            instruction.ManipulateTypesStack(stack, AirTypes.ProcessTypesIntrinsic);
            ProcessBranchingStack(instruction, stack, labelStacks);
        }

        return stack.Count > 0 ? stack[^1] : typeof(void);
    }

    private static void ProcessBranchingStack(
        Instruction instruction,
        List<Type> stack,
        Dictionary<Guid, List<Type>> labelStacks
    )
    {
        if (!instruction.UOpCode.IsAnyJump())
            return;

        var labelId = instruction.Operands[0].Get<Guid>();
        labelStacks[labelId] = new List<Type>(stack);
    }

    private static void InitializeLabels(CompilationContext context, IAbstractIR bytecode)
    {
        var typesStack = new List<Type>();
        foreach (var instruction in bytecode.Instructions)
        {
            if (instruction.UOpCode == UOpCode.Label)
            {
                var id = instruction.Operands[0].Get<Guid>();
                context.InstructionLabels[id] = context.Il.DefineLabel($"Instruction {id}");
            }

            instruction.ManipulateTypesStack(typesStack, AirTypes.ProcessTypesIntrinsic);
        }
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
                PushValue(context.Il, value);
                stack.Push(value.GetType());
                break;
            }
            case UOpCode.Drop:
                context.Il.Pop();
                stack.Pop();
                break;
            case UOpCode.Jmp:
                HandleJump(context, instruction, stack, labelStacks, static (il, label) => il.Br(label));
                break;
            case UOpCode.JmpIf:
                stack.Pop();
                HandleJump(context, instruction, stack, labelStacks, static (il, label) => il.Brtrue(label));
                break;
            case UOpCode.JmpIfNot:
                stack.Pop();
                HandleJump(context, instruction, stack, labelStacks, static (il, label) => il.Brfalse(label));
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
        List<Type> stack,
        Dictionary<Guid, List<Type>> labelStacks,
        Action<GroboIL, GroboIL.Label> branchAction
    )
    {
        var labelId = instruction.Operands[0].Get<Guid>();
        labelStacks[labelId] = new List<Type>(stack);
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

    private static void PushValue(GroboIL il, object value)
    {
        var type = value.GetType();
        var constants = typeof(GlobalExecutionConstants<>).MakeGenericType(type);
        var loadMethod = constants.GetMethod(nameof(GlobalExecutionConstants<>.GetValue)).NotNull();
        var addMethod = constants.GetMethod(nameof(GlobalExecutionConstants<>.AddValue)).NotNull();

        var ind = addMethod.Invoke(null, [value]).NotNull().Get<int>();

        il.Ldc_I4(ind);
        il.Call(loadMethod);
    }

    private static class GlobalExecutionConstants<T>
    {
        private static readonly List<T> _values = [];

        public static int AddValue(T value)
        {
            _values.Add(value);
            return _values.Count - 1;
        }

        public static T GetValue(int index) => _values[index];
    }
}