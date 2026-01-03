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
}