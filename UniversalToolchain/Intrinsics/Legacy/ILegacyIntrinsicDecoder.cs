using IntermediateRepresentationAbstractions;
using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Legacy;

/// <summary>
/// Translates current string-based intrinsic instructions into structured intrinsic invocations.
/// </summary>
public interface ILegacyIntrinsicDecoder
{
    bool TryDecode(Instruction instruction, out IntrinsicInvocation invocation);
}
