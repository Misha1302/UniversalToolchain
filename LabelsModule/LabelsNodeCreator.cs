// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace LabelsModule;

public class LabelsNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("Label");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope[childIndex].NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Identifier"))
            return false;
        if (childIndex + 1 >= scope.Children.Count)
            return false;
        if (scope[childIndex + 1].NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Colon"))
            return false;

        scope[childIndex].NodeType = AstNodeType;
        scope[childIndex].Children.Add(scope[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);
        return true;
    }
}