using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Functions.Abstractions;

public sealed record BuiltinFunctionResolution(
    bool IsSuccess,
    BuiltinFunctionDescriptor? Descriptor,
    FunctionTypeDescriptor? ReturnType,
    IReadOnlyList<RuleDiagnostic> Diagnostics);
