namespace SemicolonAsNewLineModule;

[AutoRegisterService]
public class SemicolonAsNewLineModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryUncheckedAddPattern(
            new LexemePattern(";", ExtensibleEnum<LexemeTag>.CreateOrGet("NewLine"))
        );
    }
}