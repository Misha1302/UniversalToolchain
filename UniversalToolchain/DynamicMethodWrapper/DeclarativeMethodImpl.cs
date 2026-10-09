using IntermediateRepresentationAbstractions;

namespace DynamicMethodWrapper;

public interface IBytecodeOperationData;

public interface IBytecodeOperationHandler
{
    Type OperationType { get; }
    IAbstractIR Emit(IBytecodeOperationData operation, IAbstractMethodConvertable.Context context);
}

public abstract class BytecodeOperationHandler<T> : IBytecodeOperationHandler
    where T : IBytecodeOperationData
{
    public Type OperationType => typeof(T);

    public IAbstractIR Emit(IBytecodeOperationData operation, IAbstractMethodConvertable.Context context)
    {
        if (operation is not T typed)
            throw new InvalidOperationException(
                $"Handler for '{typeof(T).FullName}' cannot emit '{operation.GetType().FullName}'.");
        return Emit(typed, context);
    }

    protected abstract IAbstractIR Emit(T operation, IAbstractMethodConvertable.Context context);
}

public sealed class BytecodeOperationHandlerRegistry
{
    private readonly IReadOnlyDictionary<Type, IBytecodeOperationHandler> _handlers;

    public BytecodeOperationHandlerRegistry(IEnumerable<IBytecodeOperationHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        var map = new Dictionary<Type, IBytecodeOperationHandler>();
        foreach (var handler in handlers)
        {
            ArgumentNullException.ThrowIfNull(handler);
            if (!map.TryAdd(handler.OperationType, handler))
                throw new InvalidOperationException(
                    $"More than one bytecode operation handler is registered for '{handler.OperationType.FullName}'.");
        }
        _handlers = map;
    }

    public IBytecodeOperationHandler Resolve(IBytecodeOperationData operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var type = operation.GetType();
        return _handlers.TryGetValue(type, out var handler)
            ? handler
            : throw new InvalidOperationException(
                $"No bytecode operation handler is registered for '{type.FullName}'.");
    }
}

public sealed class DeclarativeMethodImpl : IAbstractMethodConvertable
{
    private readonly IBytecodeOperationData _operation;
    private readonly IBytecodeOperationHandler _handler;

    public DeclarativeMethodImpl(
        string name,
        IBytecodeOperationData operation,
        BytecodeOperationHandlerRegistry registry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        _operation = operation ?? throw new ArgumentNullException(nameof(operation));
        ArgumentNullException.ThrowIfNull(registry);
        _handler = registry.Resolve(operation);
    }

    public string Name { get; }
    public IBytecodeOperationData Operation => _operation;
    public IAbstractIR GetAbstractIR(IAbstractMethodConvertable.Context context) =>
        _handler.Emit(_operation, context);

    public override string ToString() => Name;
}
