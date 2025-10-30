// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com
using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace VariablesModule;

public class VariablesNodeCreator : IAstNodeCreator
{
    private readonly HashSet<string> _variables = [];

    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } =
        ExtensibleEnum<AstNodeTag>.CreateOrGet("Variable");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Identifier"))
            return false;

        if (scope.SafeGet(childIndex + 1)?.NodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("Colon")
            && scope.SafeGet(childIndex + 2)?.NodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("Identifier"))
        {
            scope[childIndex].Children.Add(scope[childIndex + 1]);
            scope[childIndex].Children.Add(scope[childIndex + 2]);
            scope[childIndex].NodeType = AstNodeType;
            scope[childIndex].AddTag("VariableDefinition");
            scope.Children.RemoveAt(childIndex + 2);
            scope.Children.RemoveAt(childIndex + 1);

            _variables.Add(scope[childIndex].Text);
        }
        else
        {
            if (_variables.Contains(scope[childIndex].Text))
                scope[childIndex].NodeType = AstNodeType;
            else return false;
        }

        return true;
    }
}