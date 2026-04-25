using UniversalToolchain.Rules.Abstractions;
using UniversalToolchain.Rules.Core;

namespace BasicCore.Binding;

public sealed class Binder
{
    private readonly Dictionary<string, Symbol> _externals;
    private readonly Dictionary<string, Symbol> _locals = new();
    private readonly List<RuleDiagnostic> _diagnostics = [];
    private Dictionary<string, List<int>> _remainingDefinitionOrdersByName = new(StringComparer.Ordinal);
    private int _visitOrder;

    public Binder(IReadOnlyList<ExternalBinding> externalBindings)
    {
        _externals = externalBindings
            .Select((binding, slot) => CreateExternalSymbol(binding, slot))
            .ToDictionary(x => x.Name, x => x);
    }

    public AstNode Bind(AstNode root)
    {
        root = root.ArgNotNull();

        _locals.Clear();
        _diagnostics.Clear();
        _visitOrder = 0;
        _remainingDefinitionOrdersByName = CollectDefinitionOrders(root);
        var boundRoot = BindNode(root);
        if (_diagnostics.Count > 0)
            Thrower.InvalidOpEx(RuleDiagnosticFormatter.FormatDeterministic(_diagnostics));

        return boundRoot;
    }

    private AstNode BindNode(AstNode node)
    {
        _visitOrder++;

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
            ConsumeDefinitionOrder(node.Text);

            if (_locals.ContainsKey(node.Text))
            {
                AddDiagnostic(
                    RuleDiagnosticCodes.BindingNameConflict,
                    $"Local binding '{node.Text}' is already declared.",
                    node);
            }

            if (_externals.ContainsKey(node.Text))
            {
                AddDiagnostic(
                    RuleDiagnosticCodes.BindingNameConflict,
                    $"Local binding '{node.Text}' cannot shadow a declared external binding.",
                    node);
            }

            var symbol = new LocalVariableSymbol(node.Text, ResolveVariableType(node));
            _locals[symbol.Name] = symbol;
            return new BoundLocalReference(node, symbol);
        }

        if (_locals.TryGetValue(node.Text, out var local))
            return new BoundLocalReference(node, (LocalVariableSymbol)local);

        if (_externals.TryGetValue(node.Text, out var external))
            return new BoundExternalReference(node, external);

        if (HasFutureDeclaration(node.Text))
        {
            AddDiagnostic(
                RuleDiagnosticCodes.UnknownBinding,
                $"Binding '{node.Text}' is used before its declaration.",
                node);
        }
        else
        {
            AddDiagnostic(
                RuleDiagnosticCodes.UnknownBinding,
                $"Binding '{node.Text}' is not declared.",
                node);
        }

        var unresolved = new LocalVariableSymbol(node.Text, typeof(object));
        return new BoundLocalReference(node, unresolved);
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

    private static Dictionary<string, List<int>> CollectDefinitionOrders(AstNode root)
    {
        var order = 0;
        var result = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        Traverse(root);
        return result;

        void Traverse(AstNode node)
        {
            order++;

            if (node.NodeType == AstNodeType.CreateOrGet("Variable")
                && node.AllTags.Contains("VariableDefinition"))
            {
                if (!result.TryGetValue(node.Text, out var orders))
                {
                    orders = [];
                    result[node.Text] = orders;
                }

                orders.Add(order);
            }

            foreach (var child in node.Children)
                Traverse(child);
        }
    }

    private void ConsumeDefinitionOrder(string name)
    {
        if (!_remainingDefinitionOrdersByName.TryGetValue(name, out var orders)
            || orders.Count == 0)
        {
            return;
        }

        orders.RemoveAt(0);
    }

    private bool HasFutureDeclaration(string name)
    {
        if (!_remainingDefinitionOrdersByName.TryGetValue(name, out var orders))
            return false;

        return orders.Any(x => x > _visitOrder);
    }

    private void AddDiagnostic(string code, string message, AstNode node)
    {
        _diagnostics.Add(
            new RuleDiagnostic(
                code,
                RuleDiagnosticSeverity.Error,
                message,
                CreateSourceSpan(node),
                []));
    }

    private static SourceSpan? CreateSourceSpan(AstNode node)
    {
        var lexeme = node.LexemeValue;
        if (lexeme == null || lexeme.LineNumber < 1 || lexeme.CharNumber < 0)
            return null;

        var startColumn = lexeme.CharNumber + 1;
        var endColumn = startColumn + Math.Max(1, lexeme.Text.Length) - 1;
        return new SourceSpan(
            "inline",
            lexeme.LineNumber,
            startColumn,
            lexeme.LineNumber,
            endColumn);
    }
}
