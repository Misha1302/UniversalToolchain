namespace BasicInterpreter;

public class InterpreterImpl : IExecutor<IAbstractIR>
{
    public object? Execute(IAbstractIR air, IExecutionEnvironment environment)
    {
        var state = new InterpreterState
        {
            ExecutionEnvironment = environment,
            ExternalBindingsLayout = (environment as IExternalBindingsLayoutProvider)?.ExternalBindingsLayout
        };
        state.BuildLabelPositions(air.Instructions);
        return ExecuteInstructions(air.Instructions, state);
    }

    private object? ExecuteInstructions(
        IReadOnlyList<Instruction> instructions,
        InterpreterState state)
    {
        while (state.InstructionPointer < instructions.Count)
        {
            var instruction = instructions[state.InstructionPointer];
            var programCounter = state.InstructionPointer;
            state.InstructionPointer++;
            try
            {
                ExecuteInstruction(instruction, state);
            }
            catch (Exception ex) when (ex is not RuntimeExecutionException)
            {
                WistThrower.Runtime(
                    $"Error executing instruction '{instruction.UOpCode}' at pc={programCounter}, stack={state.ValueStack.Count}. {ex.Message}",
                    ex
                );
            }
        }

        if (state.ValueStack.Count == 0) return null!;
        return state.ValueStack.Count != 0 ? state.ValueStack.Peek() : null;
    }

    private void ExecuteInstruction(Instruction instruction, InterpreterState state)
    {
        switch (instruction.UOpCode)
        {
            case UOpCode.Nop:
                break;
            case UOpCode.Push:
                state.ValueStack.Push(instruction.Operands[0]);
                break;
            case UOpCode.Drop:
                if (state.ValueStack.Count > 0)
                    state.ValueStack.Pop();
                break;
            case UOpCode.Jmp:
                var labelId = instruction.Operands[0].Get<Guid>();
                state.InstructionPointer = state.GetLabelPosition(labelId);
                break;
            case UOpCode.JmpIf:
                if (state.ValueStack.Count > 0)
                {
                    var condition = state.ValueStack.Pop().Get<bool>();
                    if (condition)
                    {
                        labelId = instruction.Operands[0].Get<Guid>();
                        state.InstructionPointer = state.GetLabelPosition(labelId);
                    }
                }
                break;
            case UOpCode.JmpIfNot:
                if (state.ValueStack.Count > 0)
                {
                    var condition = state.ValueStack.Pop().Get<bool>();
                    if (!condition)
                    {
                        labelId = instruction.Operands[0].Get<Guid>();
                        state.InstructionPointer = state.GetLabelPosition(labelId);
                    }
                }
                break;
            case UOpCode.Label:
                // Label - do nothing, just skip
                break;
            case UOpCode.Annotate:
                // Annotation - do nothing
                break;
            case UOpCode.Intrinsic:
                ExecuteIntrinsic(instruction, state);
                break;
            default:
                CompilerAssert.Unreachable($"Unknown instruction '{instruction.UOpCode}'.");
                break;
        }
    }

    private void ExecuteIntrinsic(Instruction instruction, InterpreterState state)
    {
        var intrinsicName = instruction.Operands[0].Get<string>();
        if (intrinsicName == "call C#")
            ExecuteCSharpCall(instruction, state);
        else if (intrinsicName == "call C# ctor")
            ExecuteCSharpConstructor(instruction, state);
        else
            Thrower.InvalidOpEx($"Unknown intrinsic call: {intrinsicName}.");
    }


    private static bool IsVariablesContainerGet(MethodInfo method)
    {
        var declaringType = method.DeclaringType;
        return declaringType != null
               && declaringType.IsGenericType
               && declaringType.GetGenericTypeDefinition().FullName == "SettableGettableModule.Core.VariablesContainer`1"
               && method.Name == "Get"
               && method.GetParameters().Length == 1
               && method.GetParameters()[0].ParameterType == typeof(string);
    }

    private static bool TryResolveDeclaredExternalSlot(
        InterpreterState state,
        object?[] args,
        out int slot)
    {
        slot = default;
        if (args.Length != 1 || args[0] is not string key)
            return false;

        // Only declared layout metadata can mark a symbol as external.
        // Runtime call observation must not infer local/external symbol class.
        return state.ExternalBindingsLayout?.SlotsByName.TryGetValue(key, out slot) == true;
    }

    private void ExecuteCSharpCall(Instruction instruction, InterpreterState state)
    {
        var method = instruction.Operands[1].Get<MethodInfo>();

        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];
        var argsTypes = new Type[parameters.Length];

        for (var i = parameters.Length - 1; i >= 0; i--)
        {
            if (state.ValueStack.Count == 0)
                Thrower.InvalidOpEx("Cannot call method: not enough arguments on the interpreter stack.");

            var value = state.ValueStack.Pop();
            args[i] = value;
            argsTypes[i] = value?.GetType() ?? typeof(object);
        }

        if (IsVariablesContainerGet(method)
            && TryResolveDeclaredExternalSlot(state, args, out var slot))
        {
            var value = state.ExecutionEnvironment.NotNull().GetExternalValue(slot);
            state.ValueStack.Push(value!);
            return;
        }

        // Use the same logic as in compiler.
        var stackTypes = argsTypes.AsReadOnly().ToList();
        var targetTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i]?.GetType() == targetTypes[i])
                continue;

            try
            {
                args[i] = Convert.ChangeType(args[i], targetTypes[i]);
            }
            catch
            {
                // If conversion fails, keep the original value.
                // Method invocation will raise a meaningful exception if needed.
            }
        }

        // Create closed generic method when needed, using the same logic as compiler backend.
        method = GenericTypeResolver.MakeGenericMethod(method, targetTypes.ToArray());

        object result;
        if (method.IsStatic)
        {
            result = method.Invoke(null, args) ?? new object();
        }
        else
        {
            // For instance methods the target instance must be on stack before arguments.
            if (state.ValueStack.Count == 0)
                Thrower.InvalidOpEx("Cannot call instance method: object instance is missing on the interpreter stack.");

            var instance = state.ValueStack.Pop();
            result = method.Invoke(instance, args) ?? new object();
        }

        if (method.ReturnType != typeof(void))
            state.ValueStack.Push(result);
    }

    private void ExecuteCSharpConstructor(Instruction instruction, InterpreterState state)
    {
        var ctor = instruction.Operands[1].Get<ConstructorInfo>();
        var parameters = ctor.GetParameters();

        var args = new object[parameters.Length];

        for (var i = parameters.Length - 1; i >= 0; i--)
        {
            if (state.ValueStack.Count == 0)
                Thrower.InvalidOpEx("Cannot call constructor: not enough arguments on the interpreter stack.");

            args[i] = state.ValueStack.Pop();
        }

        var instance = ctor.Invoke(args);

        state.ValueStack.Push(instance);
    }
}
