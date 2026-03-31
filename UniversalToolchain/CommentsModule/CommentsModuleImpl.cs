using CommonExceptions;
using UniversalToolchain.Dialects.Abstractions;

namespace CommentsModule;

[DialectModuleAlias("Comments")]
[DialectRuntimeExport("FrontendModule", "Comments")]
[AutoRegisterService]
public class CommentsModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(@"//[^\n]*", "SingleLineComment", true, -100f),
        new(@"/\*[\s\S]*?\*/", "MultiLineComment", true, -100f)
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public string ProcessText(string curCode)
    {
        ValidateNoUnterminatedBlockComment(curCode);
        return curCode;
    }

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
    }

    private static void ValidateNoUnterminatedBlockComment(string code)
    {
        var searchIndex = 0;

        while (searchIndex < code.Length)
        {
            var openingIndex = code.IndexOf("/*", searchIndex, StringComparison.Ordinal);
            if (openingIndex < 0)
                return;

            var closingIndex = code.IndexOf("*/", openingIndex + 2, StringComparison.Ordinal);
            if (closingIndex < 0)
            {
                var location = new LexemeValue("/*", null, openingIndex, code);
                WistThrower.Lexer(
                    "Unterminated block comment (comment).",
                    new SourceLocation { Line = location.LineNumber, Column = location.CharNumber }
                );
            }

            searchIndex = closingIndex + 2;
        }
    }
}
