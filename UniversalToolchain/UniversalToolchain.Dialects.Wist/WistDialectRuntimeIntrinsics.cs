using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

internal static class WistDialectRuntimeIntrinsics
{
    public static IReadOnlyList<RuntimeIntrinsicDescriptor> All { get; } = Create();

    private static IReadOnlyList<RuntimeIntrinsicDescriptor> Create()
    {
        var names = new SortedSet<string>(StringComparer.Ordinal)
        {
            "add_decimal",
            "add_f32",
            "add_f64",
            "add_i32",
            "add_i64",
            "call C#",
            "call C# ctor",
            "div_decimal",
            "div_f32",
            "div_f64",
            "div_i32",
            "div_i64",
            "ldloc",
            "load_local",
            "load_local_ref",
            "mul_decimal",
            "mul_f32",
            "mul_f64",
            "mul_i32",
            "mul_i64",
            "sub_decimal",
            "sub_f32",
            "sub_f64",
            "sub_i32",
            "sub_i64"
        };

        return names
            .Select(x => new RuntimeIntrinsicDescriptor(x, DialectBackendTarget.Any))
            .ToList();
    }
}