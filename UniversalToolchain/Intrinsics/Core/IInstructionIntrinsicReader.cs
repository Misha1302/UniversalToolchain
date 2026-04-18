namespace BasicCore.Core;

/// <summary>
///     Reads a structured intrinsic invocation from an instruction.
/// </summary>
public interface IInstructionIntrinsicReader
{
    bool TryRead(Instruction instruction, out IntrinsicInvocation invocation);
}