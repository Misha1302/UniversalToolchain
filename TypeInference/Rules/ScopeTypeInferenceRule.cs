// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace TypeInference.Rules;

public class ScopeTypeInferenceRule : ITypeInferenceRule
{
    public int Priority => 100;

    public bool CanTryToInferType(AstNode node, TypeInferenceContext context)
    {
        return node.NodeType == ExtensibleEnum<AstNodeTag>.Get("Scope");
    }

    public Type? TryInferType(AstNode node, TypeInferenceContext context)
    {
        if (node.Children.Count > 0)
        {
            var resolver = new ExpressionTypeResolver();
            var childContext = context.CreateChildContext();
            return resolver.ResolveExpressionType(node.Children[^1], childContext);
        }

        return typeof(void);
    }
}