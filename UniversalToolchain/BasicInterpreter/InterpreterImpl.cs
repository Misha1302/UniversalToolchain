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
                    $"Error executing instruction '{instruction.UOpCode}' at pc={programCounter}, stack={state.ValueStack.Count}. {ex.Message}",
                    ex
                );
            }
        }

        if (state.ValueStack.Count == 0)
            return null;

        return state.ValueStack.Peek();
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

    private void ExecuteIntrinsic(Instruction instruction, InterpreterState state)
    {
        _intrinsicExecutor.Execute(instruction, state);
    }
}