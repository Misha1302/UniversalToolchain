using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectAstPipelineValidator
{
    public static AstNode Validate(AstNode astRoot)
    {
        if (astRoot == null)
        {
            Thrower.ArgumentNull(nameof(astRoot));
        }

        DialectDslAstValidator.Validate(astRoot);
        return astRoot;
    }
}

public static class DialectDslAstValidator
{
    public static DialectDocumentAstNode Validate(AstNode astRoot)
    {
        if (astRoot == null)
        {
            Thrower.ArgumentNull(nameof(astRoot));
        }

        if (astRoot.Children.Count != 1 || astRoot.Children[0] is not DialectDocumentAstNode)
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect AST must contain exactly one DialectDocumentAstNode root.", astRoot.Children.FirstOrDefault()?.LexemeValue);
        }

        var document = (DialectDocumentAstNode)astRoot.Children[0];
        ValidateDocument(document);
        return document;
    }

    private static void ValidateDocument(DialectDocumentAstNode document)
    {
        if (document.Children.Count == 0)
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect document must contain a declaration node.", null);
        }

        if (document.Children[0] is not DialectDeclarationAstNode)
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect document must start with a declaration node.", document.Children[0].LexemeValue);
        }

        var declaration = (DialectDeclarationAstNode)document.Children[0];
        ValidateDeclaration(declaration);

        var state = new DialectDirectiveValidationState();
        foreach (var node in document.Children.Skip(1))
        {
            if (node is not DialectDirectiveAstNode)
            {
                DialectDefinitionSliceParseErrors.Fail(
                    $"Dialect document contains an unexpected child node of type '{node.NodeType.GetName()}'.",
                    node.LexemeValue ?? node.Children.FirstOrDefault()?.LexemeValue);
            }

            var directive = (DialectDirectiveAstNode)node;
            ValidateDirectiveShape(directive);
            directive.Feature.ValidateSemantic(directive, state);
        }

        foreach (var rule in DialectDslFeatureCatalog.DocumentRules)
        {
            rule.Validate(document, state);
        }
    }

    private static void ValidateDeclaration(DialectDeclarationAstNode declaration)
    {
        if (declaration.Children.Count != 1 || declaration.Children[0] is not IdentifierValueAstNode)
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect declaration must contain exactly one identifier child.", declaration.LexemeValue);
        }

        ValidateIdentifier((IdentifierValueAstNode)declaration.Children[0], "Dialect name must not be empty.");
    }

    private static void ValidateDirectiveShape(DialectDirectiveAstNode directive)
    {
        if (directive.Feature.ArgumentShape == DialectDirectiveArgumentShape.IdentifierList)
        {
            if (directive.Children.Count != 1 || directive.Children[0] is not IdentifierListAstNode)
            {
                DialectDefinitionSliceParseErrors.Fail(
                    $"Directive '{directive.Feature.Keyword}' must contain exactly one identifier-list child.",
                    directive.LexemeValue ?? directive.Children.FirstOrDefault()?.LexemeValue);
            }

            var listNode = (IdentifierListAstNode)directive.Children[0];
            if (listNode.Identifiers.Count == 0)
            {
                DialectDefinitionSliceParseErrors.Fail($"Directive '{directive.Feature.Keyword}' must contain at least one identifier.", directive.LexemeValue);
            }

            foreach (var identifier in listNode.Identifiers)
            {
                ValidateIdentifier(identifier, $"Directive '{directive.Feature.Keyword}' contains an empty identifier.");
            }

            ValidateNoDuplicates(listNode.Identifiers.Select(x => x.Identifier), $"Directive '{directive.Feature.Keyword}' contains duplicate identifiers.", directive.LexemeValue);
            return;
        }

        if (directive.Children.Count != 1 || directive.Children[0] is not IdentifierValueAstNode)
        {
            DialectDefinitionSliceParseErrors.Fail(
                $"Directive '{directive.Feature.Keyword}' must contain exactly one identifier child.",
                directive.LexemeValue ?? directive.Children.FirstOrDefault()?.LexemeValue);
        }

        var identifierNode = (IdentifierValueAstNode)directive.Children[0];
        ValidateIdentifier(identifierNode, $"Directive '{directive.Feature.Keyword}' must not be empty.");
    }

    private static void ValidateIdentifier(IdentifierValueAstNode identifier, string message)
    {
        if (identifier == null)
        {
            Thrower.ArgumentNull(nameof(identifier));
        }

        if (string.IsNullOrWhiteSpace(identifier.Identifier))
        {
            DialectDefinitionSliceParseErrors.Fail(message, identifier.LexemeValue);
        }
    }

    private static void ValidateNoDuplicates(IEnumerable<string> values, string message, LexemeValue? token)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (!set.Add(value))
            {
                DialectDefinitionSliceParseErrors.Fail(message, token);
            }
        }
    }
}
