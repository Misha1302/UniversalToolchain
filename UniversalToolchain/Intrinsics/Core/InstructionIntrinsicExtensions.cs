namespace BasicCore.Core;

public static class InstructionIntrinsicExtensions
{
    public static bool TryGetTypedIntrinsicInvocation(this Instruction instruction, out IntrinsicInvocation invocation)
    {
        invocation = default!;

        if (instruction == null)
            return false;

        if (instruction.UOpCode != UOpCode.Intrinsic)
            return false;

        if (instruction.Operands.Count != 1)
            return false;

        if (instruction.Operands[0] is not IntrinsicInvocation typedInvocation)
            return false;

        invocation = typedInvocation;
        return true;
    }

    public static bool IsTypedIntrinsicInvocation(this Instruction instruction) => instruction.TryGetTypedIntrinsicInvocation(out _);
}