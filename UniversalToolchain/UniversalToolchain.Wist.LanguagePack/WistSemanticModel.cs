using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Semantics;
using BasicTypesExtensions;
using ArithmeticModule.Visitors;

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
/// It is data-only: no frontend module/plugin instance crosses the semantic artifact boundary.
/// </summary>
internal sealed class WistLegacySemanticNode : WistSemanticNode
{
    public WistLegacySemanticNode(
        ExtensibleEnum<AstNodeTag> nodeType,
        LexemeValue? lexemeValue,
        IEnumerable<string> tags,
        IEnumerable<AstSemanticTagId> semanticTags,
        IEnumerable<WistSemanticNode> children) : base(children)
    {
        NodeType = nodeType ?? throw new ArgumentNullException(nameof(nodeType));
        LexemeValue = lexemeValue;
        Tags = Array.AsReadOnly((tags ?? throw new ArgumentNullException(nameof(tags))).ToArray());
        SemanticTags = Array.AsReadOnly((semanticTags ?? throw new ArgumentNullException(nameof(semanticTags))).ToArray());
    }

    public ExtensibleEnum<AstNodeTag> NodeType { get; }
    public LexemeValue? LexemeValue { get; }
    public IReadOnlyList<string> Tags { get; }
    public IReadOnlyList<AstSemanticTagId> SemanticTags { get; }
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

        return new WistLegacySemanticNode(
            node.NodeType,
            node.LexemeValue,
            node.CurrentTags,
            node.LocalSemanticTags,
            children);
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

        var projected = new AstNode(legacy.NodeType, legacy.LexemeValue, children);
        foreach (var tag in legacy.Tags)
            projected.AddTag(tag);
        foreach (var tag in legacy.SemanticTags)
            projected.AddSemanticTag(tag);
        return projected;
    }
}
