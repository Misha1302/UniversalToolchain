using BasicCore.ParserWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectAstLineReader
{
    public static IReadOnlyList<AstNode> ReadStatements(AstNode astRoot)
    {
        if (astRoot == null)
        {
            Thrower.ArgumentNull(nameof(astRoot));
        }

        return DialectDslAstValidator.Validate(astRoot).Children.ToList();
    }
}
