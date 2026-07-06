using ExceptionsManager;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

public sealed class DialectDirectiveHandlerRegistry
{
    private readonly IDialectDirectiveHandler[] _handlers;

    public DialectDirectiveHandlerRegistry(IEnumerable<IDialectDirectiveHandler> handlers)
    {
        handlers = handlers.ArgNotNull();

        _handlers = handlers
            .Select(ValidateHandler)
            .OrderBy(static x => x.Order)
            .ThenBy(static x => x.GetType().FullName, StringComparer.Ordinal)
            .ToArray();

        var duplicateName = _handlers
            .GroupBy(static x => x.Name, StringComparer.Ordinal)
            .FirstOrDefault(static x => x.Count() > 1);
        if (duplicateName != null)
            Thrower.InvalidOpEx($"Dialect directive semantic handler family '{duplicateName.Key}' is registered more than once.");
    }

    public IReadOnlyList<IDialectDirectiveHandler> Handlers => _handlers;

    public void Apply(DialectDirectiveBindingContext context)
    {
        context = context.ArgNotNull();

        foreach (var handler in _handlers)
            handler.Apply(context);
    }

    private static IDialectDirectiveHandler ValidateHandler(IDialectDirectiveHandler handler)
    {
        handler = handler.ArgNotNull();

        if (string.IsNullOrWhiteSpace(handler.Name))
            Thrower.InvalidOpEx("Dialect directive semantic handler name must not be empty.");

        return handler;
    }
}
