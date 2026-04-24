using UniversalToolchain.Dialects.Abstractions;

namespace ConditionsModule.Module;

[DialectModuleAlias("Conditions", "IfExpression")]
[DialectRuntimeExport("FrontendModule", "Conditions")]
[DialectRuntimeAlias("IfExpression")]
[AutoRegisterService]
public class ConditionsModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new("if", "If"),
        new("then", "Then"),
        new("elif", "Elif"),
        new("else", "Else")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(-35f, new IfExpressionNodeCreator()),
        new(15f, new IfNodeCreator()),
        new(15f, new ElifNodeCreator()),
        new(15f, new ElseNodeCreator()),
        new(16f, new CondNodesCombiner())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new IfExpressionVisitor(), new ConditionsVisitor());
}
