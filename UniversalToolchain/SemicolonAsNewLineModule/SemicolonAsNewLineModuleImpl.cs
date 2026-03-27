namespace SemicolonAsNewLineModule;

[UniversalToolchain.Dialects.Abstractions.DialectModuleAlias("SemicolonAsNewLine")]
[UniversalToolchain.Dialects.Abstractions.DialectRuntimeExport("wist", "FrontendModule", "SemicolonAsNewLine")]
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