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
        ArgumentNullException.ThrowIfNull(parameterNames);

        var invoke = typeof(TDelegate).GetMethod("Invoke");
        if (invoke == null)
            throw new WistUserInputException($"Type '{typeof(TDelegate).FullName}' is not a delegate type.", nameof(TDelegate));

        if (invoke.ReturnType == typeof(void))
            throw new WistUserInputException("Wist compiled delegates must return a value.", nameof(TDelegate));

        var parameters = invoke.GetParameters();
        if (parameters.Length != parameterNames.Count)
        {
            throw new WistUserInputException(
                $"Delegate '{typeof(TDelegate).Name}' expects {parameters.Length} parameters but {parameterNames.Count} names were provided.",
                nameof(parameterNames));
        }

        var names = new List<string>(parameterNames.Count);
        var types = new List<Type>(parameterNames.Count);
        var bindings = new OrderedDictionary<string, Type>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < parameterNames.Count; index++)
        {
            var name = parameterNames[index];
            if (string.IsNullOrWhiteSpace(name))
                throw new WistUserInputException("Parameter names must not be null, empty, or whitespace.", nameof(parameterNames));

            if (!seen.Add(name))
                throw new WistUserInputException($"Duplicate parameter name '{name}'.", nameof(parameterNames));

            var parameterType = parameters[index].ParameterType;
            if (parameterType.IsByRef)
                throw new WistUserInputException("Wist compiled delegates do not support by-ref parameters.", nameof(TDelegate));

            names.Add(name);
            types.Add(parameterType);
            bindings[name] = parameterType;
        }

        return new WistDelegateSignature(names, types, invoke.ReturnType, bindings);
    }
}
