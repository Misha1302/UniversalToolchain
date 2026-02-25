using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace UserFunctionsModule.Creators;

public class UserFunctionReturnNodeCreator : IAstNodeCreator
{
    private static readonly ExtensibleEnum<AstNodeTag> ReturnNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Return");

    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ReturnNodeType;

    public bool TryCreateNode(AstNode scope, int childIndex) => TryCreateReturn(scope, childIndex);

    public static bool TryCreateReturn(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != ReturnNodeType)
            return false;

        var expressionNode = scope.SafeGet(childIndex + 1);
        if (expressionNode == null)
            return false;

        var returnNode = scope[childIndex];
        returnNode.Children.Add(expressionNode);
        scope.Children.RemoveAt(childIndex + 1);
        return true;
    }
}
