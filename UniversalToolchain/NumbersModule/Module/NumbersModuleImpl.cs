using BasicCore.Attributes;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using NumbersModule.Visitors;

namespace NumbersModule.Module;

[AutoRegisterService]
public class NumbersModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(
            @"[+-]?\d+(?:_?\d+)*(?:\.\d+(?:_?\d+)*)?(?:[eE][+-]?\d+(?:_?\d+)*)?",
            "Number",
            Priority: -20f
        )
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new NumberAstVisitor());
}