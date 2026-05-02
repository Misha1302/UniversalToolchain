using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.Functions.Abstractions;

public sealed record BuiltinFunctionResolution(
    bool IsSuccess,
    BuiltinFunctionDescriptor? Descriptor,
    BuiltinFunctionRuntimeBinding? RuntimeBinding,
    FunctionTypeDescriptor? ReturnType,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics);