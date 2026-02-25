using BasicCore.ParserWrapper;
using BasicTypesExtensions;
using ExceptionsManager;

namespace ConditionsModule.Core;

public class CondNodesCombiner : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("If");
    public Predicate<AstNode> NeedToVisitPredicate => _ => true;

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("If"))
            return false;

        var condNode = scope.SafeGet(childIndex).NotNull();
        var changed = false;
        while (true)
        {
            var nextNode = scope.SafeGet(childIndex + 1);
            if (nextNode == null) break;

            var type = nextNode.NodeType;
            if (type != ExtensibleEnum<AstNodeTag>.CreateOrGet("Elif") &&
                type != ExtensibleEnum<AstNodeTag>.CreateOrGet("Else"))
                break;

            condNode.Children.Add(nextNode);
            scope.Children.RemoveAt(childIndex + 1);
            condNode = nextNode;
            changed = true;
        }

        return changed;
    }
}