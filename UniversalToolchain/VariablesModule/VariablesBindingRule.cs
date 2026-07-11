using BasicCore.Binding;
using BasicCore.Binding.Symbols;
using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace VariablesModule;

internal sealed class VariablesBindingRule : IAstBindingRule
{
    private static readonly ExtensibleEnum<AstNodeTag> VariableNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Variable");

    public bool CanBind(AstNode node, BindingContext context) => node.NodeType == VariableNodeType;

    public AstNode Bind(AstNode node, BindingContext context, Func<AstNode, AstNode> bindNode)
    {
        if (VariablesAstContracts.IsDefinition(node))
        {
            var symbol = context.DeclareLocal(node.Text, ResolveVariableType(node));
            return new BoundLocalReference(node, symbol);
        }

        if (context.TryGetLocal(node.Text, out var local))
            return new BoundLocalReference(node, local);

        if (context.TryGetExternal(node.Text, out var external))
            return new BoundExternalReference(node, external);

        var inferred = context.GetOrDeclareInferredLocal(node.Text);
        return new BoundLocalReference(node, inferred);
    }

    private static Type ResolveVariableType(AstNode node)
    {
        if (!VariablesAstContracts.HasDeclaredType(node))
            return typeof(object);

        var typeNode = node.Children.LastOrDefault();
        if (typeNode == null)
            return typeof(object);

        return Type.GetType(typeNode.Text) ?? typeof(object);
    }
}
