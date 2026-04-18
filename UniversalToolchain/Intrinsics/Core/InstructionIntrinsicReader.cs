using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Legacy;

namespace UniversalToolchain.Intrinsics.Core;

/// <summary>
///     Reads typed intrinsic payloads first and falls back to legacy decoding when needed.
/// </summary>
public sealed class InstructionIntrinsicReader : IInstructionIntrinsicReader
{
    private readonly ILegacyIntrinsicDecoder _legacyIntrinsicDecoder;

    public InstructionIntrinsicReader(ILegacyIntrinsicDecoder legacyIntrinsicDecoder)
    {
        legacyIntrinsicDecoder = legacyIntrinsicDecoder.ArgNotNull();

        _legacyIntrinsicDecoder = legacyIntrinsicDecoder;
    }

    public bool TryRead(Instruction instruction, out IntrinsicInvocation invocation)
    {
        if (instruction.TryGetTypedIntrinsicInvocation(out invocation))
            return true;

        return _legacyIntrinsicDecoder.TryDecode(instruction, out invocation);
    }
}