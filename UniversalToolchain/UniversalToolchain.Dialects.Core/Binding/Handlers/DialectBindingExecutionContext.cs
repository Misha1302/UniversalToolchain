using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class DialectBindingExecutionContext
{
    public DialectBindingExecutionContext(
        IDialectBindingSource source,
        DialectDefinitionBuilder builder,
        List<DialectDiagnostic> diagnostics)
    {
        source = source.ArgNotNull();

        builder = builder.ArgNotNull();

        diagnostics = diagnostics.ArgNotNull();

        Source = source;
        Builder = builder;
        Diagnostics = diagnostics;
        DirectiveContext = DialectDirectiveHandlerContext.FromInputKind(source.InputKind);
    }

    public IDialectBindingSource Source { get; }

    public DialectDefinitionBuilder Builder { get; }

    public List<DialectDiagnostic> Diagnostics { get; }

    public DialectDirectiveHandlerContext DirectiveContext { get; }
}
