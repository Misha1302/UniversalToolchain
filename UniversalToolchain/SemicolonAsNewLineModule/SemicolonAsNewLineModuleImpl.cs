namespace SemicolonAsNewLineModule;

[UniversalToolchain.Dialects.Abstractions.DialectModuleAlias("SemicolonAsNewLine")]
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