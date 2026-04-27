using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Dialects.Abstractions;

namespace ConditionsModule.Enums;

[DialectModuleAlias("ComparisonConditions")]
[DialectCapabilityProvider(typeof(global::ConditionsModule.ComparisonCapabilityProvider))]
[DialectRuntimeExport("FrontendModule", "ComparisonConditions")]
[AutoRegisterService]
public class ComparisonOperations : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(@"\=\=", "Equal"),
        new(@"\!\=", "NotEqual"),
        new(@"\>", "Greater"),
        new(@"\<", "Less"),
        new(@"\>\=", "GreaterOrEqual", Priority: -1f),
        new(@"\<\=", "LessOrEqual", Priority: -1f)
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(-20f, new ComparisonNodeCreator("Equal")),
        new(-20f, new ComparisonNodeCreator("NotEqual")),
        new(-20f, new ComparisonNodeCreator("Greater")),
        new(-20f, new ComparisonNodeCreator("Less")),
        new(-20f, new ComparisonNodeCreator("GreaterOrEqual")),
        new(-20f, new ComparisonNodeCreator("LessOrEqual"))
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new ComparisonVisitor());
}
