namespace FunctionCallsModule;

public sealed class FunctionCallsAstVisitor : IAstVisitor
{
    private static readonly AstNodeType FunctionCallNodeType = AstNodeType.CreateOrGet("FunctionCall");
    private static readonly AstNodeType ScopeNodeType = AstNodeType.CreateOrGet("Scope");
    private static readonly AstNodeType CommaNodeType = AstNodeType.CreateOrGet("Comma");

    private readonly IReadOnlyList<BuiltinFunctionRuntimeBinding> _runtimeBindings;
    private int _callSequence;

    public FunctionCallsAstVisitor(CapabilityCatalog capabilityCatalog)
    {
        capabilityCatalog = capabilityCatalog.ArgNotNull();

        _runtimeBindings = capabilityCatalog.BuiltinFunctionRuntimeBindings
            .OrderBy(static x => x.Signature.Name, StringComparer.Ordinal)
            .ThenBy(static x => x.Signature.ParameterTypes.Count)
            .ThenBy(static x => string.Join("|", x.Signature.ParameterTypes.Select(static y => y.Name)), StringComparer.Ordinal)
            .ThenBy(static x => x.Method.DeclaringType?.FullName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(static x => x.Method.Name, StringComparer.Ordinal)
            .ToList();
    }

    public void TryVisit(BytecodeVisitorData data)
    {
        data = data.ArgNotNull();

        if (data.Node.NodeType != FunctionCallNodeType)
            return;

        var functionName = data.Node.Text;
        if (string.IsNullOrWhiteSpace(functionName))
            Thrower.InvalidOpEx("Function call node must contain a function identifier.");

        var argumentsScope = data.Node.SafeGet(0);
        if (argumentsScope?.NodeType != ScopeNodeType)
            Thrower.InvalidOpEx($"Function call '{functionName}' must contain an argument scope.");

        var arguments = GetArguments(functionName, argumentsScope);
        foreach (var argument in arguments)
            data.AstToBytecodeTranslator.Translate(argument);

        var callSequence = _callSequence++;
        var localPrefix = $"__function_call_{callSequence}";
        var method = new AbstractMethodImpl(
            $"CallFunction_{functionName}_{arguments.Count}",
            (il, context) => EmitFunctionCall(il, context, functionName, arguments.Count, localPrefix));

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    private void EmitFunctionCall(
        IAbstractIR il,
        IAbstractMethodConvertable.Context context,
        string functionName,
        int argumentCount,
        string localPrefix)
    {
        if (context.Stack.Count < argumentCount)
            Thrower.InvalidOpEx($"Function call '{functionName}' requires {argumentCount} stack argument(s).");

        var sourceTypes = context.Stack.TakeLast(argumentCount).ToList();
        var plan = Resolve(functionName, sourceTypes);

        var localNames = CreateLocalNames(localPrefix, argumentCount);
        for (var i = argumentCount - 1; i >= 0; i--)
            il.SetValueToLocal(localNames[i], sourceTypes[i]);

        for (var i = 0; i < argumentCount; i++)
        {
            il.LdLoc(localNames[i], sourceTypes[i]);
            var adapter = plan.ArgumentAdapters[i];
            if (adapter != null)
                il.CallCSharp(adapter);
        }

        il.CallCSharp(plan.Binding.Method);

        if (plan.ResultAdapterFactory != null)
            il.CallCSharp(plan.ResultAdapterFactory);
        else if (plan.ResultAdapterConstructor != null)
            il.CallCSharp(plan.ResultAdapterConstructor);
    }

    private FunctionCallPlan Resolve(string functionName, IReadOnlyList<Type> sourceTypes)
    {
        var namedBindings = _runtimeBindings
            .Where(x => string.Equals(x.Signature.Name, functionName, StringComparison.Ordinal))
            .ToList();

        if (namedBindings.Count == 0)
        {
            return Thrower.InvalidOpEx<FunctionCallPlan>(
                $"Builtin function '{functionName}' is not available in the selected runtime catalog.");
        }

        var arityMatches = namedBindings
            .Where(x => x.Method.GetParameters().Length == sourceTypes.Count)
            .ToList();

        if (arityMatches.Count == 0)
        {
            return Thrower.InvalidOpEx<FunctionCallPlan>(
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
            return Thrower.InvalidOpEx<FunctionCallPlan>(
                $"Builtin function '{functionName}' has no runtime binding for argument runtime types '{FormatTypes(sourceTypes)}'.");
        }

        var bestAdapterCount = candidates[0].AdapterCount;
        var bestCandidates = candidates
            .Where(x => x.AdapterCount == bestAdapterCount)
            .ToList();

        if (bestCandidates.Count > 1)
        {
            return Thrower.InvalidOpEx<FunctionCallPlan>(
                $"Builtin function '{functionName}' has multiple runtime bindings for argument runtime types '{FormatTypes(sourceTypes)}'.");
        }

        return bestCandidates[0];
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

    private static IReadOnlyList<AstNode> GetArguments(string functionName, AstNode argumentsScope)
    {
        if (argumentsScope.Children.Count == 0)
            return [];

        var arguments = new List<AstNode>();
        var currentSegment = new List<AstNode>();
        var previousWasSeparator = false;

        foreach (var child in argumentsScope.Children)
        {
            if (child.NodeType == CommaNodeType)
            {
                AddArgument(functionName, arguments, currentSegment);
                currentSegment.Clear();
                previousWasSeparator = true;
                continue;
            }

            currentSegment.Add(child);
            previousWasSeparator = false;
        }

        if (previousWasSeparator)
            Thrower.InvalidOpEx($"Function call '{functionName}' contains an empty argument.");

        AddArgument(functionName, arguments, currentSegment);
        return arguments;
    }

    private static void AddArgument(string functionName, List<AstNode> arguments, IReadOnlyList<AstNode> segment)
    {
        if (segment.Count == 0)
            Thrower.InvalidOpEx($"Function call '{functionName}' contains an empty argument.");

        if (segment.Count != 1)
            Thrower.InvalidOpEx($"Function call '{functionName}' argument is not a single expression node.");

        arguments.Add(segment[0]);
    }

    private static IReadOnlyList<string> CreateLocalNames(string localPrefix, int argumentCount)
    {
        return Enumerable
            .Range(0, argumentCount)
            .Select(x => $"{localPrefix}_arg_{x}")
            .ToList();
    }

    private static string FormatTypes(IEnumerable<Type> types)
    {
        return string.Join(", ", types.Select(static x => x.FullName ?? x.Name));
    }

    private sealed record FunctionCallPlan(
        BuiltinFunctionRuntimeBinding Binding,
        IReadOnlyList<MethodInfo?> ArgumentAdapters,
        MethodInfo? ResultAdapterFactory,
        ConstructorInfo? ResultAdapterConstructor,
        int AdapterCount);
}
