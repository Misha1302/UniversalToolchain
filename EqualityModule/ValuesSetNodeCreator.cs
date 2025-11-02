// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace EqualityModule;

public class ValuesSetNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("Set");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Equality")) return false;
        if (scope.SafeGet(childIndex - 1)?.NodeType == null) return false;
        if (scope.SafeGet(childIndex + 1)?.NodeType == null) return false;

        var eqNode = scope[childIndex];
        eqNode.Children.AddRange(scope[childIndex - 1], scope[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);
        scope.Children.RemoveAt(childIndex - 1);

// have to load reference to set smth, not a value
        eqNode.Children[^2].AddTag("ExpectingSettableReference");

        return true;
    }
}