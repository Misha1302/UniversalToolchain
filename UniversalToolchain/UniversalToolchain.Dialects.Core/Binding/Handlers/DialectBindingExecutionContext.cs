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
        if (source == null)
            Thrower.ArgumentNull(nameof(source));

        if (builder == null)
            Thrower.ArgumentNull(nameof(builder));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

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
