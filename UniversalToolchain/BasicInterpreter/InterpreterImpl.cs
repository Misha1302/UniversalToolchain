using System.Reflection;
using BasicCore.ExecutorWrapper;
using UniversalIntermediateRepresentation;

namespace BasicInterpreter;

public class InterpreterImpl : IExecutor<AbstractIR>
{
    public object? Execute(AbstractIR air)
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

        if (state.ValueStack.Count == 0)
            return null!;

        return state.ValueStack.Count != 0 ? state.ValueStack.Peek().Data : null;
    }

    private void ExecuteInstruction(Instruction instruction, InterpreterState state)
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
        var parametersTypes = GenericTypeResolver.GetParameterTypes(method, state.ValueStack.Take(method.GetParameters().Length).Select(x => x.Data.GetType()).ToList());

        // Извлекаем аргументы из стека
        var args = new object[parametersTypes.Count];
        var argsTypes = new Type[parametersTypes.Count];
        for (var i = parametersTypes.Count - 1; i >= 0; i--)
        {
            if (state.ValueStack.Count == 0)
                throw new InvalidOperationException("Not enough arguments on stack");

            var value = state.ValueStack.Pop();
            args[i] = ConvertValue(value, parametersTypes[i]);
            argsTypes[i] = args[i].GetType();
        }

        method = GenericTypeResolver.MakeGenericMethod(method, argsTypes);

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

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (data == null)
            return null!;

        if (data.GetType() == targetType)
            return data;

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