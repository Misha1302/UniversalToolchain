// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace ArithmeticModule;

public abstract class BinaryOperationBase : IAstNodeCreator
{
    public abstract ExtensibleEnum<AstNodeTag> AstNodeType { get; }

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        var child = scope.Children[childIndex];
        if (child.NodeType != AstNodeType) return false;

        child.Children.Add(scope.Children[childIndex - 1]);
        child.Children.Add(scope.Children[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);
        scope.Children.RemoveAt(childIndex - 1);

        return true;
    }
}