using ArithmeticModule.Visitors;
using BasicCore.Binding;
using BasicCore.Binding.Symbols;
using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace UniversalToolchain.Wist.LanguagePack;

internal readonly record struct WistSemanticOperationId(string Value)
{
    public override string ToString() => Value;
}

internal static class WistSemanticOperations
{
    public static WistSemanticOperationId Add { get; } = new("wist.semantic.arithmetic.add");
}

internal abstract class WistSemanticNode
{
    protected WistSemanticNode(IEnumerable<WistSemanticNode> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        Children = Array.AsReadOnly(children.ToArray());
    }

    public IReadOnlyList<WistSemanticNode> Children { get; }
}

internal sealed class WistSemanticOperationNode : WistSemanticNode
{
    public WistSemanticOperationNode(
        WistSemanticOperationId operation,
        IEnumerable<WistSemanticNode> children) : base(children)
    {
        Operation = operation;
    }

    public WistSemanticOperationId Operation { get; }
}

/// <summary>
/// Explicit compatibility representation for Wist features that have not yet moved to canonical semantic nodes.
/// It keeps the already-bound AST payload as data so symbol identity is not lost while those features are migrated.
/// No frontend module/plugin or optimizer instance crosses the semantic artifact boundary.
/// </summary>
internal sealed class WistLegacySemanticNode : WistSemanticNode
{
    public WistLegacySemanticNode(AstNode boundNode, IEnumerable<WistSemanticNode> children) : base(children)
    {
        BoundNode = boundNode ?? throw new ArgumentNullException(nameof(boundNode));
    }

    public AstNode BoundNode { get; }
}

internal sealed class WistSemanticProgram(WistSemanticNode root)
{
    public WistSemanticNode Root { get; } = root ?? throw new ArgumentNullException(nameof(root));
}

internal static class WistSemanticNormalizer
{
    private static readonly ExtensibleEnum<AstNodeTag> SymbolicAddition = ExtensibleEnum<AstNodeTag>.CreateOrGet("Addition");
    private static readonly ExtensibleEnum<AstNodeTag> TextualAddition = ExtensibleEnum<AstNodeTag>.CreateOrGet("TextualAddition");

    public static WistSemanticProgram Normalize(AstNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return new WistSemanticProgram(NormalizeNode(root));
    }

    public static AstNode ProjectForLegacyLowering(WistSemanticProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        return ProjectNode(program.Root);
    }

    private static WistSemanticNode NormalizeNode(AstNode node)
    {
        var children = node.Children.Select(NormalizeNode).ToArray();
        if (node.NodeType == SymbolicAddition || node.NodeType == TextualAddition)
            return new WistSemanticOperationNode(WistSemanticOperations.Add, children);

        return new WistLegacySemanticNode(node, children);
    }

    private static AstNode ProjectNode(WistSemanticNode node)
    {
        var children = node.Children.Select(ProjectNode).ToList();
        if (node is WistSemanticOperationNode operation)
        {
            if (operation.Operation != WistSemanticOperations.Add)
                throw new InvalidOperationException($"Unsupported Wist semantic operation '{operation.Operation.Value}'.");
            return new AstNode(ArithmeticSemanticLowering.AddNodeType, null, children);
        }

        if (node is not WistLegacySemanticNode legacy)
            throw new InvalidOperationException($"Unsupported Wist semantic node '{node.GetType().FullName}'.");

        return RebuildLegacyNode(legacy.BoundNode, children);
    }

    private static AstNode RebuildLegacyNode(AstNode source, List<AstNode> children)
    {
        var projectedSource = new AstNode(source.NodeType, source.LexemeValue, children);
        foreach (var tag in source.CurrentTags)
            projectedSource.AddTag(tag);
        foreach (var tag in source.LocalSemanticTags)
            projectedSource.AddSemanticTag(tag);

        return source switch
        {
            BoundLocalReference local => new BoundLocalReference(projectedSource, (LocalVariableSymbol)local.Symbol),
            BoundExternalReference external => new BoundExternalReference(projectedSource, external.Symbol),
            BoundAssignment => new BoundAssignment(projectedSource),
            BoundCall => new BoundCall(projectedSource),
            BoundBinaryOperator => new BoundBinaryOperator(projectedSource),
            _ => projectedSource
        };
    }
}
