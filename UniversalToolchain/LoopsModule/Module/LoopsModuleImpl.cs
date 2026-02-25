using BasicCore.Attributes;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using LoopsModule.Creators;
using LoopsModule.Visitors;

namespace LoopsModule.Module;

[AutoRegisterService]
public class LoopsModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> LexemeRegistrations =
    [
        new("while", "While"),
        new("for", "For")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> NodeCreatorRegistrations =
    [
        new(15f, new WhileNodeCreator()),
        new(15f, new ForNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(LexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(NodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new LoopsVisitor());
}