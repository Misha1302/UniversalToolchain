using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class DialectDirectiveHandlerRegistry
{
    private readonly IReadOnlyList<IDialectDirectiveHandler> _handlers;

    public DialectDirectiveHandlerRegistry(IEnumerable<IDialectDirectiveHandler> handlers)
    {
        if (handlers == null)
            Thrower.ArgumentNull(nameof(handlers));

        var orderedHandlers = handlers.Select(ValidateHandler)
            .OrderBy(x => x.Order)
            .ThenBy(x => x.GetType().FullName, StringComparer.Ordinal)
            .ToList();

        _handlers = orderedHandlers;
    }

    public IReadOnlyList<IDialectDirectiveHandler> Handlers => _handlers;

    public void Apply(IDialectBindingSource source, DialectDefinitionBuilder builder, List<DialectDiagnostic> diagnostics)
    {
        if (source == null)
            Thrower.ArgumentNull(nameof(source));

        if (builder == null)
            Thrower.ArgumentNull(nameof(builder));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        foreach (var handler in _handlers)
            handler.Apply(source, builder, diagnostics);
    }

    private static IDialectDirectiveHandler ValidateHandler(IDialectDirectiveHandler handler)
    {
        if (handler == null)
            Thrower.ArgumentNull(nameof(handler));

        return handler;
    }
}
