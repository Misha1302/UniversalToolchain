using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.TranslatorWrapper;

namespace NumbersModule;

[AutoRegisterService]
public class NumbersModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> LexemeRegistrations =
    [
        new(
            @"[+-]?\d+(?:_?\d+)*(?:\.\d+(?:_?\d+)*)?(?:[eE][+-]?\d+(?:_?\d+)*)?",
            "Number",
            Priority: -20f
        )
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(LexemeRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new NumberAstVisitor());
}