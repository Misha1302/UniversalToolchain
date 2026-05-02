using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.ExpressionTyping.Abstractions;

public interface IExpressionTypeResolver
{
    ExpressionTypeDescriptor? Resolve(
        object node,
        ExpressionTypeResolutionContext context,
        out IReadOnlyList<ToolchainDiagnostic> diagnostics);
}