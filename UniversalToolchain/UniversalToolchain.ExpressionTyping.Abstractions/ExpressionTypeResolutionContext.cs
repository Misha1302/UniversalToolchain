using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.ExpressionTyping.Abstractions;

public sealed record ExpressionTypeResolutionContext(
    IReadOnlyDictionary<string, ExpressionTypeDescriptor> KnownBindings,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics);
