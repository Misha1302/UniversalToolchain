namespace StringsModule.Module;

[DialectModuleAlias("Strings")]
[DialectRuntimeExport("FrontendModule", "Strings")]
[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Universal)]
public class StringsModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new("\"(?:\\\\.|[^\"\\\\])*\"", "String", Priority: -90f)
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new StringAstVisitor());
}
