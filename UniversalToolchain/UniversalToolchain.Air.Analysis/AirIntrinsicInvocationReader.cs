using BasicCore.Capabilities;
using BasicCore.Contracts;
using BasicCore.Core;
using IntermediateRepresentationAbstractions;

namespace UniversalToolchain.Air.Analysis;

public static class AirIntrinsicInvocationReader
{
    public static bool TryRead(
        Instruction instruction,
        out IntrinsicInvocation invocation,
        out string intrinsicId,
        out string? diagnostic)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        invocation = default!;
        intrinsicId = string.Empty;
        diagnostic = null;

        if (!instruction.TryGetTypedIntrinsicInvocation(out invocation))
        {
            diagnostic = "AIR Intrinsic must contain exactly one structured IntrinsicInvocation payload.";
            return false;
        }

        if (!IntrinsicCapabilityNameEncoder.TryEncode(invocation, out intrinsicId))
        {
            diagnostic = $"AIR intrinsic '{invocation.Symbol}' has no stable capability identifier.";
            return false;
        }

        return true;
    }
}
