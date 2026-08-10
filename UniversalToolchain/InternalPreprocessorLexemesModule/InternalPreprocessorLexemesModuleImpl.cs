using CommonExceptions;
using UniversalToolchain.Dialects.Abstractions;

namespace InternalPreprocessorLexemesModule;
[DialectComponentContract("FrontendModule", "InternalPreprocessorLexemes")]
[AutoRegisterService]
public class InternalPreprocessorLexemesModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(
                "\\#\\!\\[[^\\n\\]]*(?:\\]|(?=\\n|$))",
                LexemeType.CreateOrGet(PreprocessorLexemeContracts.NodeTypeName)
            ),
            priority: 100_000_000_000
        );
    }

    public List<LexemeValue> ProcessLexemes(List<LexemeValue> current)
    {
        foreach (var lexeme in current)
        {
            if (lexeme.LexemePattern?.LexemeType.GetName() != PreprocessorLexemeContracts.NodeTypeName)
                continue;

            ToolchainThrower.Parser(
                "preprocessor token is internal-only",
                new SourceLocation { Line = lexeme.LineNumber, Column = lexeme.CharNumber }
            );
        }

        return current;
    }
}