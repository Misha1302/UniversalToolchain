namespace BasicCore.Core;

/// <summary>
/// Reads the canonical structured intrinsic payload. String-shaped intrinsic instructions are rejected.
/// </summary>
public sealed class InstructionIntrinsicReader : IInstructionIntrinsicReader
{
    public bool TryRead(Instruction instruction, out IntrinsicInvocation invocation) =>
        instruction.TryGetTypedIntrinsicInvocation(out invocation);
}
