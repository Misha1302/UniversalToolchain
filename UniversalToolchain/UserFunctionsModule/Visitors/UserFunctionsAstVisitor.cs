using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using ExceptionsManager;

namespace UserFunctionsModule;

public class UserFunctionsAstVisitor : IAstVisitor
{
    private readonly Dictionary<string, UserFunctionDefinition> _functions = [];

    public void TryVisit(BytecodeVisitorData data)
    {
        var nodeType = data.Node.NodeType;

        if (nodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("FunctionDeclaration"))
        {
            RegisterFunction(data.Node);
            return;
        }

        if (nodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("UserFunctionCall"))
        {
            TranslateFunctionCall(data);
            return;
        }

        if (nodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("Return"))
            Thrower.InvalidOpEx("'return' can be used only inside a function body.");
    }

    private void RegisterFunction(AstNode declarationNode)
    {
        var functionName = declarationNode.Children[0].Text;
        var parameters = AstInliningHelper.ExtractParameters(declarationNode.Children[1]);
        var bodyScope = declarationNode.Children[2];

        Thrower.AssertAlways(bodyScope.Children.Count == 1, $"Function '{functionName}' body must contain exactly one statement in MVP implementation.");
        var returnNode = bodyScope.Children[0];
        Thrower.AssertAlways(returnNode.NodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("Return"),
            $"Function '{functionName}' must end with explicit return in MVP implementation.");

        _functions[functionName] = new UserFunctionDefinition(functionName, parameters, returnNode.Children[0]);
    }

    private void TranslateFunctionCall(BytecodeVisitorData data)
    {
        var functionName = data.Node.Text;
        Thrower.AssertAlways(_functions.TryGetValue(functionName, out var function), $"Unknown user function '{functionName}'.");

        var argsScope = data.Node.Children[0];
        var arguments = AstInliningHelper.ExtractArguments(argsScope).ToList();
        Thrower.AssertAlways(arguments.Count == function.Parameters.Count,
            $"Function '{functionName}' expects {function.Parameters.Count} args, got {arguments.Count}.");

        var substitutions = new Dictionary<string, AstNode>(StringComparer.Ordinal);
        for (var i = 0; i < function.Parameters.Count; i++)
            substitutions[function.Parameters[i]] = arguments[i];

        var inlinedExpression = AstInliningHelper.CloneWithSubstitution(function.ReturnExpression, substitutions);
        data.AstToBytecodeTranslator.Translate(inlinedExpression);
    }

    private sealed record UserFunctionDefinition(string Name, IReadOnlyList<string> Parameters, AstNode ReturnExpression);
}