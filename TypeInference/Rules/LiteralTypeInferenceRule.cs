// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicTypesExtensions;
using NumbersModule;

namespace TypeInference.Rules;

public class LiteralTypeInferenceRule : ITypeInferenceRule
{
    public int Priority => 1000;

    public bool CanTryToInferType(AstNode node, TypeInferenceContext context)
    {
        return node.NodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("Number") ||
               node.NodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("String") ||
               node.NodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("True") ||
               node.NodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("False");
    }

    public Type? TryInferType(AstNode node, TypeInferenceContext context)
    {
        var nodeType = node.NodeType;

        // TODO: infer from bytecode
        if (nodeType == ExtensibleEnum<AstNodeTag>.Get("Number"))
            return typeof(RealNumberImpl);

        if (nodeType == ExtensibleEnum<AstNodeTag>.Get("String"))
            return typeof(string);

        if (nodeType == ExtensibleEnum<AstNodeTag>.Get("True") ||
            nodeType == ExtensibleEnum<AstNodeTag>.Get("False"))
            return typeof(bool);

        return null;
    }
}