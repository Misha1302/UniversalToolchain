using BasicCore.Attributes;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;

namespace CommentsModule;

[AutoRegisterService]
public class CommentsModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(@"//[^\n]*", "SingleLineComment", true, -100f),
        new(@"/\*[\s\S]*?\*/", "MultiLineComment", true, -100f)
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

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