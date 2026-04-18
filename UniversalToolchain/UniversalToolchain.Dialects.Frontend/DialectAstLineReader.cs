using BasicCore.ParserWrapper;
using ExceptionsManager;
using UniversalToolchain.Dialects.Frontend.Composition;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectAstLineReader
{
    public static IReadOnlyList<AstNode> ReadStatements(AstNode astRoot, DialectDslRegistry registry)
    {
        astRoot = astRoot.ArgNotNull();

        registry = registry.ArgNotNull();

        return DialectDslAstValidator.Validate(astRoot, registry).Children.ToList();
    }
}