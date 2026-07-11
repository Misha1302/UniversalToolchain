namespace BasicCore.Binding;

public interface IAstBindingRule
{
    bool CanBind(AstNode node, BindingContext context);

    AstNode Bind(AstNode node, BindingContext context, Func<AstNode, AstNode> bindNode);
}
