// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace ConditionsModule;

public class ComparisonNodeCreator(string nodeType) : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet(nodeType);

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != AstNodeType)
            return false;

        var opNode = scope[childIndex];

        // Левый операнд
        if (scope.SafeGet(childIndex - 1) != null)
        {
            opNode.Children.Add(scope[childIndex - 1]);
            scope.Children.RemoveAt(childIndex - 1);
            childIndex--; // Индекс изменился после удаления
        }

        // Правый операнд
        if (scope.SafeGet(childIndex + 1) != null)
        {
            opNode.Children.Add(scope[childIndex + 1]);
            scope.Children.RemoveAt(childIndex + 1);
        }

        opNode.NodeType = AstNodeType;
        return true;
    }
}