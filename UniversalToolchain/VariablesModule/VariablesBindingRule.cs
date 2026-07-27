using BasicCore.Binding;
using BasicCore.Binding.Symbols;
using BasicCore.ParserWrapper;
using BasicTypesExtensions;
using ExceptionsManager;

namespace VariablesModule;

internal sealed class VariablesBindingRule : IAstBindingRule
{
    private static readonly ExtensibleEnum<AstNodeTag> VariableNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Variable");
    private static readonly ExtensibleEnum<AstNodeTag> GotoNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Goto");

    private static readonly IReadOnlyDictionary<string, Type> AllowedDeclaredTypes =
        new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["bool"] = typeof(bool),
            ["byte"] = typeof(byte),
            ["sbyte"] = typeof(sbyte),
            ["short"] = typeof(short),
            ["ushort"] = typeof(ushort),
            ["int"] = typeof(int),
            ["uint"] = typeof(uint),
            ["long"] = typeof(long),
            ["ulong"] = typeof(ulong),
            ["float"] = typeof(float),
            ["double"] = typeof(double),
            ["decimal"] = typeof(decimal),
            ["char"] = typeof(char),
            ["string"] = typeof(string),
            ["object"] = typeof(object),
            ["System.Boolean"] = typeof(bool),
            ["System.Byte"] = typeof(byte),
            ["System.SByte"] = typeof(sbyte),
            ["System.Int16"] = typeof(short),
            ["System.UInt16"] = typeof(ushort),
            ["System.Int32"] = typeof(int),
            ["System.UInt32"] = typeof(uint),
            ["System.Int64"] = typeof(long),
            ["System.UInt64"] = typeof(ulong),
            ["System.Single"] = typeof(float),
            ["System.Double"] = typeof(double),
            ["System.Decimal"] = typeof(decimal),
            ["System.Char"] = typeof(char),
            ["System.String"] = typeof(string),
            ["System.Object"] = typeof(object)
        };

    public bool CanBind(AstNode node, BindingContext context) =>
        node.NodeType == VariableNodeType &&
        !(node.Text.StartsWith('@') && node.Parent?.NodeType == GotoNodeType);

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

        return Thrower.InvalidOpEx<AstNode>(
            $"Unknown identifier '{node.Text}'. Variables must be declared with 'let' or supplied as an explicit external binding.");
    }

    private static Type ResolveVariableType(AstNode node)
    {
        if (!VariablesAstContracts.HasDeclaredType(node))
            return typeof(object);

        var typeName = node.Children.LastOrDefault()?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(typeName))
            return Thrower.InvalidOpEx<Type>($"Variable '{node.Text}' declares an empty type name.");

        if (typeName.Contains(',', StringComparison.Ordinal))
        {
            return Thrower.InvalidOpEx<Type>(
                $"Assembly-qualified declared type '{typeName}' is not allowed. Use a supported primitive type name.");
        }

        if (AllowedDeclaredTypes.TryGetValue(typeName, out var resolvedType))
            return resolvedType;

        return Thrower.InvalidOpEx<Type>(
            $"Unknown declared type '{typeName}' for variable '{node.Text}'. Only the deterministic primitive type catalog is allowed.");
    }
}
