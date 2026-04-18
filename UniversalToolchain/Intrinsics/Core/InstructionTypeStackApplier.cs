using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core;

public static class InstructionTypeStackApplier
{
    public static void Apply(
        IReadOnlyList<Instruction> instructions,
        List<Type> stack,
        IInstructionIntrinsicReader intrinsicReader,
        IIntrinsicTypeStackProcessor processor)
    {
        instructions = instructions.ArgNotNull();

        stack = stack.ArgNotNull();

        intrinsicReader = intrinsicReader.ArgNotNull();

        processor = processor.ArgNotNull();

        foreach (var instruction in instructions)
        {
            switch (instruction.UOpCode)
            {
                case UOpCode.Nop:
                    break;

                case UOpCode.Push:
                    stack.Add(instruction.Operands[0].GetType());
                    break;

                case UOpCode.Drop:
                    stack.RemoveAt(stack.Count - 1);
                    break;

                case UOpCode.Jmp:
                    break;

                case UOpCode.JmpIf:
                    stack.RemoveAt(stack.Count - 1);
                    break;

                case UOpCode.JmpIfNot:
                    stack.RemoveAt(stack.Count - 1);
                    break;

                case UOpCode.Label:
                    break;

                case UOpCode.Annotate:
                    break;

                case UOpCode.Intrinsic:
                    if (!intrinsicReader.TryRead(instruction, out var invocation))
                        Thrower.InvalidOpEx($"Unable to read intrinsic invocation from instruction {instruction}");

                    processor.Process(invocation, stack);
                    break;

                default:
                    Thrower.InvalidOpEx($"Unknown opcode {instruction.UOpCode}");
                    break;
            }
        }
    }
}