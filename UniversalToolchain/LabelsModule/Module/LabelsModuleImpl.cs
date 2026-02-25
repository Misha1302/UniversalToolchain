using BasicCore.Attributes;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using LabelsModule.Core;
using LabelsModule.Creators;
using LabelsModule.Visitors;

namespace LabelsModule.Module;

[AutoRegisterService]
public class LabelsModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> LexemeRegistrations =
    [
        new(":", "Colon"),
        new("goto", "Goto", Priority: -10f)
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> NodeCreatorRegistrations =
    [
        new(-2f, new LabelsNodeCreator()),
        new(-2f, new GotoNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(LexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(NodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        var labelsSharedData = new LabelsSharedData();
        translator.AddVisitors(new LabelsVisitor(labelsSharedData), new GotoVisitor(labelsSharedData));
    }
}