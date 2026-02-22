using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.TranslatorWrapper;

namespace CommentsModule;

[AutoRegisterService]
public class CommentsModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> LexemeRegistrations =
    [
        new(@"//[^\n]*", "SingleLineComment", Ignore: true, Priority: -100f),
        new(@"/\*[\s\S]*?\*/", "MultiLineComment", Ignore: true, Priority: -100f)
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(LexemeRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        // Комментарии не попадают в AST
    }

    // Метод ProcessText для предварительной обработки (опционально)
    public string ProcessText(string curCode) =>
        // Можно добавить предварительную обработку для удаления комментариев,
        // но лучше оставить это лексеру
        curCode;
}
