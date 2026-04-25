using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Dialects.Abstractions;

namespace VariablesModule;

[DialectModuleAlias("Variables")]
[DialectCapabilityProvider(typeof(VariablesCapabilityProvider))]
[DialectRuntimeExport("FrontendModule", "Variables")]
[AutoRegisterService]
public class VariablesModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(":", "Colon"),
        new("let", "Let")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(-1.5f, new VariablesNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new VariablesVisitor());
}
