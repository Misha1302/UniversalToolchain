using CommonExceptions;
using UniversalToolchain.Dialects.Abstractions;

namespace InternalPreprocessorLexemesModule;

[DialectModuleAlias("InternalPreprocessorLexemes")]
[DialectRuntimeExport("FrontendModule", "InternalPreprocessorLexemes")]
[AutoRegisterService]
public class InternalPreprocessorLexemesModuleImpl : IFrontendCoreModule
{
    private const string PreprocessorLexemeName = "Preprocessor lexeme";

    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(
                "\\#\\!\\[[^\\n\\]]*(?:\\]|(?=\\n|$))",
                LexemeType.CreateOrGet(PreprocessorLexemeName)
            ),
            priority: 100_000_000_000
        );
    }

    public List<LexemeValue> ProcessLexemes(List<LexemeValue> current)
    {
        foreach (var lexeme in current)
        {
            if (lexeme.LexemePattern?.LexemeType.GetName() != PreprocessorLexemeName)
                continue;

            WistThrower.Parser(
                "preprocessor token is internal-only",
                new SourceLocation { Line = lexeme.LineNumber, Column = lexeme.CharNumber }
            );
        }

        return current;
    }
}