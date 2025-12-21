using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace ConditionsModule;

public class ElifNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("Elif");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Elif"))
            return false;

        var elifNode = scope[childIndex];

        // Следующий узел должен быть условием (scope)
        if (scope.SafeGet(childIndex + 1) == null)
            return false;

        // Добавляем условие как дочерний узел
        elifNode.Children.Add(scope[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);

        // Добавляем тело elif (следующий scope)
        if (scope.SafeGet(childIndex + 1) != null)
        {
            elifNode.Children.Add(scope[childIndex + 1]);
            scope.Children.RemoveAt(childIndex + 1);
        }

        return true;
    }
}