using BasicCore.ParserWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectAstPipelineValidator
{
    public static AstNode Validate(AstNode astRoot)
    {
        if (astRoot == null)
            Thrower.ArgumentNull(nameof(astRoot));

        return new DialectDslAstValidator().Validate(astRoot);
    }
}
