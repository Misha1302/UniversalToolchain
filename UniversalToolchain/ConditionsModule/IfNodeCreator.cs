using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace ConditionsModule;

public class IfNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("If");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("If"))
            return false;

        var ifNode = scope[childIndex];

        // Следующий узел должен быть условием (scope)
        if (scope.SafeGet(childIndex + 1) == null)
            return false;

        // Добавляем условие как дочерний узел
        ifNode.Children.Add(scope[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);

        // Добавляем тело if (следующий scope)
        if (scope.SafeGet(childIndex + 1) != null)
        {
            ifNode.Children.Add(scope[childIndex + 1]);
            scope.Children.RemoveAt(childIndex + 1);
        }

        return true;
    }
}