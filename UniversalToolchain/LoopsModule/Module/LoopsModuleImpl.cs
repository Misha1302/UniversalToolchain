namespace LoopsModule.Module;

[UniversalToolchain.Dialects.Abstractions.DialectModuleAlias("Loops")]
[UniversalToolchain.Dialects.Abstractions.DialectRuntimeExport("FrontendModule", "Loops")]
[AutoRegisterService]
public class LoopsModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new("while", "While"),
        new("for", "For")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(15f, new WhileNodeCreator()),
        new(15f, new ForNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new LoopsVisitor());
}