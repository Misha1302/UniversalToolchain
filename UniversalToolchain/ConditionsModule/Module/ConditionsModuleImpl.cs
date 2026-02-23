using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;

namespace ConditionsModule;

[AutoRegisterService]
public class ConditionsModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> LexemeRegistrations =
    [
        new("if", "If"),
        new("elif", "Elif"),
        new("else", "Else")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> NodeCreatorRegistrations =
    [
        new(15f, new IfNodeCreator()),
        new(15f, new ElifNodeCreator()),
        new(15f, new ElseNodeCreator()),
        new(16f, new CondNodesCombiner())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(LexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(NodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new ConditionsVisitor());
}