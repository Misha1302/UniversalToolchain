using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.Functions.Abstractions;

public sealed record BuiltinFunctionInvocationResult(bool IsSuccess, object? Value, IReadOnlyList<ToolchainDiagnostic> Diagnostics);
