// ./BasicInterpreter/InterpreterState.cs


using System.Reflection;
using BasicCore;
using BasicCore.ExecutorWrapper;
using BasicCore.TranslatorWrapper;
using DynamicMethodWrapper;
using UniversalIntermediateRepresentation;

public class InterpreterState
{
    private readonly Dictionary<Guid, int> _labelPositions = new();
    private bool _labelsBuilt;
    public Stack<Value> ValueStack { get; } = new();
    public Dictionary<Guid, Value> Locals { get; } = new();

    public int InstructionPointer { get; set; }

    public void BuildLabelPositions(IReadOnlyList<Instruction> instructions)
    {
        if (_labelsBuilt) return;

        _labelPositions.Clear();
        for (var i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].OpCode == OpCode.Label)
            {
                var labelId = instructions[i].Operands[0].Get<Guid>();
                _labelPositions[labelId] = i;
            }
        }

        _labelsBuilt = true;
    }

    public int GetLabelPosition(Guid labelId)
    {
        if (!_labelPositions.ContainsKey(labelId))
            throw new InvalidOperationException($"Label {labelId} not found");

        return _labelPositions[labelId];
    }
} // ./BasicInterpreter/IRCompilerImpl.cs


public class IRCompilerImpl : IAbstractMethodsCompiler<AbstractIR>
{
    public AbstractIR Compile(Bytecode bytecode)
    {
        var air = new AbstractIR();
        var typesStack = new List<Type>();

        foreach (var instruction in bytecode.Instructions)
        {
            foreach (var op in instruction.Ops)
            {
                foreach (var convertable in op.Value)
                {
                    var context = new IAbstractMethodConvertable.Context(typesStack);
                    var methodIR = convertable.GetAbstractIR(context);

                    air.AppendInstructions(methodIR);

                    // Обновляем стек типов
                    for (var i = 0; i < convertable.ParamsCount; i++)
                        typesStack.RemoveAt(typesStack.Count - 1);

                    var returnType = convertable.GetReturnType(context);
                    if (returnType != typeof(void))
                        typesStack.Add(returnType);
                }
            }
        }

        return air;
    }
} // ./BasicInterpreter/InterpreterImpl.cs


public class InterpreterImpl : IExecutor<AbstractIR>
{
    public object Execute(AbstractIR air)
    {
        var state = new InterpreterState();
        state.BuildLabelPositions(air.Instructions);

        return ExecuteInstructions(air.Instructions, state);
    }

    private object ExecuteInstructions(
        IReadOnlyList<Instruction> instructions,
        InterpreterState state)
    {
        while (state.InstructionPointer < instructions.Count)
        {
            var instruction = instructions[state.InstructionPointer];
            state.InstructionPointer++;

            ExecuteInstruction(instruction, state);
        }

        if (state.ValueStack.Count == 0)
            return null!;

        return state.ValueStack.Peek().Data;
    }

    private void ExecuteInstruction(Instruction instruction, InterpreterState state)
    {
        try
        {
            switch (instruction.OpCode)
            {
                case OpCode.Nop:
                    // Ничего не делаем
                    break;

                case OpCode.Push:
                    state.ValueStack.Push(instruction.Operands[0]);
                    break;

                case OpCode.Drop:
                    if (state.ValueStack.Count > 0)
                        state.ValueStack.Pop();
                    break;

                case OpCode.Jmp:
                    var labelId = instruction.Operands[0].Get<Guid>();
                    state.InstructionPointer = state.GetLabelPosition(labelId);
                    break;

                case OpCode.JmpIf:
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

                case OpCode.JmpIfNot:
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

                case OpCode.Label:
                    // Метка - ничего не делаем, просто пропускаем
                    break;

                case OpCode.StLoc:
                    if (state.ValueStack.Count > 0)
                    {
                        var localId = instruction.Operands[0].Get<Guid>();
                        var value = state.ValueStack.Pop();
                        state.Locals[localId] = value;
                    }
                    break;

                case OpCode.LdLoc:
                    var loadId = instruction.Operands[0].Get<Guid>();
                    if (state.Locals.ContainsKey(loadId))
                    {
                        state.ValueStack.Push(state.Locals[loadId]);
                    }
                    break;

                case OpCode.Annotate:
                    // Аннотация - ничего не делаем
                    break;

                case OpCode.Intrinsic:
                    ExecuteIntrinsic(instruction, state);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown opcode: {instruction.OpCode}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error executing instruction at position {state.InstructionPointer - 1}: {instruction}",
                ex
            );
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
        else
        {
            throw new InvalidOperationException($"Unknown intrinsic: {intrinsicName}");
        }
    }

    private void ExecuteCSharpCall(Instruction instruction, InterpreterState state)
    {
        var method = instruction.Operands[1].Get<MethodInfo>();
        var parametersTypes = GetParameterTypes(method, state.ValueStack.Reverse().Select(x => x.Data.GetType()).ToList());

        // Извлекаем аргументы из стека
        var args = new object[parametersTypes.Count];
        for (var i = parametersTypes.Count - 1; i >= 0; i--)
        {
            if (state.ValueStack.Count == 0)
                throw new InvalidOperationException("Not enough arguments on stack");

            var value = state.ValueStack.Pop();
            args[i] = ConvertValue(value, parametersTypes[i]);
        }

        method = MakeGenericMethod(method, parametersTypes);

        // Вызываем метод
        object result;
        if (method.IsStatic)
        {
            result = method.Invoke(null, args) ?? new object();
        }
        else
        {
            // Для нестатических методов нужен экземпляр
            if (state.ValueStack.Count == 0)
                throw new InvalidOperationException("No instance on stack for instance method");

            var instanceValue = state.ValueStack.Pop();
            var instance = ConvertValue(instanceValue, method.DeclaringType ?? throw new InvalidOperationException("Method has no declaring type"));
            result = method.Invoke(instance, args) ?? new object();
        }

        // Если метод возвращает значение, кладем его в стек
        if (method.ReturnType != typeof(void))
        {
            state.ValueStack.Push(Value.Create(result));
        }
    }


    private static MethodInfo MakeGenericMethod(MethodInfo call, IReadOnlyList<Type> argTypes)
    {
        if (!call.ContainsGenericParameters) return call;

        var genericTypes = call.GetGenericArguments()
            .Select((x, i) => x.FullName == null ? argTypes[i] : x)
            .ToArray();

        return call.GetGenericMethodDefinition().MakeGenericMethod(genericTypes);
    }

    private IReadOnlyList<Type> GetParameterTypes(MethodInfo method, List<Type> stack)
    {
        var types = (List<Type>)[];
        var parameters = method.GetParameters();
        foreach (var parameter in parameters)
        {
            var targetType = parameter.ParameterType.ContainsGenericParameters
                ? MakeGenericType(parameter.ParameterType, stack.TakeLast(parameters.Length).Reverse().ToList())
                : parameter.ParameterType;
            types.Add(targetType);
        }
        return types;
    }


    private static Type MakeGenericType(Type parameterType, List<Type> sourceTypes)
    {
        var gArgs = parameterType.GetGenericArguments();
        if (!parameterType.IsGenericType)
            return sourceTypes[0];

        var genericTypes = gArgs
            .Select((x, i) => x.FullName == null ? sourceTypes[i] : x)
            .ToArray();

        return parameterType.GetGenericTypeDefinition().MakeGenericType(genericTypes);
    }


    private void ExecuteCSharpConstructor(Instruction instruction, InterpreterState state)
    {
        var ctor = instruction.Operands[1].Get<ConstructorInfo>();
        var parameters = ctor.GetParameters();

        // Извлекаем аргументы из стека
        var args = new object[parameters.Length];
        for (var i = parameters.Length - 1; i >= 0; i--)
        {
            if (state.ValueStack.Count == 0)
                throw new InvalidOperationException("Not enough arguments on stack");

            var value = state.ValueStack.Pop();
            args[i] = ConvertValue(value, parameters[i].ParameterType);
        }

        // Создаем экземпляр
        var instance = ctor.Invoke(args);

        // Кладем экземпляр в стек
        state.ValueStack.Push(Value.Create(instance));
    }

    private object ConvertValue(Value value, Type targetType)
    {
        var data = value.Data;
        if (data.GetType() == targetType)
            return data;

        if (data == null)
            return null!;

        if (targetType.IsInstanceOfType(data))
            return data;

        // Попробуем преобразовать типы
        if (targetType == typeof(bool) && data is int intValue)
            return intValue != 0;

        if (targetType == typeof(int) && data is bool boolValue)
            return boolValue ? 1 : 0;

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Value))
            return value;

        // Для числовых типов
        if (IsNumericType(targetType) && IsNumericType(data.GetType()))
        {
            return Convert.ChangeType(data, targetType);
        }

        throw new InvalidOperationException(
            $"Cannot convert value of type {data.GetType()} to {targetType}"
        );
    }

    private bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(double) ||
               type == typeof(float) || type == typeof(decimal) ||
               type == typeof(long) || type == typeof(short);
    }
}