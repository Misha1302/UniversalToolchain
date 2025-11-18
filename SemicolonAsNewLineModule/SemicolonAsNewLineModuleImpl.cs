using BasicCore;
using BasicCore.LexerWrapper;
using BasicTypesExtensions;

namespace SemicolonAsNewLineModule;

public class SemicolonAsNewLineModuleImpl : ICoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryUncheckedRewritePattern(
            new LexemePattern(";", ExtensibleEnum<LexemeTag>.CreateOrGet("NewLine"))
        );
    }
}