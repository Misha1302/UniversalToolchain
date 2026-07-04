using UniversalToolchain.Semantics.Abstractions;

namespace UniversalToolchain.StandardSemantics;

public static class StandardSemanticTypes
{
    public static SemanticTypeId Bool { get; } = new("std.bool");

    public static SemanticTypeId Int32 { get; } = new("std.i32");

    public static SemanticTypeId Float64 { get; } = new("std.f64");
}

public static class StandardCallables
{
    public static CallableId AddInt32Unchecked { get; } = new("std.i32.add.unchecked");

    public static CallableId SubtractInt32Unchecked { get; } = new("std.i32.sub.unchecked");

    public static CallableId MultiplyInt32Unchecked { get; } = new("std.i32.mul.unchecked");

    public static CallableId EqualInt32 { get; } = new("std.i32.eq");
}
