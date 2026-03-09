using BasicCore.Attributes;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using ConditionsModule.Core;
using ConditionsModule.Creators;
using ConditionsModule.Visitors;

namespace ConditionsModule.Module;

[AutoRegisterService]
public class ConditionsModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new("if", "If"),
        new("elif", "Elif"),
        new("else", "Else")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(15f, new IfNodeCreator()),
        new(15f, new ElifNodeCreator()),
        new(15f, new ElseNodeCreator()),
        new(16f, new CondNodesCombiner())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new ConditionsVisitor());
}