using System.Reflection;
using BasicCore.Builtins;
using BasicCore.Contracts;

namespace BasicCore.Core;

/// <summary>
///     Reads C# call intrinsics through the canonical intrinsic model.
///     String spellings such as legacy display names are intentionally isolated in the legacy decoder.
/// </summary>
public static class CSharpCallIntrinsicReader
{
    public static bool TryGetCallOperand(Instruction instruction, out object? operand)
    {
        operand = null;

        if (!BuiltinIntrinsicInstruction.TryGetInvocation(instruction, out var invocation))
            return false;

        if (invocation.Symbol != BuiltinIntrinsicSymbols.Core.CallCSharp || invocation.DataOperands.Count == 0)
            return false;

        operand = invocation.DataOperands[0];
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
            CSharpCallDescriptor descriptor => descriptor.Method,
            _ => null!
        };

        return method != null;
    }

    public static bool TryGetCallDescriptor(Instruction instruction, out CSharpCallDescriptor descriptor)
    {
        descriptor = default!;

        if (!TryGetCallOperand(instruction, out var operand) || operand is not CSharpCallDescriptor candidate)
            return false;

        descriptor = candidate;
        return true;
    }
}
