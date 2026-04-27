using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Dialects.Abstractions;

namespace ConditionsModule.Module;

[DialectModuleAlias("Conditions")]
[DialectCapabilityProvider(typeof(global::ConditionsModule.ConditionalBranchesCapabilityProvider))]
[DialectRuntimeExport("FrontendModule", "Conditions")]
[AutoRegisterService]
public class ConditionsModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new("if", "If"),
        new("elif", "Elif"),
        new("then", "Then"),
        new("else", "Else")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(14.5f, new IfExpressionNodeCreator()),
        new(15f, new IfNodeCreator()),
        new(15f, new ElifNodeCreator()),
        new(15f, new ElseNodeCreator()),
        new(16f, new CondNodesCombiner())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) =>
        translator.AddVisitors(new IfExpressionVisitor(), new ConditionsVisitor());
}
