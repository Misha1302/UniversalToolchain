namespace BasicCore.Execution;

public sealed class ExecutionEnvironment : IExecutionEnvironment, IExternalBindingsLayoutProvider
{
    private readonly object?[] _values;
    private readonly Dictionary<RuntimeContextKey, object> _runtimeContexts = [];
    private readonly Dictionary<Type, object> _runtimeProviders = [];

    public ExecutionEnvironment(IReadOnlyList<ExternalBinding> bindings, ExternalBindingsLayout? externalBindingsLayout = null)
    {
        bindings = bindings.ArgNotNull();

        _values = new object?[bindings.Count];
        for (var i = 0; i < bindings.Count; i++)
            _values[i] = bindings[i].Value;

        ExternalBindingsLayout = externalBindingsLayout ?? ExternalBindingsLayout.FromDeclaredBindings(bindings);
    }

    public object? GetExternalValue(int slot) => _values[slot];

    public void SetExternalValue(int slot, object? value) => _values[slot] = value;

    public TContext GetOrCreate<TContext>(RuntimeContextKey key, Func<TContext> factory) where TContext : class
    {
        factory = factory.ArgNotNull();

        if (_runtimeContexts.TryGetValue(key, out var existing))
            return (TContext)existing;

        var context = factory().NotNull();
        _runtimeContexts[key] = context;
        return context;
    }

    public object GetRequiredProvider(Type providerType)
    {
        providerType = providerType.ArgNotNull();

        if (_runtimeProviders.TryGetValue(providerType, out var existing))
            return existing;

        var provider = CreateRuntimeProvider(providerType);
        _runtimeProviders[providerType] = provider;
        return provider;
    }

    public ExternalBindingsLayout ExternalBindingsLayout { get; }

    private object CreateRuntimeProvider(Type providerType)
    {
        var constructors = providerType.GetConstructors();
        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(IRuntimeContextStore))
                return constructor.Invoke([this]).NotNull();
        }

        var parameterless = providerType.GetConstructor(Type.EmptyTypes);
        if (parameterless != null)
            return parameterless.Invoke(null).NotNull();

        return Thrower.InvalidOpEx<object>(
            $"Runtime call provider '{providerType.FullName}' must expose either a parameterless constructor or a constructor that accepts IRuntimeContextStore.");
    }
}
