using BasicCore.Capabilities;
using BasicCore.Contracts;
using IntermediateRepresentationAbstractions;

namespace BasicCore.Core;

/// <summary>
/// Canonical view of a typed AIR intrinsic instruction.
/// </summary>
public readonly record struct IntrinsicInstructionView(
    IntrinsicInvocation Invocation,
    string CapabilityId)
{
    public IReadOnlyList<object?> DataOperands => Invocation.DataOperands;
    public IReadOnlyList<IntrinsicTypeArgument> TypeArguments => Invocation.TypeArguments;

    public static bool TryRead(Instruction instruction, out IntrinsicInstructionView view)
    {
        view = default;
        if (!instruction.TryGetTypedIntrinsicInvocation(out var invocation))
            return false;
        if (!IntrinsicCapabilityNameEncoder.TryEncode(invocation, out var capabilityId))
            return false;

        view = new IntrinsicInstructionView(invocation, capabilityId);
        return true;
    }

    public static IntrinsicInstructionView ReadOrThrow(Instruction instruction) =>
        TryRead(instruction, out var view)
            ? view
            : throw new InvalidOperationException(
                $"AIR intrinsic must contain exactly one structured IntrinsicInvocation payload: {instruction}");
}

public static class IntrinsicInvocationExtensions
{
    public static object GetRequiredDataOperand(this IntrinsicInvocation invocation, int index)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        if (index < 0 || index >= invocation.DataOperands.Count || invocation.DataOperands[index] == null)
            throw new InvalidOperationException(
                $"Intrinsic '{invocation.Symbol}' requires a non-null data operand at index {index}.");
        return invocation.DataOperands[index]!;
    }

    public static T GetRequiredDataOperand<T>(this IntrinsicInvocation invocation, int index)
    {
        var operand = invocation.GetRequiredDataOperand(index);
        return operand is T value
            ? value
            : throw new InvalidOperationException(
                $"Intrinsic '{invocation.Symbol}' data operand {index} must be '{typeof(T)}', but was '{operand.GetType()}'.");
    }

    public static Type GetRequiredSingleRuntimeType(this IntrinsicInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        return invocation.TypeArguments.Count == 1
            ? invocation.TypeArguments[0].RuntimeType
            : throw new InvalidOperationException(
                $"Intrinsic '{invocation.Symbol}' requires exactly one type argument, but has {invocation.TypeArguments.Count}.");
    }
}
