using System.Reflection;
using BasicCore.Builtins;
using BasicCore.Capabilities;
using BasicCore.Contracts;
using IntermediateRepresentationAbstractions;

namespace BasicCore.Core;

/// <summary>
///     Reads C# call intrinsics through the canonical intrinsic model.
/// </summary>
public static class CSharpCallIntrinsicReader
{
    public static bool TryGetCallOperand(Instruction instruction, out object? operand)
    {
        operand = null;
        if (!IntrinsicInstructionView.TryRead(instruction, out var intrinsic))
            return false;
        if (!string.Equals(intrinsic.CapabilityId, IntrinsicCapabilityIds.CallCSharp, StringComparison.Ordinal) ||
            intrinsic.Invocation.DataOperands.Count == 0)
            return false;
        operand = intrinsic.Invocation.DataOperands[0];
        return true;
    }

    public static bool TryGetCallMethod(Instruction instruction, out MethodInfo method)
    {
        method = default!;
        if (!TryGetCallOperand(instruction, out var operand))
            return false;
        method = operand switch
        {
            MethodInfo methodInfo => methodInfo,
            IManagedCallDescriptor descriptor => descriptor.Method,
            _ => null!
        };
        return method != null;
    }

    public static bool TryGetCallDescriptor(Instruction instruction, out IManagedCallDescriptor descriptor)
    {
        descriptor = default!;
        if (!TryGetCallOperand(instruction, out var operand) || operand is not IManagedCallDescriptor candidate)
            return false;
        descriptor = candidate;
        return true;
    }
}
