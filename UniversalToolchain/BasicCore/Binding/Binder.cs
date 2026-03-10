namespace BasicCore.Binding;

public sealed class Binder
{
    private readonly Dictionary<string, Symbol> _externals;
    private readonly Dictionary<string, Symbol> _locals = new();

    public Binder(IReadOnlyList<ExternalBinding> externalBindings)
    {
        _externals = externalBindings
            .Select((binding, slot) => CreateExternalSymbol(binding, slot))
            .ToDictionary(x => x.Name, x => x);
    }

    public AstNode Bind(AstNode root) => BindNode(root);

    private AstNode BindNode(AstNode node)
    {
        if (node.NodeType == AstNodeType.CreateOrGet("Variable"))
            return BindVariable(node);

        for (var i = 0; i < node.Children.Count; i++)
            node[i] = BindNode(node[i]);

        return node;
    }

    private AstNode BindVariable(AstNode node)
    {
        if (node.AllTags.Contains("VariableDefinition"))
        {
            var symbol = new LocalVariableSymbol(node.Text, ResolveVariableType(node));
            _locals[symbol.Name] = symbol;
            return new BoundLocalReference(node, symbol);
        }

        if (_locals.TryGetValue(node.Text, out var local))
            return new BoundLocalReference(node, (LocalVariableSymbol)local);

        if (_externals.TryGetValue(node.Text, out var external))
            return new BoundExternalReference(node, external);

        var inferred = new LocalVariableSymbol(node.Text, typeof(object));
        _locals[inferred.Name] = inferred;
        return new BoundLocalReference(node, inferred);
    }

    private static Symbol CreateExternalSymbol(ExternalBinding binding, int slot) => binding.Kind switch
    {
        ExternalBindingKind.Variable => new ExternalVariableSymbol(binding.Name, binding.Type, slot),
        ExternalBindingKind.Constant => new ExternalConstantSymbol(binding.Name, binding.Type, slot),
        _ => Thrower.InvalidOpEx<Symbol>($"Unsupported external binding kind: {binding.Kind}")
    };

    private static Type ResolveVariableType(AstNode node)
    {
        if (!node.AllTags.Contains("VariableDefinitionWithType"))
            return typeof(object);

        var typeNode = node.Children.LastOrDefault();
        if (typeNode == null)
            return typeof(object);

        return Type.GetType(typeNode.Text) ?? typeof(object);
    }
}