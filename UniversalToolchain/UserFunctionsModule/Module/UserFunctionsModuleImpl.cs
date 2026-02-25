using BasicCore.Attributes;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using UserFunctionsModule.Creators;
using UserFunctionsModule.Visitors;

namespace UserFunctionsModule.Module;

[AutoRegisterService]
public class UserFunctionsModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> LexemeRegistrations =
    [
        new("fn", "Fn"),
        new("return", "Return"),
        new(",", "Comma")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> NodeCreatorRegistrations =
    [
        new(-40f, new UserFunctionNodeCreator(handleReturnNodes: false)),
        new(-1.75f, new UserFunctionReturnNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(LexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(NodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new UserFunctionsAstVisitor());
}
