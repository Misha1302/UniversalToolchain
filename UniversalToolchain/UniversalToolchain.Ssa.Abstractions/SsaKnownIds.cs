namespace UniversalToolchain.Ssa.Abstractions;

using UniversalToolchain.Semantics.Abstractions;

public static class SsaTypes
{
    public static SsaTypeId Bool { get; } = new("core.bool");

    public static SsaTypeId Int32 { get; } = new("core.i32");

    public static SsaTypeId Float64 { get; } = new("core.f64");

    public static SsaTypeId Object { get; } = new("core.object");
}

public static class SsaOperations
{
    public static SsaOpId ConstantInt32 { get; } = new("core.const.i32");

    public static SsaOpId ConstantBool { get; } = new("core.const.bool");

    public static SsaOpId ConstantFloat64 { get; } = new("core.const.f64");
}

public static class SsaAttributeKeys
{
    public static SsaAttributeKey ConstantValue { get; } = new("ssa.constant.value");
}

public static class SsaPreviewSemanticTypes
{
    public static SemanticTypeId Bool { get; } = new(SsaTypes.Bool.Value);

    public static SemanticTypeId Int32 { get; } = new(SsaTypes.Int32.Value);

    public static SemanticTypeId Float64 { get; } = new(SsaTypes.Float64.Value);

    public static SemanticTypeId Object { get; } = new(SsaTypes.Object.Value);
}

public static class SsaPreviewCallables
{
    public static CallableId AddInt32Unchecked { get; } = new("ssa.preview.i32.add.unchecked");

    public static CallableId SubtractInt32Unchecked { get; } = new("ssa.preview.i32.sub.unchecked");

    public static CallableId MultiplyInt32Unchecked { get; } = new("ssa.preview.i32.mul.unchecked");

    public static CallableId EqualInt32 { get; } = new("ssa.preview.i32.eq");
}
