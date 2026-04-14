using BasicCore.ParserWrapper;
using ExceptionsManager;
using AstNodeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.ParserWrapper.AstNodeTag>;

namespace UniversalToolchain.Dialects.Frontend;

internal static class DialectNodeCreatorSupport
{
    private static readonly AstNodeType ScopeType = AstNodeType.CreateOrGet("Scope");

    public static bool IsScope(AstNode scope) => scope.NodeType == ScopeType;

    public static bool IsDialectLine(AstNode node) => node.NodeType == DialectAstNodeTypes.DialectLine;

    public static bool IsNewLineToken(AstNode node) => DialectLexemeTags.IsTag(node.LexemeValue, DialectLexemeTags.NewLine);

    public static bool IsCommaToken(AstNode node) => DialectLexemeTags.IsTag(node.LexemeValue, DialectLexemeTags.CommaToken);

    public static bool IsIdentifierToken(AstNode node) => DialectLexemeTags.IsTag(node.LexemeValue, DialectLexemeTags.Identifier);

    public static string GetKeywordText(AstNode lineNode)
    {
        if (lineNode.Children.Count == 0 || lineNode.Children[0].LexemeValue == null)
            return string.Empty;

        return lineNode.Children[0].Text;
    }

    public static IdentifierValueAstNode ParseSingleIdentifier(AstNode lineNode, string directiveDisplayName)
    {
        lineNode = lineNode.ArgNotNull();

        if (lineNode.Children.Count != 2)
            DialectDefinitionSliceParseErrors.Fail(
                $"Directive '{directiveDisplayName}' expects exactly one identifier argument.",
                lineNode.Children[0].LexemeValue);

        var identifierNode = lineNode.Children[1];
        if (!IsIdentifierToken(identifierNode))
            DialectDefinitionSliceParseErrors.Fail(
                $"Directive '{directiveDisplayName}' expects an identifier argument.",
                identifierNode.LexemeValue);

        return new IdentifierValueAstNode(identifierNode.LexemeValue!);
    }

    public static IdentifierListAstNode ParseIdentifierList(AstNode lineNode, string directiveDisplayName)
    {
        lineNode = lineNode.ArgNotNull();

        if (lineNode.Children.Count < 2)
            DialectDefinitionSliceParseErrors.Fail(
                $"Directive '{directiveDisplayName}' expects at least one identifier.",
                lineNode.Children[0].LexemeValue);

        var identifiers = new List<IdentifierValueAstNode>();
        var expectIdentifier = true;

        for (var i = 1; i < lineNode.Children.Count; i++)
        {
            var current = lineNode.Children[i];
            if (expectIdentifier)
            {
                if (!IsIdentifierToken(current))
                    DialectDefinitionSliceParseErrors.Fail(
                        $"Directive '{directiveDisplayName}' contains an invalid identifier list item.",
                        current.LexemeValue);

                identifiers.Add(new IdentifierValueAstNode(current.LexemeValue!));
                expectIdentifier = false;
                continue;
            }

            if (!IsCommaToken(current))
                DialectDefinitionSliceParseErrors.Fail(
                    $"Directive '{directiveDisplayName}' expects comma-separated identifiers.",
                    current.LexemeValue);

            expectIdentifier = true;
        }

        if (expectIdentifier)
            DialectDefinitionSliceParseErrors.Fail(
                $"Directive '{directiveDisplayName}' must not end with a trailing comma.",
                lineNode.Children[^1].LexemeValue);

        return new IdentifierListAstNode(identifiers);
    }
}

public sealed class DialectLineNodeCreator : IAstNodeCreator
{
    public AstNodeType AstNodeType => DialectAstNodeTypes.DialectLine;

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope == null || !DialectNodeCreatorSupport.IsScope(scope) || childIndex < 0 || childIndex >= scope.Children.Count)
            return false;

        var current = scope.Children[childIndex];
        if (DialectNodeCreatorSupport.IsDialectLine(current))
            return false;

        if (DialectNodeCreatorSupport.IsNewLineToken(current))
        {
            scope.Children.RemoveAt(childIndex);
            return true;
        }

        if (childIndex > 0)
        {
            var previous = scope.Children[childIndex - 1];
            if (!DialectNodeCreatorSupport.IsNewLineToken(previous) && !DialectNodeCreatorSupport.IsDialectLine(previous))
                return false;
        }

        var end = childIndex;
        while (end < scope.Children.Count && !DialectNodeCreatorSupport.IsNewLineToken(scope.Children[end]))
            end++;

        var lineChildren = new List<AstNode>();
        for (var i = childIndex; i < end; i++)
            lineChildren.Add(scope.Children[i]);

        var removeCount = end - childIndex;
        if (end < scope.Children.Count && DialectNodeCreatorSupport.IsNewLineToken(scope.Children[end]))
            removeCount++;

        scope.Children.RemoveRange(childIndex, removeCount);
        scope.Children.Insert(childIndex, new AstNode(DialectAstNodeTypes.DialectLine, null, lineChildren));
        return true;
    }
}

public abstract class DialectLineConstructNodeCreator : IAstNodeCreator
{
    protected abstract string Keyword { get; }
    public abstract AstNodeType AstNodeType { get; }

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope == null || !DialectNodeCreatorSupport.IsScope(scope) || childIndex < 0 || childIndex >= scope.Children.Count)
            return false;

        var candidate = scope.Children[childIndex];
        if (!DialectNodeCreatorSupport.IsDialectLine(candidate))
            return false;

        if (!string.Equals(DialectNodeCreatorSupport.GetKeywordText(candidate), Keyword, StringComparison.Ordinal))
            return false;

        scope.Children[childIndex] = CreateNode(candidate);
        return true;
    }

    protected abstract AstNode CreateNode(AstNode lineNode);
}

public sealed class DialectDeclarationNodeCreator : DialectLineConstructNodeCreator
{
    public override AstNodeType AstNodeType => DialectAstNodeTypes.DialectDeclaration;

    override protected string Keyword => DialectDslKeywords.Dialect;

    override protected AstNode CreateNode(AstNode lineNode)
    {
        if (lineNode.Children.Count != 2)
            DialectDefinitionSliceParseErrors.Fail("Dialect declaration must have the form 'dialect <name>'.", lineNode.Children[0].LexemeValue);

        var nameToken = lineNode.Children[1];
        if (!DialectNodeCreatorSupport.IsIdentifierToken(nameToken))
            DialectDefinitionSliceParseErrors.Fail("Dialect declaration expects an identifier name.", nameToken.LexemeValue);

        return new DialectDeclarationAstNode(new IdentifierValueAstNode(nameToken.LexemeValue!));
    }
}

public sealed class FeatureDialectDirectiveNodeCreator : DialectLineConstructNodeCreator
{
    private readonly IDialectDirectiveFeature _feature;

    public FeatureDialectDirectiveNodeCreator(IDialectDirectiveFeature feature)
    {
        feature = feature.ArgNotNull();

        _feature = feature;
    }

    public override AstNodeType AstNodeType => DialectAstNodeTypes.DialectDirective;

    override protected string Keyword => _feature.Keyword;

    override protected AstNode CreateNode(AstNode lineNode) => _feature.ParseDirective(lineNode);
}

public sealed class DialectDocumentNodeCreator : IAstNodeCreator
{
    public AstNodeType AstNodeType => DialectAstNodeTypes.DialectDocument;

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope == null || !DialectNodeCreatorSupport.IsScope(scope) || childIndex != 0)
            return false;

        if (scope.Children.Count == 0)
            return false;

        if (scope.Children.Count == 1 && scope.Children[0] is DialectDocumentAstNode)
            return false;

        if (scope.Children.Any(DialectNodeCreatorSupport.IsDialectLine))
        {
            var lineNode = scope.Children.First(DialectNodeCreatorSupport.IsDialectLine);
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
            DialectDefinitionSliceParseErrors.Fail("Dialect source must declare 'dialect <name>' before directives.", null);

        if (declarations.Count > 1)
            DialectDefinitionSliceParseErrors.Fail("Dialect source must contain exactly one dialect declaration.", declarations[1].NameNode.LexemeValue);

        if (!ReferenceEquals(scope.Children[0], declarations[0]))
            DialectDefinitionSliceParseErrors.Fail("Dialect declaration must be the first non-empty line in the document.", declarations[0].NameNode.LexemeValue);

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