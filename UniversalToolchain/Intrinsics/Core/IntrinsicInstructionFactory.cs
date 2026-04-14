using IntermediateRepresentationAbstractions;
using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core;

public static class IntrinsicInstructionFactory
{
    public static Instruction Create(IntrinsicInvocation invocation)
    {
        invocation = invocation.ArgNotNull();

        return new Instruction(UOpCode.Intrinsic, [invocation]);
    }
}
