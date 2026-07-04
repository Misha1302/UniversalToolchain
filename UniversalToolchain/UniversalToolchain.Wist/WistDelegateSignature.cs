using ExceptionsManager;

namespace UniversalToolchain.Wist;

internal sealed class WistDelegateSignature
{
    private WistDelegateSignature(
        IReadOnlyList<string> parameterNames,
        IReadOnlyList<Type> parameterTypes,
        Type returnType,
        IReadOnlyDictionary<string, Type> bindingTypes)
    {
        ParameterNames = parameterNames;
        ParameterTypes = parameterTypes;
        ReturnType = returnType;
        BindingTypes = bindingTypes;
    }

    public IReadOnlyList<string> ParameterNames { get; }

    public IReadOnlyList<Type> ParameterTypes { get; }

    public Type ReturnType { get; }

    public IReadOnlyDictionary<string, Type> BindingTypes { get; }

    public static WistDelegateSignature FromDelegate<TDelegate>(IReadOnlyList<string> parameterNames)
        where TDelegate : Delegate
    {
        parameterNames = parameterNames.ArgNotNull();

        var invoke = typeof(TDelegate).GetMethod("Invoke");
        if (invoke == null)
            Thrower.Argument(nameof(TDelegate), $"Type '{typeof(TDelegate).FullName}' is not a delegate type.");

        if (invoke.ReturnType == typeof(void))
            Thrower.Argument(nameof(TDelegate), "Wist compiled delegates must return a value.");

        var parameters = invoke.GetParameters();
        if (parameters.Length != parameterNames.Count)
        {
            Thrower.Argument(
                nameof(parameterNames),
                $"Delegate '{typeof(TDelegate).Name}' expects {parameters.Length} parameters but {parameterNames.Count} names were provided.");
        }

        var names = new List<string>(parameterNames.Count);
        var types = new List<Type>(parameterNames.Count);
        var bindings = new OrderedDictionary<string, Type>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < parameterNames.Count; index++)
        {
            var name = parameterNames[index];
            if (string.IsNullOrWhiteSpace(name))
                Thrower.Argument(nameof(parameterNames), "Parameter names must not be null, empty, or whitespace.");

            if (!seen.Add(name))
                Thrower.Argument(nameof(parameterNames), $"Duplicate parameter name '{name}'.");

            var parameterType = parameters[index].ParameterType;
            if (parameterType.IsByRef)
                Thrower.Argument(nameof(TDelegate), "Wist compiled delegates do not support by-ref parameters.");

            names.Add(name);
            types.Add(parameterType);
            bindings[name] = parameterType;
        }

        return new WistDelegateSignature(names, types, invoke.ReturnType, bindings);
    }
}
