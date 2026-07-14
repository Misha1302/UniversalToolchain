namespace BasicInterpreter;

public class InterpreterImpl : IExecutor<IAbstractIR>
{
    private readonly InterpreterIntrinsicExecutor _intrinsicExecutor = new();

    public object? Execute(IAbstractIR air, IExecutionEnvironment environment)
    {
        var state = new InterpreterState
        {
            ExecutionEnvironment = environment
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
                ToolchainThrower.Runtime(
                    $"Error executing instruction '{instruction.UOpCode}' at pc={programCounter}, stack={state.EvaluationStackCount}. {ex.Message}",
                    ex
                );
            }
        }

        return state.EvaluationStackCount switch
        {
            0 => null,
            1 => state.PeekEvaluationValue().Value,
            _ => throw new RuntimeExecutionException(
                $"AIR execution finished with {state.EvaluationStackCount} evaluation-stack values; expected zero or one.")
        };
    }

    private void ExecuteInstruction(Instruction instruction, InterpreterState state)
    {
        switch (instruction.UOpCode)
        {
            case UOpCode.Nop:
                break;
            case UOpCode.Push:
                var operand = GetRequiredOperand(instruction, 0);
                state.PushEvaluationValue(
                    AirPushOperand.GetValue(operand),
                    AirPushOperand.GetDeclaredType(operand));
                break;
            case UOpCode.Drop:
                _ = PopRequired(state, instruction.UOpCode);
                break;
            case UOpCode.Jmp:
                var labelId = GetRequiredOperand(instruction, 0).NotNull().Get<Guid>();
                state.InstructionPointer = state.GetLabelPosition(labelId);
                break;
            case UOpCode.JmpIf:
                var condition = PopRequired(state, instruction.UOpCode).Value.NotNull().Get<bool>();
                if (condition)
                {
                    labelId = GetRequiredOperand(instruction, 0).NotNull().Get<Guid>();
                    state.InstructionPointer = state.GetLabelPosition(labelId);
                }
                break;
            case UOpCode.JmpIfNot:
                condition = PopRequired(state, instruction.UOpCode).Value.NotNull().Get<bool>();
                if (!condition)
                {
                    labelId = GetRequiredOperand(instruction, 0).NotNull().Get<Guid>();
                    state.InstructionPointer = state.GetLabelPosition(labelId);
                }
                break;
            case UOpCode.Label:
                break;
            case UOpCode.Annotate:
                break;
            case UOpCode.Intrinsic:
                ExecuteIntrinsic(instruction, state);
                break;
            default:
                CompilerAssert.Unreachable($"Unknown instruction '{instruction.UOpCode}'.");
                break;
        }
    }


    private static object? GetRequiredOperand(Instruction instruction, int index)
    {
        if (instruction.Operands.Count <= index)
        {
            return Thrower.InvalidOpEx<object?>(
                $"Instruction '{instruction.UOpCode}' requires operand at index {index}.");
        }

        return instruction.Operands[index];
    }

    private static InterpreterStackValue PopRequired(InterpreterState state, UOpCode opcode)
    {
        if (state.EvaluationStackCount == 0)
            return Thrower.InvalidOpEx<InterpreterStackValue>($"Instruction '{opcode}' requires a value on the evaluation stack.");
        return state.PopEvaluationValue();
    }

    private void ExecuteIntrinsic(Instruction instruction, InterpreterState state)
    {
        _intrinsicExecutor.Execute(instruction, state);
    }
}