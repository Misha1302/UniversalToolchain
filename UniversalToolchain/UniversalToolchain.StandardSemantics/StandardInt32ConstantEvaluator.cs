using System.Globalization;
using UniversalToolchain.Semantics.Abstractions;

namespace UniversalToolchain.StandardSemantics;

public sealed class StandardInt32ConstantEvaluator : IConstantEvaluator
{
    public bool TryEvaluate(
        CallableDescriptor descriptor,
        IReadOnlyList<ConstantValue> arguments,
        out ConstantValue result)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(arguments);

        result = default!;
        if (descriptor.Id == StandardCallables.AddInt32Unchecked)
            return TryEvaluateBinaryInt32(arguments, StandardSemanticTypes.Int32, static (left, right) => unchecked(left + right), out result);

        if (descriptor.Id == StandardCallables.SubtractInt32Unchecked)
            return TryEvaluateBinaryInt32(arguments, StandardSemanticTypes.Int32, static (left, right) => unchecked(left - right), out result);

        if (descriptor.Id == StandardCallables.MultiplyInt32Unchecked)
            return TryEvaluateBinaryInt32(arguments, StandardSemanticTypes.Int32, static (left, right) => unchecked(left * right), out result);

        if (descriptor.Id == StandardCallables.EqualInt32)
        {
            if (!TryReadBinaryInt32(arguments, out var left, out var right))
                return false;

            result = new ConstantValue(StandardSemanticTypes.Bool, (left == right).ToString());
            return true;
        }

        return false;
    }

    private static bool TryEvaluateBinaryInt32(
        IReadOnlyList<ConstantValue> arguments,
        SemanticTypeId resultType,
        Func<int, int, int> operation,
        out ConstantValue result)
    {
        result = default!;
        if (!TryReadBinaryInt32(arguments, out var left, out var right))
            return false;

        result = new ConstantValue(resultType, operation(left, right).ToString(CultureInfo.InvariantCulture));
        return true;
    }

    private static bool TryReadBinaryInt32(IReadOnlyList<ConstantValue> arguments, out int left, out int right)
    {
        left = default;
        right = default;

        if (arguments.Count != 2 ||
            arguments[0].Type != StandardSemanticTypes.Int32 ||
            arguments[1].Type != StandardSemanticTypes.Int32)
        {
            return false;
        }

        return int.TryParse(arguments[0].CanonicalValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out left) &&
               int.TryParse(arguments[1].CanonicalValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out right);
    }
}
