using ArithmeticModule.Visitors;
using BasicCore.Binding;
using BasicCore.Binding.Symbols;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Semantics;
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

internal enum WistLegacySemanticNodeKind
{
    Plain,
    LocalReference,
    ExternalReference,
    Assignment,
    Call,
    BinaryOperator
}

/// <summary>
/// Explicit compatibility representation for Wist features that have not yet moved to canonical semantic nodes.
/// The mutable compiler AST is snapshotted into immutable data at the semantic boundary so later AST mutation cannot
/// change an already-produced semantic artifact. Bound symbol identity is retained as immutable semantic identity;
/// no frontend module/plugin, optimizer instance, or live AST node crosses the boundary.
/// </summary>
internal sealed class WistLegacySemanticNode : WistSemanticNode
{
    public WistLegacySemanticNode(AstNode boundNode, IEnumerable<WistSemanticNode> children) : base(children)
    {
        ArgumentNullException.ThrowIfNull(boundNode);

        NodeType = boundNode.NodeType;
        LexemeValue = boundNode.LexemeValue;
        CurrentTags = Array.AsReadOnly(boundNode.CurrentTags.OrderBy(static tag => tag, StringComparer.Ordinal).ToArray());
        LocalSemanticTags = Array.AsReadOnly(boundNode.LocalSemanticTags.OrderBy(static tag => tag.Value, StringComparer.Ordinal).ToArray());
        (Kind, Symbol) = boundNode switch
        {
            BoundLocalReference local => (WistLegacySemanticNodeKind.LocalReference, local.Symbol),
            BoundExternalReference external => (WistLegacySemanticNodeKind.ExternalReference, external.Symbol),
            BoundAssignment => (WistLegacySemanticNodeKind.Assignment, null),
            BoundCall => (WistLegacySemanticNodeKind.Call, null),
            BoundBinaryOperator => (WistLegacySemanticNodeKind.BinaryOperator, null),
            _ => (WistLegacySemanticNodeKind.Plain, null)
        };
    }

    public ExtensibleEnum<AstNodeTag> NodeType { get; }
    public LexemeValue? LexemeValue { get; }
    public IReadOnlyList<string> CurrentTags { get; }
    public IReadOnlyList<AstSemanticTagId> LocalSemanticTags { get; }
    public WistLegacySemanticNodeKind Kind { get; }
    public Symbol? Symbol { get; }
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

        return RebuildLegacyNode(legacy, children);
    }

    private static AstNode RebuildLegacyNode(WistLegacySemanticNode source, List<AstNode> children)
    {
        var projectedSource = new AstNode(source.NodeType, source.LexemeValue, children);
        foreach (var tag in source.CurrentTags)
            projectedSource.AddTag(tag);
        foreach (var tag in source.LocalSemanticTags)
            projectedSource.AddSemanticTag(tag);

        return source.Kind switch
        {
            WistLegacySemanticNodeKind.LocalReference =>
                new BoundLocalReference(projectedSource, (LocalVariableSymbol)(source.Symbol ?? throw MissingBoundSymbol(source.Kind))),
            WistLegacySemanticNodeKind.ExternalReference =>
                new BoundExternalReference(projectedSource, source.Symbol ?? throw MissingBoundSymbol(source.Kind)),
            WistLegacySemanticNodeKind.Assignment => new BoundAssignment(projectedSource),
            WistLegacySemanticNodeKind.Call => new BoundCall(projectedSource),
            WistLegacySemanticNodeKind.BinaryOperator => new BoundBinaryOperator(projectedSource),
            WistLegacySemanticNodeKind.Plain => projectedSource,
            _ => throw new InvalidOperationException($"Unsupported Wist legacy semantic node kind '{source.Kind}'.")
        };
    }

    private static InvalidOperationException MissingBoundSymbol(WistLegacySemanticNodeKind kind) =>
        new($"Wist legacy semantic node kind '{kind}' requires a bound symbol snapshot.");
}
