using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using ExceptionsManager;
using AstNodeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.ParserWrapper.AstNodeTag>;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectAstNodeTypes
{
    public static readonly AstNodeType DialectLine = AstNodeType.CreateOrGet("DialectLine");
    public static readonly AstNodeType DialectDocument = AstNodeType.CreateOrGet("DialectDocument");
    public static readonly AstNodeType DialectDeclaration = AstNodeType.CreateOrGet("DialectDeclaration");
    public static readonly AstNodeType IdentifierValue = AstNodeType.CreateOrGet("IdentifierValue");
    public static readonly AstNodeType IdentifierList = AstNodeType.CreateOrGet("IdentifierList");
    public static readonly AstNodeType DialectDirective = AstNodeType.CreateOrGet("DialectDirective");
}

public abstract class DialectAstNode : AstNode
{
    protected DialectAstNode(AstNodeType nodeType, LexemeValue? lexemeValue, List<AstNode> children) : base(nodeType, lexemeValue, children)
    {
    }
}

public sealed class DialectDocumentAstNode : DialectAstNode
{
    public DialectDocumentAstNode(DialectDeclarationAstNode declaration, IReadOnlyList<DialectDirectiveAstNode> directives) : base(
        DialectAstNodeTypes.DialectDocument,
        null,
        CreateChildren(declaration, directives))
    {
    }

    public DialectDeclarationAstNode Declaration => (DialectDeclarationAstNode)Children[0];

    public IReadOnlyList<DialectDirectiveAstNode> Directives => Children.Skip(1).Cast<DialectDirectiveAstNode>().ToList();

    private static List<AstNode> CreateChildren(DialectDeclarationAstNode declaration, IReadOnlyList<DialectDirectiveAstNode> directives)
    {
        declaration = declaration.ArgNotNull();

        directives = directives.ArgNotNull();

        var children = new List<AstNode> { declaration };
        children.AddRange(directives);
        return children;
    }
}

public sealed class DialectDeclarationAstNode : DialectAstNode
{
    public DialectDeclarationAstNode(IdentifierValueAstNode name) : base(DialectAstNodeTypes.DialectDeclaration, null, [name])
    {
    }

    public IdentifierValueAstNode NameNode => (IdentifierValueAstNode)Children[0];
}

public sealed class DialectDirectiveAstNode : DialectAstNode
{
    public DialectDirectiveAstNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IReadOnlyList<AstNode> payloadNodes) : base(
        DialectAstNodeTypes.DialectDirective,
        lexemeValue,
        CreateChildren(payloadNodes))
    {
        feature = feature.ArgNotNull();

        Feature = feature;
    }

    public IDialectDirectiveFeature Feature { get; }

    public AstNode Payload => Children[0];

    private static List<AstNode> CreateChildren(IReadOnlyList<AstNode> payloadNodes)
    {
        payloadNodes = payloadNodes.ArgNotNull();

        if (payloadNodes.Count != 1)
            Thrower.Argument(nameof(payloadNodes), "Dialect directives must contain exactly one payload node.");

        if (payloadNodes[0] == null)
            Thrower.Argument(nameof(payloadNodes), "Dialect directive payload must not be null.");

        return payloadNodes.ToList();
    }
}

public sealed class IdentifierValueAstNode : DialectAstNode
{
    public IdentifierValueAstNode(LexemeValue lexemeValue) : base(DialectAstNodeTypes.IdentifierValue, lexemeValue, [])
    {
        lexemeValue = lexemeValue.ArgNotNull();
    }

    public string Identifier => Text;
}

public sealed class IdentifierListAstNode : DialectAstNode
{
    public IdentifierListAstNode(IReadOnlyList<IdentifierValueAstNode> identifiers) : base(DialectAstNodeTypes.IdentifierList, null, CreateChildren(identifiers))
    {
    }

    public IReadOnlyList<IdentifierValueAstNode> Identifiers => Children.Cast<IdentifierValueAstNode>().ToList();

    private static List<AstNode> CreateChildren(IReadOnlyList<IdentifierValueAstNode> identifiers)
    {
        identifiers = identifiers.ArgNotNull();

        return identifiers.Cast<AstNode>().ToList();
    }
}