// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace LabelsModule;

public class GotoNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("Goto");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope[childIndex].NodeType != AstNodeType) return false;
        if (scope.Children.Count <= childIndex + 1) return false;
        if (scope[childIndex + 1].NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Identifier")) return false;

        scope[childIndex].Children.Add(scope[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);
        return true;
    }
}