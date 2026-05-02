namespace UniversalToolchain.Functions.Abstractions;

public sealed record BuiltinFunctionSignature(
    string Name,
    IReadOnlyList<FunctionTypeDescriptor> ParameterTypes);