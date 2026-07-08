using System.Globalization;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

public static class SsaConstantReader
{
    public static bool TryRead(ISsaInstruction instruction, out ConstantValue constant)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        constant = default!;
        if (instruction is not SsaOperation operation ||
            operation.Results.Count != 1 ||
            !operation.Attributes.TryGet(SsaAttributeKeys.ConstantValue, out var attribute))
        {
            return false;
        }

        var result = operation.Results[0];
        if (operation.OpId == SsaOperations.ConstantInt32 &&
            result.Type == SsaTypes.Int32 &&
            int.TryParse(attribute.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            constant = new ConstantValue(SsaPreviewSemanticTypes.Int32, attribute.Value);
            return true;
        }

        if (operation.OpId == SsaOperations.ConstantBool &&
            result.Type == SsaTypes.Bool &&
            bool.TryParse(attribute.Value, out _))
        {
            constant = new ConstantValue(SsaPreviewSemanticTypes.Bool, attribute.Value);
            return true;
        }

        if (operation.OpId == SsaOperations.ConstantFloat64 &&
            result.Type == SsaTypes.Float64 &&
            double.TryParse(attribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            constant = new ConstantValue(SsaPreviewSemanticTypes.Float64, attribute.Value);
            return true;
        }

        return false;
    }
}
