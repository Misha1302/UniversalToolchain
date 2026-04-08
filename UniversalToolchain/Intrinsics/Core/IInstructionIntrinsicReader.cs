using IntermediateRepresentationAbstractions;
using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core;

/// <summary>
/// Reads a structured intrinsic invocation from an instruction.
/// </summary>
public interface IInstructionIntrinsicReader
{
    bool TryRead(Instruction instruction, out IntrinsicInvocation invocation);
}
