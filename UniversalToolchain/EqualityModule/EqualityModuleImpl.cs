using BasicCore.Attributes;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;

namespace EqualityModule;

[AutoRegisterService]
public class EqualityModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> LexemeRegistrations =
    [
        new(@"\=", "Equality", Priority: 100f)
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> NodeCreatorRegistrations =
    [
        new(10f, new ValuesSetNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(LexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(NodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new EqualityAstVisitor());
}