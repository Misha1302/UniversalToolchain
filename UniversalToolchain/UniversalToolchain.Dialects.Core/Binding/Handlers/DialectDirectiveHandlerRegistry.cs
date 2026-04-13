using ExceptionsManager;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class DialectDirectiveHandlerRegistry
{
    private readonly IDialectDirectiveHandler[] _handlers;

    public DialectDirectiveHandlerRegistry(IEnumerable<IDialectDirectiveHandler> handlers)
    {
        if (handlers == null)
            Thrower.ArgumentNull(nameof(handlers));

        _handlers = handlers
            .Select(ValidateHandler)
            .OrderBy(static x => x.Order)
            .ThenBy(static x => x.GetType().FullName, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<IDialectDirectiveHandler> Handlers => _handlers;

    public void Apply(DialectBindingExecutionContext context)
    {
        if (context == null)
            Thrower.ArgumentNull(nameof(context));

        foreach (var handler in _handlers)
            handler.Apply(context);
    }

    private static IDialectDirectiveHandler ValidateHandler(IDialectDirectiveHandler handler)
    {
        if (handler == null)
            Thrower.ArgumentNull(nameof(handler));

        return handler;
    }
}
