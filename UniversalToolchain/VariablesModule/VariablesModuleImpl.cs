using BasicCore;
using BasicCore.Attributes;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;

namespace VariablesModule;

[AutoRegisterService]
public class VariablesModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> LexemeRegistrations =
    [
        new(":", "Colon"),
        new("let", "Let")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> NodeCreatorRegistrations =
    [
        new(-1.5f, new VariablesNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(LexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(NodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new VariablesVisitor());
}