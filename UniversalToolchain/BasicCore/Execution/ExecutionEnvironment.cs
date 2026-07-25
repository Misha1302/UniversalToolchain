using System.Reflection;

namespace BasicCore.Execution;

public sealed class ExecutionEnvironment : IExecutionEnvironment, IExternalBindingsLayoutProvider
{
    private readonly HashSet<Type>? _allowedRuntimeProviderTypes;
    private readonly Dictionary<RuntimeContextKey, object> _runtimeContexts = [];
    private readonly Dictionary<Type, object> _runtimeProviders = [];
    private readonly object?[] _values;

    public ExecutionEnvironment(
        IReadOnlyList<ExternalBinding> bindings,
        ExternalBindingsLayout? externalBindingsLayout = null,
        IReadOnlyCollection<Type>? allowedRuntimeProviderTypes = null)
    {
        bindings = bindings.ArgNotNull();

        _values = new object?[bindings.Count];
        for (var i = 0; i < bindings.Count; i++)
            _values[i] = bindings[i].Value;

        if (allowedRuntimeProviderTypes != null)
            _allowedRuntimeProviderTypes = allowedRuntimeProviderTypes.ToHashSet();

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

        if (_allowedRuntimeProviderTypes != null && !_allowedRuntimeProviderTypes.Contains(providerType))
            return Thrower.InvalidOpEx<object>(
                $"Runtime call provider '{providerType.FullName}' is not allowed in the current execution session.");

        if (_runtimeProviders.TryGetValue(providerType, out var existing))
            return existing;

        var provider = CreateRuntimeProvider(providerType);
        _runtimeProviders[providerType] = provider;
        return provider;
    }

    public ExternalBindingsLayout ExternalBindingsLayout { get; }

    private object CreateRuntimeProvider(Type providerType)
    {
        var supportedConstructors = providerType.GetConstructors()
            .Where(static constructor => IsSupportedProviderConstructor(constructor.GetParameters()))
            .OrderBy(static constructor => FormatConstructorSignature(constructor), StringComparer.Ordinal)
            .ToArray();

        if (supportedConstructors.Length == 0)
        {
            return Thrower.InvalidOpEx<object>(
                $"Runtime call provider '{providerType.FullName}' must expose exactly one supported public constructor: " +
                "(), (IRuntimeContextStore), or (IExecutionEnvironment).");
        }

        if (supportedConstructors.Length != 1)
        {
            return Thrower.InvalidOpEx<object>(
                $"Runtime call provider '{providerType.FullName}' has ambiguous supported constructors: " +
                $"{string.Join(", ", supportedConstructors.Select(FormatConstructorSignature))}. " +
                "Expose exactly one supported constructor or use an explicit provider factory.");
        }

        var constructor = supportedConstructors[0];
        var parameters = constructor.GetParameters();
        return parameters.Length == 0
            ? constructor.Invoke(null).NotNull()
            : constructor.Invoke([this]).NotNull();
    }

    private static bool IsSupportedProviderConstructor(IReadOnlyList<ParameterInfo> parameters) =>
        parameters.Count == 0 ||
        parameters.Count == 1 &&
        (parameters[0].ParameterType == typeof(IRuntimeContextStore) ||
         parameters[0].ParameterType == typeof(IExecutionEnvironment));

    private static string FormatConstructorSignature(ConstructorInfo constructor) =>
        $"({string.Join(", ", constructor.GetParameters().Select(static parameter => parameter.ParameterType.Name))})";
}