// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using AssemblyFinder;
using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace CSharpInteropModule;

public class CSharpFunctionCallsNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } =
        ExtensibleEnum<AstNodeTag>.CreateOrGet("CSharpFunctionCall");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        var child = scope.Children[childIndex];
        if (child.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Identifier"))
            return false;

        var method = MethodsFinder.GetMethod(child.Text);
        if (method == null) return false;

        child.NodeType = AstNodeType;
        child.Children.Add(scope.Children[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);

        return true;
    }
}