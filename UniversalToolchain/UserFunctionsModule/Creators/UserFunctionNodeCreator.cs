using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace UserFunctionsModule.Creators;

public class UserFunctionNodeCreator : IAstNodeCreator
{
    public UserFunctionNodeCreator(bool handleReturnNodes = true)
    {
        _handleReturnNodes = handleReturnNodes;
    }

    private static readonly ExtensibleEnum<AstNodeTag> FnNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Fn");
    private static readonly ExtensibleEnum<AstNodeTag> IdentifierNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Identifier");
    private static readonly ExtensibleEnum<AstNodeTag> ScopeNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope");
    private readonly HashSet<string> _declaredFunctions = [];
    private readonly bool _handleReturnNodes;

    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("FunctionDeclaration");

    public bool TryCreateNode(AstNode scope, int childIndex) =>
        TryCreateFunctionDeclaration(scope, childIndex)
        || (_handleReturnNodes && UserFunctionReturnNodeCreator.TryCreateReturn(scope, childIndex))
        || TryCreateFunctionCall(scope, childIndex);

    private bool TryCreateFunctionDeclaration(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != FnNodeType)
            return false;

        var nameNode = scope.SafeGet(childIndex + 1);
        var argsNode = scope.SafeGet(childIndex + 2);
        var bodyNode = scope.SafeGet(childIndex + 3);

        if (nameNode?.NodeType != IdentifierNodeType || argsNode?.NodeType != ScopeNodeType || bodyNode?.NodeType != ScopeNodeType)
            return false;

        var fnNode = scope[childIndex];
        fnNode.NodeType = AstNodeType;
        fnNode.Children.Add(nameNode);
        fnNode.Children.Add(argsNode);
        fnNode.Children.Add(bodyNode);

        _declaredFunctions.Add(nameNode.Text);

        scope.Children.RemoveAt(childIndex + 3);
        scope.Children.RemoveAt(childIndex + 2);
        scope.Children.RemoveAt(childIndex + 1);
        return true;
    }

    private bool TryCreateFunctionCall(AstNode scope, int childIndex)
    {
        var identifierNode = scope.SafeGet(childIndex);
        var argsNode = scope.SafeGet(childIndex + 1);

        if (scope.NodeType == AstNodeType)
            return false;

        if (identifierNode?.NodeType != IdentifierNodeType || argsNode?.NodeType != ScopeNodeType)
            return false;

        DiscoverFunctionNames(scope);
        if (!_declaredFunctions.Contains(identifierNode.Text))
            return false;

        identifierNode.NodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("UserFunctionCall");
        identifierNode.Children.Add(argsNode);
        scope.Children.RemoveAt(childIndex + 1);
        return true;
    }
    private void DiscoverFunctionNames(AstNode scope)
    {
        for (var i = 0; i <= scope.Children.Count - 4; i++)
        {
            if (scope.Children[i].NodeType == FnNodeType &&
                scope.Children[i + 1].NodeType == IdentifierNodeType &&
                scope.Children[i + 2].NodeType == ScopeNodeType &&
                scope.Children[i + 3].NodeType == ScopeNodeType)
            {
                _declaredFunctions.Add(scope.Children[i + 1].Text);
            }
        }
    }

}
