namespace BasicInterpreter;

public class InterpreterImpl : IExecutor<IAbstractIR>
{
    public object? Execute(IAbstractIR air, IExecutionEnvironment environment)
    {
        var state = new InterpreterState { ExecutionEnvironment = environment };
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

    private static object? ResolveVariableValue(string key, InterpreterState state)
    {
        if (state.ExternalSlotsByName.TryGetValue(key, out var existingSlot))
            return state.ExecutionEnvironment?.GetExternalValue(existingSlot);

        if (state.LocalVariables.Contains(key))
            return null;

        if (state.ExecutionEnvironment == null)
            return null;

        var slot = state.ExternalSlotsByName.Count;
        try
        {
            var value = state.ExecutionEnvironment.GetExternalValue(slot);
            state.ExternalSlotsByName[key] = slot;
            return value;
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private void ExecuteCSharpCall(Instruction instruction, InterpreterState state)
    {
        var method = instruction.Operands[1].Get<MethodInfo>();

        // Получаем типы аргументов из стека
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];
        var argsTypes = new Type[parameters.Length];

        // Собираем аргументы в обратном порядке (последний аргумент первым в стеке)
        for (var i = parameters.Length - 1; i >= 0; i--)
        {
            if (state.ValueStack.Count == 0)
                Thrower.InvalidOpEx("Cannot call method: not enough arguments on the interpreter stack.");

            var value = state.ValueStack.Pop();
            args[i] = value;
            argsTypes[i] = value?.GetType() ?? typeof(object);
        }

        if (IsVariablesContainerGet(method) && args[0] is string key)
        {
            var value = ResolveVariableValue(key, state);
            if (value != null || state.ExternalSlotsByName.ContainsKey(key))
            {
                state.ValueStack.Push(value!);
                return;
            }
        }

        if (method.Name == "Set" && method.GetParameters().Length == 2 && args[0] is string localVariable)
            state.LocalVariables.Add(localVariable);

        // Use the same logic as in compiler
        var stackTypes = argsTypes.AsReadOnly().ToList();
        var targetTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();

        // Приводим аргументы к нужным типам, если необходимо
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
                // Если не удалось преобразовать, оставляем как есть
                // Это может привести к исключению при вызове, что корректно
            }
        }

        // Создаем конкретный generic-метод, если нужно (используем ту же логику, что и в компиляторе)
        method = GenericTypeResolver.MakeGenericMethod(method, targetTypes.ToArray());

        // Вызов метода
        object result;
        if (method.IsStatic)
        {
            result = method.Invoke(null, args) ?? new object();
        }
        else
        {
            // Для нестатических методов, экземпляр должен быть в стеке перед аргументами
            if (state.ValueStack.Count == 0)
                Thrower.InvalidOpEx("Cannot call instance method: object instance is missing on the interpreter stack.");

            var instance = state.ValueStack.Pop();
            result = method.Invoke(instance, args) ?? new object();
        }

        // Если метод возвращает значение, кладем его в стек
        if (method.ReturnType != typeof(void))
            state.ValueStack.Push(result);
    }

    private void ExecuteCSharpConstructor(Instruction instruction, InterpreterState state)
    {
        var ctor = instruction.Operands[1].Get<ConstructorInfo>();
        var parameters = ctor.GetParameters();

        // Собираем аргументы в обратном порядке
        var args = new object[parameters.Length];

        for (var i = parameters.Length - 1; i >= 0; i--)
        {
            if (state.ValueStack.Count == 0)
                Thrower.InvalidOpEx("Cannot call constructor: not enough arguments on the interpreter stack.");

            args[i] = state.ValueStack.Pop();
        }

        // Создаем экземпляр
        var instance = ctor.Invoke(args);

        // Кладем экземпляр в стек
        state.ValueStack.Push(instance);
    }
}
