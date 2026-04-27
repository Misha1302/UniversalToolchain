using UniversalToolchain.Capabilities.Abstractions;

namespace UniversalToolchain.Functions.Abstractions;

public sealed record BuiltinFunctionDescriptor(
    string Name,
    LanguageFeatureId FeatureId,
    IReadOnlyList<FunctionParameterDescriptor> Parameters,
    FunctionTypeDescriptor ReturnType,
    FunctionPurity Purity,
    IReadOnlyList<string> SupportedBackendAliases);
