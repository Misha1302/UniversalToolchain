using BasicCore.ParserWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectAstPipelineValidator
{
    public static AstNode Validate(AstNode astRoot, DialectDslRegistry registry)
    {
        astRoot = astRoot.ArgNotNull();

        registry = registry.ArgNotNull();

        DialectDslAstValidator.Validate(astRoot, registry);
        return astRoot;
    }
}

public static class DialectDslAstValidator
{
    public static DialectDocumentAstNode Validate(AstNode astRoot, DialectDslRegistry registry)
    {
        astRoot = astRoot.ArgNotNull();

        registry = registry.ArgNotNull();

        if (astRoot.Children.Count != 1 || astRoot.Children[0] is not DialectDocumentAstNode)
            DialectDefinitionSliceParseErrors.Fail("Dialect AST must contain exactly one DialectDocumentAstNode root.", astRoot.Children.FirstOrDefault()?.LexemeValue);

        var document = (DialectDocumentAstNode)astRoot.Children[0];
        ValidateDocument(document, registry);
        return document;
    }

    private static void ValidateDocument(DialectDocumentAstNode document, DialectDslRegistry registry)
    {
        if (document.Children.Count == 0)
            DialectDefinitionSliceParseErrors.Fail("Dialect document must contain a declaration node.", null);

        if (document.Children[0] is not DialectDeclarationAstNode)
            DialectDefinitionSliceParseErrors.Fail("Dialect document must start with a declaration node.", document.Children[0].LexemeValue);

        var declaration = (DialectDeclarationAstNode)document.Children[0];
        ValidateDeclaration(declaration);

        var context = new DialectDirectiveValidationContext();
        foreach (var directive in document.Directives)
        {
            if (directive.Feature.IsSingleton)
                context.EnsureSingleton(directive.Feature, directive.LexemeValue);

            directive.Feature.ValidateSemantic(directive, context);
        }

        foreach (var rule in registry.DocumentRules)
            rule.Validate(document, context);
    }

    private static void ValidateDeclaration(DialectDeclarationAstNode declaration)
    {
        if (declaration.Children.Count != 1 || declaration.Children[0] is not IdentifierValueAstNode)
            DialectDefinitionSliceParseErrors.Fail("Dialect declaration must contain exactly one identifier child.", declaration.LexemeValue);

        var identifier = (IdentifierValueAstNode)declaration.Children[0];
        if (string.IsNullOrWhiteSpace(identifier.Identifier))
            DialectDefinitionSliceParseErrors.Fail("Dialect name must not be empty.", identifier.LexemeValue);
    }
}