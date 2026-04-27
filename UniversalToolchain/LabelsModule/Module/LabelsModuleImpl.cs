using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Dialects.Abstractions;

namespace LabelsModule.Module;

[DialectModuleAlias("Labels")]
[DialectCapabilityProvider(typeof(global::LabelsModule.LabelsCapabilityProvider))]
[DialectRuntimeExport("FrontendModule", "Labels")]
[AutoRegisterService]
public class LabelsModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(":", "Colon"),
        new("goto", "Goto", Priority: -10f)
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(-2f, new LabelsNodeCreator()),
        new(-2f, new GotoNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        var labelsSharedData = new LabelsSharedData();
        translator.AddVisitors(new LabelsVisitor(labelsSharedData), new GotoVisitor(labelsSharedData));
    }
}
