namespace FunctionCallsModule;

public sealed class FunctionCallPlanner
{
    private readonly IReadOnlyList<BuiltinFunctionRuntimeBinding> _runtimeBindings;

    public FunctionCallPlanner(IEnumerable<BuiltinFunctionRuntimeBinding> runtimeBindings)
    {
        runtimeBindings = runtimeBindings.ArgNotNull();

        _runtimeBindings = runtimeBindings
            .Select(static x => x.ArgNotNull())
            .OrderBy(static x => x.Signature.Name, StringComparer.Ordinal)
            .ThenBy(static x => x.Signature.ParameterTypes.Count)
            .ThenBy(static x => string.Join("|", x.Signature.ParameterTypes.Select(static y => y.Name)), StringComparer.Ordinal)
            .ThenBy(static x => x.Method.DeclaringType?.FullName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(static x => x.Method.Name, StringComparer.Ordinal)
            .ToList();
    }

    public FunctionCallPlan PlanOrThrow(string functionName, IReadOnlyList<Type> sourceTypes)
    {
        var result = TryPlan(functionName, sourceTypes);
        if (result.IsSuccess)
            return result.Plan!;

        return Thrower.InvalidOpEx<FunctionCallPlan>(result.DiagnosticMessage.NotNull());
    }

    public FunctionCallPlanningResult TryPlan(string functionName, IReadOnlyList<Type> sourceTypes)
    {
        if (string.IsNullOrWhiteSpace(functionName))
            Thrower.Argument(nameof(functionName), "Function name must not be empty.");

        sourceTypes = sourceTypes.ArgNotNull();

        var namedBindings = _runtimeBindings
            .Where(x => string.Equals(x.Signature.Name, functionName, StringComparison.Ordinal))
            .ToList();

        if (namedBindings.Count == 0)
        {
            return FunctionCallPlanningResult.Failure(
                "F001",
                $"Builtin function '{functionName}' is not available in the selected runtime catalog.");
        }

        var arityMatches = namedBindings
            .Where(x => x.Method.GetParameters().Length == sourceTypes.Count)
            .ToList();

        if (arityMatches.Count == 0)
        {
            return FunctionCallPlanningResult.Failure(
                "F002",
                $"Builtin function '{functionName}' expects a different argument count. Actual argument count: {sourceTypes.Count}.");
        }

        var candidates = arityMatches
            .Select(x => TryCreatePlan(x, sourceTypes))
            .Where(static x => x != null)
            .Select(static x => x!)
            .OrderBy(static x => x.AdapterCount)
            .ThenBy(static x => x.Binding.FeatureId.Value, StringComparer.Ordinal)
            .ThenBy(static x => x.Binding.Method.DeclaringType?.FullName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(static x => x.Binding.Method.Name, StringComparer.Ordinal)
            .ToList();

        if (candidates.Count == 0)
        {
            return FunctionCallPlanningResult.Failure(
                "F003",
                $"Builtin function '{functionName}' has no runtime binding for argument runtime types '{FormatTypes(sourceTypes)}'.");
        }

        var bestAdapterCount = candidates[0].AdapterCount;
        var bestCandidates = candidates
            .Where(x => x.AdapterCount == bestAdapterCount)
            .ToList();

        if (bestCandidates.Count > 1)
        {
            return FunctionCallPlanningResult.Failure(
                "F004",
                $"Builtin function '{functionName}' has multiple runtime bindings for argument runtime types '{FormatTypes(sourceTypes)}'.");
        }

        return FunctionCallPlanningResult.Success(bestCandidates[0]);
    }

    private static FunctionCallPlan? TryCreatePlan(
        BuiltinFunctionRuntimeBinding binding,
        IReadOnlyList<Type> sourceTypes)
    {
        var parameters = binding.Method.GetParameters();
        var argumentAdapters = new MethodInfo?[sourceTypes.Count];
        var adapterCount = 0;

        for (var i = 0; i < sourceTypes.Count; i++)
        {
            var sourceType = sourceTypes[i];
            var targetType = parameters[i].ParameterType;

            if (sourceType == targetType)
                continue;

            if (!TryGetValueAdapter(sourceType, targetType, out var adapter))
                return null;

            argumentAdapters[i] = adapter;
            adapterCount++;
        }

        var resultAdapterFactory = default(MethodInfo);
        var resultAdapterConstructor = default(ConstructorInfo);
        if (binding.Method.ReturnType != typeof(void)
            && TryResolveResultRuntimeType(binding.Method.ReturnType, sourceTypes, out var resultRuntimeType)
            && resultRuntimeType != binding.Method.ReturnType)
        {
            if (TryGetCreateAdapter(resultRuntimeType, binding.Method.ReturnType, out resultAdapterFactory))
            {
                adapterCount++;
            }
            else if (TryGetConstructorAdapter(resultRuntimeType, binding.Method.ReturnType, out resultAdapterConstructor))
            {
                adapterCount++;
            }
            else
            {
                return null;
            }
        }

        return new FunctionCallPlan(
            binding,
            argumentAdapters,
            resultAdapterFactory,
            resultAdapterConstructor,
            adapterCount);
    }

    private static bool TryGetValueAdapter(Type sourceType, Type targetType, out MethodInfo adapter)
    {
        adapter = null!;

        var gettableInterface = sourceType
            .GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType
                                 && x.GetGenericTypeDefinition() == typeof(IGettable<>)
                                 && x.GetGenericArguments()[0] == targetType);
        if (gettableInterface == null)
            return false;

        adapter = typeof(FunctionCallValueAdapter)
            .GetMethod(nameof(FunctionCallValueAdapter.GetValue), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(targetType);
        return true;
    }

    private static bool TryResolveResultRuntimeType(
        Type returnType,
        IReadOnlyList<Type> sourceTypes,
        out Type resultRuntimeType)
    {
        foreach (var sourceType in sourceTypes)
        {
            if (sourceType == returnType)
            {
                resultRuntimeType = sourceType;
                return true;
            }

            if (ImplementsCustomNumber(sourceType, returnType))
            {
                resultRuntimeType = sourceType;
                return true;
            }
        }

        resultRuntimeType = returnType;
        return true;
    }

    private static bool TryGetCreateAdapter(Type resultRuntimeType, Type returnType, out MethodInfo adapter)
    {
        adapter = null!;

        if (!ImplementsCustomNumber(resultRuntimeType, returnType))
            return false;

        adapter = resultRuntimeType.GetMethod(
            "Create",
            BindingFlags.Static | BindingFlags.Public,
            binder: null,
            types: [returnType],
            modifiers: null)!;
        return adapter != null && adapter.ReturnType == resultRuntimeType;
    }

    private static bool TryGetConstructorAdapter(Type resultRuntimeType, Type returnType, out ConstructorInfo adapter)
    {
        adapter = resultRuntimeType.GetConstructor([returnType])!;
        return adapter != null;
    }

    private static bool ImplementsCustomNumber(Type type, Type valueType)
    {
        return type
            .GetInterfaces()
            .Any(x => x.IsGenericType
                      && x.GetGenericTypeDefinition() == typeof(ICustomNumber<,>)
                      && x.GetGenericArguments()[0] == type
                      && x.GetGenericArguments()[1] == valueType);
    }

    private static string FormatTypes(IEnumerable<Type> types)
    {
        return string.Join(", ", types.Select(static x => x.FullName ?? x.Name));
    }
}
