using System.Globalization;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

public sealed class SsaPreviewInt32ConstantEvaluator : IConstantEvaluator
{
    public bool TryEvaluate(
        CallableDescriptor descriptor,
        IReadOnlyList<ConstantValue> arguments,
        out ConstantValue result)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(arguments);

        result = default!;
        if (descriptor.Id == SsaPreviewCallables.AddInt32Unchecked)
            return TryEvaluateBinaryInt32(arguments, SsaPreviewSemanticTypes.Int32, static (left, right) => unchecked(left + right), out result);

        if (descriptor.Id == SsaPreviewCallables.SubtractInt32Unchecked)
            return TryEvaluateBinaryInt32(arguments, SsaPreviewSemanticTypes.Int32, static (left, right) => unchecked(left - right), out result);

        if (descriptor.Id == SsaPreviewCallables.MultiplyInt32Unchecked)
            return TryEvaluateBinaryInt32(arguments, SsaPreviewSemanticTypes.Int32, static (left, right) => unchecked(left * right), out result);

        if (descriptor.Id == SsaPreviewCallables.EqualInt32)
        {
            if (!TryReadBinaryInt32(arguments, out var left, out var right))
                return false;

            result = new ConstantValue(SsaPreviewSemanticTypes.Bool, (left == right).ToString());
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
            arguments[0].Type != SsaPreviewSemanticTypes.Int32 ||
            arguments[1].Type != SsaPreviewSemanticTypes.Int32)
        {
            return false;
        }

        return int.TryParse(arguments[0].CanonicalValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out left) &&
               int.TryParse(arguments[1].CanonicalValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out right);
    }
}
