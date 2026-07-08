using System.Globalization;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

public static class SsaConstantMaterializer
{
    public static SsaOperation? TryCreate(ISsaInstruction source, ConstantValue value)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Results.Count != 1 ||
            !SameType(source.Results[0].Type, value.Type))
        {
            return null;
        }

        if (source.Results[0].Type == SsaTypes.Int32 &&
            int.TryParse(value.CanonicalValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return Create(source, SsaOperations.ConstantInt32, intValue.ToString(CultureInfo.InvariantCulture));
        }

        if (source.Results[0].Type == SsaTypes.Bool &&
            bool.TryParse(value.CanonicalValue, out var boolValue))
        {
            return Create(source, SsaOperations.ConstantBool, boolValue.ToString());
        }

        if (source.Results[0].Type == SsaTypes.Float64 &&
            double.TryParse(value.CanonicalValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
        {
            return Create(source, SsaOperations.ConstantFloat64, floatValue.ToString("R", CultureInfo.InvariantCulture));
        }

        return null;
    }

    public static SsaOperation Int32(SsaOperationId id, SsaValue result, int value) =>
        Create(id, SsaOperations.ConstantInt32, [result], value.ToString(CultureInfo.InvariantCulture));

    public static SsaOperation Bool(SsaOperationId id, SsaValue result, bool value) =>
        Create(id, SsaOperations.ConstantBool, [result], value.ToString());

    public static SsaOperation Float64(SsaOperationId id, SsaValue result, double value) =>
        Create(id, SsaOperations.ConstantFloat64, [result], value.ToString("R", CultureInfo.InvariantCulture));

    private static SsaOperation Create(ISsaInstruction source, SsaOpId opId, string canonicalValue) =>
        Create(source.Id, opId, source.Results, canonicalValue);

    private static SsaOperation Create(
        SsaOperationId id,
        SsaOpId opId,
        IEnumerable<SsaValue> results,
        string canonicalValue) =>
        new(
            id,
            opId,
            results: results,
            attributes: new SsaAttributeBag(
            [
                new SsaAttribute(SsaAttributeKeys.ConstantValue, canonicalValue)
            ]));

    private static bool SameType(SsaTypeId ssaType, SemanticTypeId semanticType) =>
        string.Equals(ssaType.Value, semanticType.Value, StringComparison.Ordinal);
}
