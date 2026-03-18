using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using ExceptionsManager;
using AstNodeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.ParserWrapper.AstNodeTag>;

namespace UniversalToolchain.Dialects.Frontend;

internal static class DialectNodeCreatorSupport
{
    private static readonly AstNodeType ScopeType = AstNodeType.CreateOrGet("Scope");

    public static bool IsScope(AstNode scope)
    {
        return scope.NodeType == ScopeType;
    }

    public static bool IsDialectLine(AstNode node)
    {
        return node.NodeType == DialectAstNodeTypes.DialectLine;
    }

    public static bool IsNewLineToken(AstNode node)
    {
        return DialectLexemeTags.IsTag(node.LexemeValue, DialectLexemeTags.NewLine);
    }

    public static bool IsCommaToken(AstNode node)
    {
        return DialectLexemeTags.IsTag(node.LexemeValue, DialectLexemeTags.CommaToken);
    }

    public static bool IsIdentifierToken(AstNode node)
    {
        return DialectLexemeTags.IsTag(node.LexemeValue, DialectLexemeTags.Identifier);
    }

    public static AstNode GetChild(AstNode scope, int childIndex)
    {
        if (scope == null)
        {
            Thrower.ArgumentNull(nameof(scope));
        }

        if (childIndex < 0 || childIndex >= scope.Children.Count)
        {
            Thrower.ArgumentOutOfRange<object>(nameof(childIndex), "Child index is out of range for the current parser scope.");
        }

        return scope.Children[childIndex];
    }

    public static string GetKeywordText(AstNode lineNode)
    {
        if (lineNode.Children.Count == 0 || lineNode.Children[0].LexemeValue == null)
        {
            return string.Empty;
        }

        return lineNode.Children[0].Text;
    }

    public static IdentifierValueAstNode ParseSingleIdentifier(AstNode lineNode, string directiveDisplayName)
    {
        if (lineNode == null)
        {
            Thrower.ArgumentNull(nameof(lineNode));
        }

        if (lineNode.Children.Count != 2)
        {
            DialectDefinitionSliceParseErrors.Fail(
                $"Directive '{directiveDisplayName}' expects exactly one identifier argument.",
                lineNode.Children[0].LexemeValue);
        }

        var identifierNode = lineNode.Children[1];
        if (!IsIdentifierToken(identifierNode))
        {
            DialectDefinitionSliceParseErrors.Fail(
                $"Directive '{directiveDisplayName}' expects an identifier argument.",
                identifierNode.LexemeValue);
        }

        return new IdentifierValueAstNode(identifierNode.LexemeValue!);
    }

    public static IdentifierListAstNode ParseIdentifierList(AstNode lineNode, string directiveDisplayName)
    {
        if (lineNode == null)
        {
            Thrower.ArgumentNull(nameof(lineNode));
        }

        if (lineNode.Children.Count < 2)
        {
            DialectDefinitionSliceParseErrors.Fail(
                $"Directive '{directiveDisplayName}' expects at least one identifier.",
                lineNode.Children[0].LexemeValue);
        }

        var identifiers = new List<IdentifierValueAstNode>();
        var expectIdentifier = true;

        for (var i = 1; i < lineNode.Children.Count; i++)
        {
            var current = lineNode.Children[i];
            if (expectIdentifier)
            {
                if (!IsIdentifierToken(current))
                {
                    DialectDefinitionSliceParseErrors.Fail(
                        $"Directive '{directiveDisplayName}' contains an invalid identifier list item.",
                        current.LexemeValue);
                }

                identifiers.Add(new IdentifierValueAstNode(current.LexemeValue!));
                expectIdentifier = false;
                continue;
            }

            if (!IsCommaToken(current))
            {
                DialectDefinitionSliceParseErrors.Fail(
                    $"Directive '{directiveDisplayName}' expects comma-separated identifiers.",
                    current.LexemeValue);
            }

            expectIdentifier = true;
        }

        if (expectIdentifier)
        {
            DialectDefinitionSliceParseErrors.Fail(
                $"Directive '{directiveDisplayName}' must not end with a trailing comma.",
                lineNode.Children[^1].LexemeValue);
        }

        return new IdentifierListAstNode(identifiers);
    }
}

public sealed class DialectLineNodeCreator : IAstNodeCreator
{
    public AstNodeType AstNodeType => DialectAstNodeTypes.DialectLine;

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope == null || !DialectNodeCreatorSupport.IsScope(scope) || childIndex < 0 || childIndex >= scope.Children.Count)
        {
            return false;
        }

        var current = scope.Children[childIndex];
        if (DialectNodeCreatorSupport.IsDialectLine(current))
        {
            return false;
        }

        if (DialectNodeCreatorSupport.IsNewLineToken(current))
        {
            scope.Children.RemoveAt(childIndex);
            return true;
        }

        if (childIndex > 0)
        {
            var previous = scope.Children[childIndex - 1];
            if (!DialectNodeCreatorSupport.IsNewLineToken(previous) && !DialectNodeCreatorSupport.IsDialectLine(previous))
            {
                return false;
            }
        }

        var end = childIndex;
        while (end < scope.Children.Count && !DialectNodeCreatorSupport.IsNewLineToken(scope.Children[end]))
        {
            end++;
        }

        var lineChildren = new List<AstNode>();
        for (var i = childIndex; i < end; i++)
        {
            lineChildren.Add(scope.Children[i]);
        }

        var removeCount = end - childIndex;
        if (end < scope.Children.Count && DialectNodeCreatorSupport.IsNewLineToken(scope.Children[end]))
        {
            removeCount++;
        }

        scope.Children.RemoveRange(childIndex, removeCount);
        scope.Children.Insert(childIndex, new AstNode(DialectAstNodeTypes.DialectLine, null, lineChildren));
        return true;
    }
}

public abstract class DialectLineConstructNodeCreator : IAstNodeCreator
{
    public abstract AstNodeType AstNodeType { get; }

    protected abstract string Keyword { get; }

    protected abstract AstNode CreateNode(AstNode lineNode);

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope == null || !DialectNodeCreatorSupport.IsScope(scope) || childIndex < 0 || childIndex >= scope.Children.Count)
        {
            return false;
        }

        var candidate = scope.Children[childIndex];
        if (!DialectNodeCreatorSupport.IsDialectLine(candidate))
        {
            return false;
        }

        if (!string.Equals(DialectNodeCreatorSupport.GetKeywordText(candidate), Keyword, StringComparison.Ordinal))
        {
            return false;
        }

        scope.Children[childIndex] = CreateNode(candidate);
        return true;
    }
}

public sealed class DialectDeclarationNodeCreator : DialectLineConstructNodeCreator
{
    public override AstNodeType AstNodeType => DialectAstNodeTypes.DialectDeclaration;

    protected override string Keyword => DialectDslKeywords.Dialect;

    protected override AstNode CreateNode(AstNode lineNode)
    {
        if (lineNode.Children.Count != 2)
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect declaration must have the form 'dialect <name>'.", lineNode.Children[0].LexemeValue);
        }

        var nameToken = lineNode.Children[1];
        if (!DialectNodeCreatorSupport.IsIdentifierToken(nameToken))
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect declaration expects an identifier name.", nameToken.LexemeValue);
        }

        return new DialectDeclarationAstNode(new IdentifierValueAstNode(nameToken.LexemeValue!));
    }
}

public abstract class IdentifierListDirectiveNodeCreator<TNode> : DialectLineConstructNodeCreator where TNode : AstNode
{
    protected override AstNode CreateNode(AstNode lineNode)
    {
        return CreateTypedNode(lineNode, DialectNodeCreatorSupport.ParseIdentifierList(lineNode, Keyword));
    }

    protected abstract TNode CreateTypedNode(AstNode lineNode, IdentifierListAstNode identifiers);
}

public abstract class SingleIdentifierDirectiveNodeCreator<TNode> : DialectLineConstructNodeCreator where TNode : AstNode
{
    protected override AstNode CreateNode(AstNode lineNode)
    {
        return CreateTypedNode(lineNode, DialectNodeCreatorSupport.ParseSingleIdentifier(lineNode, Keyword));
    }

    protected abstract TNode CreateTypedNode(AstNode lineNode, IdentifierValueAstNode identifier);
}

public class IdentifierListDialectDirectiveNodeCreator : IdentifierListDirectiveNodeCreator<DialectDirectiveAstNode>
{
    private readonly IDialectDirectiveFeature _feature;
    private readonly Func<LexemeValue?, IdentifierListAstNode, DialectDirectiveAstNode> _factory;

    public IdentifierListDialectDirectiveNodeCreator(IDialectDirectiveFeature feature)
    {
        if (feature == null)
        {
            Thrower.ArgumentNull(nameof(feature));
        }

        if (feature.ArgumentShape != DialectDirectiveArgumentShape.IdentifierList)
        {
            Thrower.Argument(nameof(feature), $"Feature '{feature.Keyword}' must use identifier-list syntax.");
        }

        _feature = feature;
        _factory = DialectDirectiveNodeFactory.CreateIdentifierListFactory(feature);
    }

    public override AstNodeType AstNodeType => DialectDirectiveNodeFactory.GetNodeType(_feature.Kind);

    protected override string Keyword => _feature.Keyword;

    protected override DialectDirectiveAstNode CreateTypedNode(AstNode lineNode, IdentifierListAstNode identifiers) => _factory(lineNode.Children[0].LexemeValue, identifiers);
}

public class SingleIdentifierDialectDirectiveNodeCreator : SingleIdentifierDirectiveNodeCreator<DialectDirectiveAstNode>
{
    private readonly IDialectDirectiveFeature _feature;
    private readonly Func<LexemeValue?, IdentifierValueAstNode, DialectDirectiveAstNode> _factory;

    public SingleIdentifierDialectDirectiveNodeCreator(IDialectDirectiveFeature feature)
    {
        if (feature == null)
        {
            Thrower.ArgumentNull(nameof(feature));
        }

        if (feature.ArgumentShape != DialectDirectiveArgumentShape.Identifier)
        {
            Thrower.Argument(nameof(feature), $"Feature '{feature.Keyword}' must use single-identifier syntax.");
        }

        _feature = feature;
        _factory = DialectDirectiveNodeFactory.CreateSingleIdentifierFactory(feature);
    }

    public override AstNodeType AstNodeType => DialectDirectiveNodeFactory.GetNodeType(_feature.Kind);

    protected override string Keyword => _feature.Keyword;

    protected override DialectDirectiveAstNode CreateTypedNode(AstNode lineNode, IdentifierValueAstNode identifier) => _factory(lineNode.Children[0].LexemeValue, identifier);
}

internal static class DialectDirectiveNodeFactory
{
    public static AstNodeType GetNodeType(DialectDirectiveKind kind)
    {
        return kind switch
        {
            DialectDirectiveKind.UseModules => DialectAstNodeTypes.UseModulesDirective,
            DialectDirectiveKind.ExcludeModules => DialectAstNodeTypes.ExcludeModulesDirective,
            DialectDirectiveKind.RequiresModules => DialectAstNodeTypes.RequiresModulesDirective,
            DialectDirectiveKind.BeforeModules => DialectAstNodeTypes.BeforeModulesDirective,
            DialectDirectiveKind.AfterModules => DialectAstNodeTypes.AfterModulesDirective,
            DialectDirectiveKind.Backend => DialectAstNodeTypes.BackendDirective,
            DialectDirectiveKind.AllowIntrinsic => DialectAstNodeTypes.AllowIntrinsicDirective,
            DialectDirectiveKind.ForbidIntrinsic => DialectAstNodeTypes.ForbidIntrinsicDirective,
            DialectDirectiveKind.EnableIntrinsic => DialectAstNodeTypes.EnableIntrinsicDirective,
            DialectDirectiveKind.DisableIntrinsic => DialectAstNodeTypes.DisableIntrinsicDirective,
            DialectDirectiveKind.Security => DialectAstNodeTypes.SecurityDirective,
            DialectDirectiveKind.Capability => DialectAstNodeTypes.CapabilityDirective,
            _ => Thrower.InvalidOpEx<AstNodeType>($"Unknown dialect directive kind '{kind}'.")
        };
    }

    public static Func<LexemeValue?, IdentifierListAstNode, DialectDirectiveAstNode> CreateIdentifierListFactory(IDialectDirectiveFeature feature)
    {
        return feature.Kind switch
        {
            DialectDirectiveKind.UseModules => (lexeme, identifiers) => new UseModulesDirectiveAstNode(feature, lexeme, identifiers),
            DialectDirectiveKind.ExcludeModules => (lexeme, identifiers) => new ExcludeModulesDirectiveAstNode(feature, lexeme, identifiers),
            DialectDirectiveKind.RequiresModules => (lexeme, identifiers) => new RequiresModulesDirectiveAstNode(feature, lexeme, identifiers),
            DialectDirectiveKind.BeforeModules => (lexeme, identifiers) => new BeforeModulesDirectiveAstNode(feature, lexeme, identifiers),
            DialectDirectiveKind.AfterModules => (lexeme, identifiers) => new AfterModulesDirectiveAstNode(feature, lexeme, identifiers),
            DialectDirectiveKind.Backend => (lexeme, identifiers) => new BackendDirectiveAstNode(feature, lexeme, identifiers),
            DialectDirectiveKind.Capability => (lexeme, identifiers) => new CapabilityDirectiveAstNode(feature, lexeme, identifiers),
            _ => Thrower.InvalidOpEx<Func<LexemeValue?, IdentifierListAstNode, DialectDirectiveAstNode>>($"Feature '{feature.Keyword}' does not support identifier-list directives.")
        };
    }

    public static Func<LexemeValue?, IdentifierValueAstNode, DialectDirectiveAstNode> CreateSingleIdentifierFactory(IDialectDirectiveFeature feature)
    {
        return feature.Kind switch
        {
            DialectDirectiveKind.AllowIntrinsic => (lexeme, identifier) => new AllowIntrinsicDirectiveAstNode(feature, lexeme, identifier),
            DialectDirectiveKind.ForbidIntrinsic => (lexeme, identifier) => new ForbidIntrinsicDirectiveAstNode(feature, lexeme, identifier),
            DialectDirectiveKind.EnableIntrinsic => (lexeme, identifier) => new EnableIntrinsicDirectiveAstNode(feature, lexeme, identifier),
            DialectDirectiveKind.DisableIntrinsic => (lexeme, identifier) => new DisableIntrinsicDirectiveAstNode(feature, lexeme, identifier),
            DialectDirectiveKind.Security => (lexeme, identifier) => new SecurityDirectiveAstNode(feature, lexeme, identifier),
            _ => Thrower.InvalidOpEx<Func<LexemeValue?, IdentifierValueAstNode, DialectDirectiveAstNode>>($"Feature '{feature.Keyword}' does not support single-identifier directives.")
        };
    }
}

public sealed class UseModulesDirectiveNodeCreator() : IdentifierListDialectDirectiveNodeCreator(DialectDslFeatureCatalog.GetFeature(DialectDirectiveKind.UseModules));

public sealed class ExcludeModulesDirectiveNodeCreator() : IdentifierListDialectDirectiveNodeCreator(DialectDslFeatureCatalog.GetFeature(DialectDirectiveKind.ExcludeModules));

public sealed class RequiresModulesDirectiveNodeCreator() : IdentifierListDialectDirectiveNodeCreator(DialectDslFeatureCatalog.GetFeature(DialectDirectiveKind.RequiresModules));

public sealed class BeforeModulesDirectiveNodeCreator() : IdentifierListDialectDirectiveNodeCreator(DialectDslFeatureCatalog.GetFeature(DialectDirectiveKind.BeforeModules));

public sealed class AfterModulesDirectiveNodeCreator() : IdentifierListDialectDirectiveNodeCreator(DialectDslFeatureCatalog.GetFeature(DialectDirectiveKind.AfterModules));

public sealed class BackendDirectiveNodeCreator() : IdentifierListDialectDirectiveNodeCreator(DialectDslFeatureCatalog.GetFeature(DialectDirectiveKind.Backend));

public sealed class AllowIntrinsicDirectiveNodeCreator() : SingleIdentifierDialectDirectiveNodeCreator(DialectDslFeatureCatalog.GetFeature(DialectDirectiveKind.AllowIntrinsic));

public sealed class ForbidIntrinsicDirectiveNodeCreator() : SingleIdentifierDialectDirectiveNodeCreator(DialectDslFeatureCatalog.GetFeature(DialectDirectiveKind.ForbidIntrinsic));

public sealed class EnableIntrinsicDirectiveNodeCreator() : SingleIdentifierDialectDirectiveNodeCreator(DialectDslFeatureCatalog.GetFeature(DialectDirectiveKind.EnableIntrinsic));

public sealed class DisableIntrinsicDirectiveNodeCreator() : SingleIdentifierDialectDirectiveNodeCreator(DialectDslFeatureCatalog.GetFeature(DialectDirectiveKind.DisableIntrinsic));

public sealed class SecurityDirectiveNodeCreator() : SingleIdentifierDialectDirectiveNodeCreator(DialectDslFeatureCatalog.GetFeature(DialectDirectiveKind.Security));

public sealed class CapabilityDirectiveNodeCreator() : IdentifierListDialectDirectiveNodeCreator(DialectDslFeatureCatalog.GetFeature(DialectDirectiveKind.Capability));

public sealed class DialectDocumentNodeCreator : IAstNodeCreator
{
    public AstNodeType AstNodeType => DialectAstNodeTypes.DialectDocument;

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope == null || !DialectNodeCreatorSupport.IsScope(scope) || childIndex != 0)
        {
            return false;
        }

        if (scope.Children.Count == 0)
        {
            return false;
        }

        if (scope.Children.Count == 1 && scope.Children[0] is DialectDocumentAstNode)
        {
            return false;
        }

        if (scope.Children.Any(x => DialectNodeCreatorSupport.IsDialectLine(x)))
        {
            var lineNode = scope.Children.First(x => DialectNodeCreatorSupport.IsDialectLine(x));
            var keyword = DialectNodeCreatorSupport.GetKeywordText(lineNode);
            var message = string.IsNullOrWhiteSpace(keyword)
                ? "Encountered an empty directive line."
                : $"Unknown dialect directive '{keyword}'.";
            DialectDefinitionSliceParseErrors.Fail(message, lineNode.Children.FirstOrDefault()?.LexemeValue);
        }

        if (scope.Children.Any(x => x.NodeType != DialectAstNodeTypes.DialectDeclaration && x is not DialectDirectiveAstNode))
        {
            var invalidNode = scope.Children.First(x => x.NodeType != DialectAstNodeTypes.DialectDeclaration && x is not DialectDirectiveAstNode);
            DialectDefinitionSliceParseErrors.Fail(
                $"Dialect parser produced an unexpected AST node of type '{invalidNode.NodeType.GetName()}'.",
                invalidNode.LexemeValue ?? invalidNode.Children.FirstOrDefault()?.LexemeValue);
        }

        var declarations = scope.Children.OfType<DialectDeclarationAstNode>().ToList();
        if (declarations.Count == 0)
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect source must declare 'dialect <name>' before directives.", null);
        }

        if (declarations.Count > 1)
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect source must contain exactly one dialect declaration.", declarations[1].NameNode.LexemeValue);
        }

        if (!ReferenceEquals(scope.Children[0], declarations[0]))
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect declaration must be the first non-empty line in the document.", declarations[0].NameNode.LexemeValue);
        }

        var directives = scope.Children.Skip(1).OfType<DialectDirectiveAstNode>().ToList();
        var document = new DialectDocumentAstNode(declarations[0], directives);
        scope.Children.Clear();
        scope.Children.Add(document);
        return true;
    }
}

internal static class DialectDirectiveSyntax
{
    public const string DialectKeyword = DialectDslKeywords.Dialect;
}
