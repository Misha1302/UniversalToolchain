using BasicCore;
using BasicCore.LexerWrapper;
using BasicTypesExtensions;

namespace SemicolonAsNewLineModule;

public class SemicolonAsNewLineModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryUncheckedRewritePattern(
            new LexemePattern(";", ExtensibleEnum<LexemeTag>.CreateOrGet("NewLine"))
        );
    }
}