using System.Reflection;
using BasicCore.ExecutorWrapper;
using DotnetHelper;
using IntermediateRepresentationAbstractions;
using ObjectExtensions;

namespace BasicInterpreter;

public class InterpreterImpl : IExecutor<IAbstractIR>
{
    public object? Execute(IAbstractIR air)
    {
        var state = new InterpreterState();
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
            state.InstructionPointer++;
            ExecuteInstruction(instruction, state);
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
                throw new InvalidOperationException($"Unknown opcode: {instruction.UOpCode}");
        }
    }

    private void ExecuteIntrinsic(Instruction instruction, InterpreterState state)
    {
        var intrinsicName = instruction.Operands[0].Get<string>();
        if (intrinsicName == "call C#")
        {
            ExecuteCSharpCall(instruction, state);
        }
        else if (intrinsicName == "call C# ctor")
        {
            ExecuteCSharpConstructor(instruction, state);
        }
        else if (intrinsicName is "store_local" or "load_local" or "load_local_ref")
        {
            ExecuteLocalVariableIntrinsic(instruction, state, intrinsicName);
        }
        else if (intrinsicName is "load_i32" or "load_i64" or "load_f32" or "load_f64")
        {
            ExecuteLoadNativeNumber(instruction, state, intrinsicName);
        }
        else
        {
            throw new InvalidOperationException($"Unknown intrinsic: {intrinsicName}");
        }
    }

    private void ExecuteCSharpCall(Instruction instruction, InterpreterState state)
    {
        var method = instruction.Operands[1].Get<MethodInfo>();
        
        // Получаем типы аргументов из стека
        var parameters = method.GetParameters();
        var args = new object[parameters.Length];
        var argsTypes = new Type[parameters.Length];
        
        // Собираем аргументы в обратном порядке (последний аргумент первым в стеке)
        for (var i = parameters.Length - 1; i >= 0; i--)
        {
            if (state.ValueStack.Count == 0)
                throw new InvalidOperationException("Not enough arguments on stack");
                
            var value = state.ValueStack.Pop();
            args[i] = value;
            argsTypes[i] = value.GetType();
        }
        
        // Используем ту же логику, что и в компиляторе
        var stackTypes = argsTypes.AsReadOnly().Reverse().ToList(); // Восстанавливаем порядок как в стеках компилятора
        var targetTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();
        
        // Приводим аргументы к нужным типам, если необходимо
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] != null && args[i].GetType() != targetTypes[i])
            {
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
                throw new InvalidOperationException("No instance on stack for instance method");
                
            var instance = state.ValueStack.Pop();
            result = method.Invoke(instance, args) ?? new object();
        }
        
        // Если метод возвращает значение, кладем его в стек
        if (method.ReturnType != typeof(void))
        {
            state.ValueStack.Push(result);
        }
    }

    private void ExecuteCSharpConstructor(Instruction instruction, InterpreterState state)
    {
        var ctor = instruction.Operands[1].Get<ConstructorInfo>();
        var parameters = ctor.GetParameters();
        
        // Собираем аргументы в обратном порядке
        var args = new object[parameters.Length];
        var argsTypes = new Type[parameters.Length];
        
        for (var i = parameters.Length - 1; i >= 0; i--)
        {
            if (state.ValueStack.Count == 0)
                throw new InvalidOperationException("Not enough arguments on stack");
                
            var value = state.ValueStack.Pop();
            args[i] = value;
            argsTypes[i] = value.GetType();
        }
        
        // Создаем экземпляр
        var instance = ctor.Invoke(args);
        
        // Кладем экземпляр в стек
        state.ValueStack.Push(instance);
    }

    private void ExecuteLocalVariableIntrinsic(Instruction instruction, InterpreterState state, string intrinsicName)
    {
        var varName = instruction.Operands[1].Get<string>();
        var varType = instruction.Operands[2].Get<Type>();
        
        switch (intrinsicName)
        {
            case "store_local":
                // Значение должно быть на вершине стека
                if (state.ValueStack.Count == 0)
                    throw new InvalidOperationException("No value on stack to store");
                    
                var value = state.ValueStack.Pop();
                state.Locals[Guid.NewGuid()] = value; // Упрощенная реализация
                break;
                
            case "load_local":
                // Ищем переменную по имени (упрощенная реализация)
                var localEntry = state.Locals.FirstOrDefault(kv => kv.Key.ToString().Contains(varName));
                if (localEntry.Key == Guid.Empty)
                {
                    // Если переменная не найдена, создаем со значением по умолчанию
                    state.ValueStack.Push(GetDefaultValue(varType));
                }
                else
                {
                    state.ValueStack.Push(localEntry.Value);
                }
                break;
                
            case "load_local_ref":
                // В интерпретаторе работа с ссылками сложнее, используем упрощенный вариант
                var localEntryRef = state.Locals.FirstOrDefault(kv => kv.Key.ToString().Contains(varName));
                if (localEntryRef.Key == Guid.Empty)
                {
                    // Создаем новую переменную со значением по умолчанию
                    var newId = Guid.NewGuid();
                    var defaultValue = GetDefaultValue(varType);
                    state.Locals[newId] = defaultValue;
                    state.ValueStack.Push(new VariableReferenceWrapper(newId, state.Locals));
                }
                else
                {
                    state.ValueStack.Push(new VariableReferenceWrapper(localEntryRef.Key, state.Locals));
                }
                break;
        }
    }
    
    private void ExecuteLoadNativeNumber(Instruction instruction, InterpreterState state, string intrinsicName)
    {
        var arg = instruction.Operands[1];
        
        switch (intrinsicName)
        {
            case "load_i32":
                state.ValueStack.Push(arg.Get<int>());
                break;
            case "load_i64":
                state.ValueStack.Push(arg.Get<long>());
                break;
            case "load_f32":
                state.ValueStack.Push(arg.Get<float>());
                break;
            case "load_f64":
                state.ValueStack.Push(arg.Get<double>());
                break;
            default:
                throw new InvalidOperationException($"Unknown native number loading {intrinsicName}");
        }
    }
    
    private object GetDefaultValue(Type type)
    {
        if (type.IsValueType)
            return Activator.CreateInstance(type) ?? new object();
        return null!;
    }
    
    // Вспомогательный класс для работы с ссылками на переменные в интерпретаторе
    private class VariableReferenceWrapper
    {
        private readonly Guid _variableId;
        private readonly Dictionary<Guid, object> _locals;
        
        public VariableReferenceWrapper(Guid variableId, Dictionary<Guid, object> locals)
        {
            _variableId = variableId;
            _locals = locals;
        }
        
        public object GetValue() => _locals[_variableId];
        
        public void SetValue(object value) => _locals[_variableId] = value;
    }
}