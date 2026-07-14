using BasicCore.Contracts;
using IntermediateRepresentationAbstractions;

namespace BasicCore.Core;

public static class IntrinsicInstructionFactory
{
    public static Instruction Create(IntrinsicInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        return new Instruction(UOpCode.Intrinsic, [invocation]);
    }

    public static Instruction CreateForCapability(string capabilityId, params object?[] dataOperands) =>
        Create(IntrinsicInvocationFactory.ForCapability(capabilityId, dataOperands));
}
