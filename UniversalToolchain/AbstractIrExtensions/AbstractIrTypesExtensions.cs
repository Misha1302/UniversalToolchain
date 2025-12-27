using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using ListExtensions;

namespace AbstractIrExtensions;

public static class AbstractIrTypesExtensions
{
    public static void ManipulateTypesStack(
        this Instruction instructions,
        List<Type> stack,
        Action<Instruction, List<Type>> processIntrinsic
    )
    {
        ((IReadOnlyList<Instruction>)[instructions]).ManipulateTypesStack(stack, processIntrinsic);
    }

    public static void ManipulateTypesStack(
        this IReadOnlyList<Instruction> instructions,
        List<Type> stack,
        Action<Instruction, List<Type>> processIntrinsic
    )
    {
        foreach (var instruction in instructions)
        {
            // ReSharper disable RedundantJumpStatement
            // ReSharper disable RedundantIfElseBlock

            var t = instruction.UOpCode;
            if (t == UOpCode.Nop) continue;
            else if (t == UOpCode.Push) stack.Push(instruction.Operands[0].GetType());
            else if (t == UOpCode.Drop) stack.Pop();
            else if (t == UOpCode.Jmp) continue;
            else if (t == UOpCode.JmpIf) stack.Pop();
            else if (t == UOpCode.JmpIfNot) stack.Pop();
            else if (t == UOpCode.Label) continue;
            else if (t == UOpCode.Annotate) continue;
            else if (t == UOpCode.Intrinsic) processIntrinsic(instruction, stack);
            else Thrower.InvalidOpEx($"Unknown opcode {t}");
        }
    }
}