using System.Reflection;
using UniversalToolchain.Capabilities.Abstractions;

namespace UniversalToolchain.Functions.Abstractions;

public sealed record BuiltinFunctionRuntimeBinding(
    BuiltinFunctionSignature Signature,
    FunctionTypeDescriptor ReturnType,
    LanguageFeatureId FeatureId,
    MethodInfo Method,
    IReadOnlyList<string> SupportedBackendAliases);