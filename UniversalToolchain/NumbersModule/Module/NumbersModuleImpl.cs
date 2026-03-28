namespace NumbersModule.Module;

[UniversalToolchain.Dialects.Abstractions.DialectModuleAlias("Numbers")]
[UniversalToolchain.Dialects.Abstractions.DialectRuntimeExport("FrontendModule", "Numbers")]
[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Universal)]
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