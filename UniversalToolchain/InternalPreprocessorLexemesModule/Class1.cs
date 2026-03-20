namespace InternalPreprocessorLexemesModule;

[UniversalToolchain.Dialects.Abstractions.DialectModuleAlias("InternalPreprocessorLexemes")]
[AutoRegisterService]
public class InternalPreprocessorLexemesModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(
                "\\#\\!\\[.*?\\]",
                LexemeType.CreateOrGet("Preprocessor lexeme")
            ),
            priority: 100_000_000_000
        );
    }
}