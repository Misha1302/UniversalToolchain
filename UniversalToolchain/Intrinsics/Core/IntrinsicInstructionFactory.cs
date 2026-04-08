using IntermediateRepresentationAbstractions;
using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core;

public static class IntrinsicInstructionFactory
{
    public static Instruction Create(IntrinsicInvocation invocation)
    {
        if (invocation == null)
            Thrower.ArgumentNull(nameof(invocation));

        return new Instruction(UOpCode.Intrinsic, [invocation]);
    }
}
