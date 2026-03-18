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
    protected DialectDirectiveAstNode(AstNodeType nodeType, DialectDirectiveKind directiveKind, LexemeValue? lexemeValue, List<AstNode> children) : base(nodeType, lexemeValue, children)
    {
        DirectiveKind = directiveKind;
    }

    public DialectDirectiveKind DirectiveKind { get; }
}

public abstract class IdentifierListDirectiveAstNode : DialectDirectiveAstNode
{
    protected IdentifierListDirectiveAstNode(AstNodeType nodeType, DialectDirectiveKind directiveKind, LexemeValue? lexemeValue, IdentifierListAstNode identifiers) : base(
        nodeType,
        directiveKind,
        lexemeValue,
        [identifiers])
    {
    }

    public IdentifierListAstNode Identifiers => (IdentifierListAstNode)Children[0];
}

public abstract class SingleIdentifierDirectiveAstNode : DialectDirectiveAstNode
{
    protected SingleIdentifierDirectiveAstNode(AstNodeType nodeType, DialectDirectiveKind directiveKind, LexemeValue? lexemeValue, IdentifierValueAstNode identifier) : base(
        nodeType,
        directiveKind,
        lexemeValue,
        [identifier])
    {
    }

    public IdentifierValueAstNode Identifier => (IdentifierValueAstNode)Children[0];
}

public sealed class UseModulesDirectiveAstNode(LexemeValue? lexemeValue, IdentifierListAstNode identifiers)
    : IdentifierListDirectiveAstNode(DialectAstNodeTypes.UseModulesDirective, DialectDirectiveKind.UseModules, lexemeValue, identifiers);

public sealed class ExcludeModulesDirectiveAstNode(LexemeValue? lexemeValue, IdentifierListAstNode identifiers)
    : IdentifierListDirectiveAstNode(DialectAstNodeTypes.ExcludeModulesDirective, DialectDirectiveKind.ExcludeModules, lexemeValue, identifiers);

public sealed class RequiresModulesDirectiveAstNode(LexemeValue? lexemeValue, IdentifierListAstNode identifiers)
    : IdentifierListDirectiveAstNode(DialectAstNodeTypes.RequiresModulesDirective, DialectDirectiveKind.RequiresModules, lexemeValue, identifiers);

public sealed class BeforeModulesDirectiveAstNode(LexemeValue? lexemeValue, IdentifierListAstNode identifiers)
    : IdentifierListDirectiveAstNode(DialectAstNodeTypes.BeforeModulesDirective, DialectDirectiveKind.BeforeModules, lexemeValue, identifiers);

public sealed class AfterModulesDirectiveAstNode(LexemeValue? lexemeValue, IdentifierListAstNode identifiers)
    : IdentifierListDirectiveAstNode(DialectAstNodeTypes.AfterModulesDirective, DialectDirectiveKind.AfterModules, lexemeValue, identifiers);

public sealed class BackendDirectiveAstNode(LexemeValue? lexemeValue, IdentifierListAstNode identifiers)
    : IdentifierListDirectiveAstNode(DialectAstNodeTypes.BackendDirective, DialectDirectiveKind.Backend, lexemeValue, identifiers);

public sealed class CapabilityDirectiveAstNode(LexemeValue? lexemeValue, IdentifierListAstNode identifiers)
    : IdentifierListDirectiveAstNode(DialectAstNodeTypes.CapabilityDirective, DialectDirectiveKind.Capability, lexemeValue, identifiers);

public sealed class AllowIntrinsicDirectiveAstNode(LexemeValue? lexemeValue, IdentifierValueAstNode identifier)
    : SingleIdentifierDirectiveAstNode(DialectAstNodeTypes.AllowIntrinsicDirective, DialectDirectiveKind.AllowIntrinsic, lexemeValue, identifier);

public sealed class ForbidIntrinsicDirectiveAstNode(LexemeValue? lexemeValue, IdentifierValueAstNode identifier)
    : SingleIdentifierDirectiveAstNode(DialectAstNodeTypes.ForbidIntrinsicDirective, DialectDirectiveKind.ForbidIntrinsic, lexemeValue, identifier);

public sealed class EnableIntrinsicDirectiveAstNode(LexemeValue? lexemeValue, IdentifierValueAstNode identifier)
    : SingleIdentifierDirectiveAstNode(DialectAstNodeTypes.EnableIntrinsicDirective, DialectDirectiveKind.EnableIntrinsic, lexemeValue, identifier);

public sealed class DisableIntrinsicDirectiveAstNode(LexemeValue? lexemeValue, IdentifierValueAstNode identifier)
    : SingleIdentifierDirectiveAstNode(DialectAstNodeTypes.DisableIntrinsicDirective, DialectDirectiveKind.DisableIntrinsic, lexemeValue, identifier);

public sealed class SecurityDirectiveAstNode(LexemeValue? lexemeValue, IdentifierValueAstNode identifier)
    : SingleIdentifierDirectiveAstNode(DialectAstNodeTypes.SecurityDirective, DialectDirectiveKind.Security, lexemeValue, identifier);

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
