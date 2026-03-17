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

        var lines = DialectAstLineReader.ReadLines(astRoot);
        if (lines.Count == 0)
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect source is empty.", null);
        }

        return astRoot;
    }
}
