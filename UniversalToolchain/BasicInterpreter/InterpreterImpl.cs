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

        if (state.ValueStack.Count == 0)
            return null!;

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
        var parametersTypes =
            GenericTypeResolver.GetParameterTypes(method, state.ValueStack.Take(method.GetParameters().Length)
                .Select(x => x.GetType()).ToList());

        // Extract arguments from the stack
        var args = new object[parametersTypes.Count];
        var argsTypes = new Type[parametersTypes.Count];
        for (var i = parametersTypes.Count - 1; i >= 0; i--)
        {
            if (state.ValueStack.Count == 0)
                throw new InvalidOperationException("Not enough arguments on stack");

            var value = state.ValueStack.Pop();
            args[i] = value;
            argsTypes[i] = args[i].GetType();
        }

        method = GenericTypeResolver.MakeGenericMethod(method, argsTypes);

        // Call the method
        object result;
        if (method.IsStatic)
        {
            result = method.Invoke(null, args) ?? new object();
        }
        else
        {
            // For non-static methods, an instance is required
            if (state.ValueStack.Count == 0)
                throw new InvalidOperationException("No instance on stack for instance method");

            var instanceValue = state.ValueStack.Pop();
            var instance = instanceValue;
            result = method.Invoke(instance, args) ?? new object();
        }

        // If the method returns a value, push it onto the stack
        if (method.ReturnType != typeof(void))
        {
            state.ValueStack.Push(result);
        }
    }


    private void ExecuteCSharpConstructor(Instruction instruction, InterpreterState state)
    {
        var ctor = instruction.Operands[1].Get<ConstructorInfo>();
        var parameters = ctor.GetParameters();

        // Extract arguments from the stack
        var args = new object[parameters.Length];
        for (var i = parameters.Length - 1; i >= 0; i--)
        {
            if (state.ValueStack.Count == 0)
                throw new InvalidOperationException("Not enough arguments on stack");

            var value = state.ValueStack.Pop();
            args[i] = value;
        }

        // Create an instance
        var instance = ctor.Invoke(args);

        // Push the instance onto the stack
        state.ValueStack.Push(instance);
    }
}