using IntermediateRepresentationAbstractions;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Legacy;

namespace UniversalToolchain.Intrinsics.Core;

public static class InstructionTypeStackApplier
{
    public static void Apply(
        IReadOnlyList<Instruction> instructions,
        List<Type> stack,
        ILegacyIntrinsicDecoder decoder,
        IIntrinsicTypeStackProcessor processor)
    {
        if (instructions == null)
            Thrower.ArgumentNull(nameof(instructions));

        if (stack == null)
            Thrower.ArgumentNull(nameof(stack));

        if (decoder == null)
            Thrower.ArgumentNull(nameof(decoder));

        if (processor == null)
            Thrower.ArgumentNull(nameof(processor));

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
                    if (instruction.TryGetTypedIntrinsicInvocation(out var typedInvocation))
                    {
                        processor.Process(typedInvocation, stack);
                        break;
                    }

                    if (!decoder.TryDecode(instruction, out var invocation))
                        Thrower.InvalidOpEx($"Unknown intrinsic {instruction}");

                    processor.Process(invocation, stack);
                    break;

                default:
                    Thrower.InvalidOpEx($"Unknown opcode {instruction.UOpCode}");
                    break;
            }
        }
    }
}
