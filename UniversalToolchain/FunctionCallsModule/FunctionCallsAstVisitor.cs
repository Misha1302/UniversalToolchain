namespace FunctionCallsModule;

public sealed class FunctionCallsAstVisitor : IAstVisitor
{
    private static readonly AstNodeType FunctionCallNodeType = AstNodeType.CreateOrGet("FunctionCall");
    private static readonly AstNodeType ScopeNodeType = AstNodeType.CreateOrGet("Scope");
    private static readonly AstNodeType CommaNodeType = AstNodeType.CreateOrGet("Comma");

    private readonly FunctionCallPlanner _planner;
    private int _callSequence;

    public FunctionCallsAstVisitor(CapabilityCatalog capabilityCatalog)
    {
        capabilityCatalog = capabilityCatalog.ArgNotNull();

        _planner = new FunctionCallPlanner(capabilityCatalog.BuiltinFunctionRuntimeBindings);
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
        var plan = _planner.PlanOrThrow(functionName, sourceTypes);

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

}
