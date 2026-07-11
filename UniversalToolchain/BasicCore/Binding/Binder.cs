namespace BasicCore.Binding;

public sealed class Binder
{
    private readonly BindingContext _context;
    private readonly IReadOnlyList<IAstBindingRule> _rules;

    public Binder(IReadOnlyList<ExternalBinding> externalBindings, IReadOnlyList<IAstBindingRule>? rules = null)
    {
        _context = new BindingContext(externalBindings);
        _rules = rules ?? [];
    }

    public AstNode Bind(AstNode root) => BindNode(root);

    private AstNode BindNode(AstNode node)
    {
        foreach (var rule in _rules)
        {
            if (rule.CanBind(node, _context))
                return rule.Bind(node, _context, BindNode);
        }

        for (var i = 0; i < node.Children.Count; i++)
            node[i] = BindNode(node[i]);

        return node;
    }
}
