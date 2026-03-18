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
    public static readonly AstNodeType UseModulesDirective = AstNodeType.CreateOrGet("UseModulesDirective");
    public static readonly AstNodeType ExcludeModulesDirective = AstNodeType.CreateOrGet("ExcludeModulesDirective");
    public static readonly AstNodeType RequiresModulesDirective = AstNodeType.CreateOrGet("RequiresModulesDirective");
    public static readonly AstNodeType BeforeModulesDirective = AstNodeType.CreateOrGet("BeforeModulesDirective");
    public static readonly AstNodeType AfterModulesDirective = AstNodeType.CreateOrGet("AfterModulesDirective");
    public static readonly AstNodeType BackendDirective = AstNodeType.CreateOrGet("BackendDirective");
    public static readonly AstNodeType AllowIntrinsicDirective = AstNodeType.CreateOrGet("AllowIntrinsicDirective");
    public static readonly AstNodeType ForbidIntrinsicDirective = AstNodeType.CreateOrGet("ForbidIntrinsicDirective");
    public static readonly AstNodeType EnableIntrinsicDirective = AstNodeType.CreateOrGet("EnableIntrinsicDirective");
    public static readonly AstNodeType DisableIntrinsicDirective = AstNodeType.CreateOrGet("DisableIntrinsicDirective");
    public static readonly AstNodeType SecurityDirective = AstNodeType.CreateOrGet("SecurityDirective");
    public static readonly AstNodeType CapabilityDirective = AstNodeType.CreateOrGet("CapabilityDirective");
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
        if (declaration == null)
        {
            Thrower.ArgumentNull(nameof(declaration));
        }

        if (directives == null)
        {
            Thrower.ArgumentNull(nameof(directives));
        }

        var children = new List<AstNode> { declaration };
        children.AddRange(directives.Cast<AstNode>());
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

public abstract class DialectDirectiveAstNode : DialectAstNode
{
    protected DialectDirectiveAstNode(AstNodeType nodeType, IDialectDirectiveFeature feature, LexemeValue? lexemeValue, List<AstNode> children) : base(nodeType, lexemeValue, children)
    {
        if (feature == null)
        {
            Thrower.ArgumentNull(nameof(feature));
        }

        Feature = feature;
    }

    public IDialectDirectiveFeature Feature { get; }

    public DialectDirectiveKind DirectiveKind => Feature.Kind;
}

public abstract class IdentifierListDirectiveAstNode : DialectDirectiveAstNode
{
    protected IdentifierListDirectiveAstNode(AstNodeType nodeType, IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IdentifierListAstNode identifiers) : base(
        nodeType,
        feature,
        lexemeValue,
        [identifiers])
    {
    }

    public IdentifierListAstNode Identifiers => (IdentifierListAstNode)Children[0];
}

public abstract class SingleIdentifierDirectiveAstNode : DialectDirectiveAstNode
{
    protected SingleIdentifierDirectiveAstNode(AstNodeType nodeType, IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IdentifierValueAstNode identifier) : base(
        nodeType,
        feature,
        lexemeValue,
        [identifier])
    {
    }

    public IdentifierValueAstNode Identifier => (IdentifierValueAstNode)Children[0];
}

public sealed class UseModulesDirectiveAstNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IdentifierListAstNode identifiers)
    : IdentifierListDirectiveAstNode(DialectAstNodeTypes.UseModulesDirective, feature, lexemeValue, identifiers);

public sealed class ExcludeModulesDirectiveAstNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IdentifierListAstNode identifiers)
    : IdentifierListDirectiveAstNode(DialectAstNodeTypes.ExcludeModulesDirective, feature, lexemeValue, identifiers);

public sealed class RequiresModulesDirectiveAstNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IdentifierListAstNode identifiers)
    : IdentifierListDirectiveAstNode(DialectAstNodeTypes.RequiresModulesDirective, feature, lexemeValue, identifiers);

public sealed class BeforeModulesDirectiveAstNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IdentifierListAstNode identifiers)
    : IdentifierListDirectiveAstNode(DialectAstNodeTypes.BeforeModulesDirective, feature, lexemeValue, identifiers);

public sealed class AfterModulesDirectiveAstNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IdentifierListAstNode identifiers)
    : IdentifierListDirectiveAstNode(DialectAstNodeTypes.AfterModulesDirective, feature, lexemeValue, identifiers);

public sealed class BackendDirectiveAstNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IdentifierListAstNode identifiers)
    : IdentifierListDirectiveAstNode(DialectAstNodeTypes.BackendDirective, feature, lexemeValue, identifiers);

public sealed class CapabilityDirectiveAstNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IdentifierListAstNode identifiers)
    : IdentifierListDirectiveAstNode(DialectAstNodeTypes.CapabilityDirective, feature, lexemeValue, identifiers);

public sealed class AllowIntrinsicDirectiveAstNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IdentifierValueAstNode identifier)
    : SingleIdentifierDirectiveAstNode(DialectAstNodeTypes.AllowIntrinsicDirective, feature, lexemeValue, identifier);

public sealed class ForbidIntrinsicDirectiveAstNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IdentifierValueAstNode identifier)
    : SingleIdentifierDirectiveAstNode(DialectAstNodeTypes.ForbidIntrinsicDirective, feature, lexemeValue, identifier);

public sealed class EnableIntrinsicDirectiveAstNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IdentifierValueAstNode identifier)
    : SingleIdentifierDirectiveAstNode(DialectAstNodeTypes.EnableIntrinsicDirective, feature, lexemeValue, identifier);

public sealed class DisableIntrinsicDirectiveAstNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IdentifierValueAstNode identifier)
    : SingleIdentifierDirectiveAstNode(DialectAstNodeTypes.DisableIntrinsicDirective, feature, lexemeValue, identifier);

public sealed class SecurityDirectiveAstNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, IdentifierValueAstNode identifier)
    : SingleIdentifierDirectiveAstNode(DialectAstNodeTypes.SecurityDirective, feature, lexemeValue, identifier);

public sealed class IdentifierValueAstNode : DialectAstNode
{
    public IdentifierValueAstNode(LexemeValue lexemeValue) : base(DialectAstNodeTypes.IdentifierValue, lexemeValue, [])
    {
        if (lexemeValue == null)
        {
            Thrower.ArgumentNull(nameof(lexemeValue));
        }
    }

    public string Identifier => Text;
}

public sealed class IdentifierListAstNode : DialectAstNode
{
    public IdentifierListAstNode(IReadOnlyList<IdentifierValueAstNode> identifiers) : base(DialectAstNodeTypes.IdentifierList, null, identifiers.Cast<AstNode>().ToList())
    {
        if (identifiers == null)
        {
            Thrower.ArgumentNull(nameof(identifiers));
        }
    }

    public IReadOnlyList<IdentifierValueAstNode> Identifiers => Children.Cast<IdentifierValueAstNode>().ToList();
}
