// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace TypeInference.Rules;

public class VariableTypeInferenceRule : ITypeInferenceRule
{
    public int Priority => 900;

    public bool CanTryToInferType(AstNode node, TypeInferenceContext context)
    {
        return node.NodeType == ExtensibleEnum<AstNodeTag>.Get("Variable") ||
               node.NodeType == ExtensibleEnum<AstNodeTag>.Get("Identifier");
    }

    public Type? TryInferType(AstNode node, TypeInferenceContext context)
    {
        var variableName = node.Text;

        return context.TryGetVariableType(variableName, out var type) ? type : null;
    }
}