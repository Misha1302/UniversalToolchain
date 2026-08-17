namespace ArithmeticModule.Module;
[DialectComponentContract("FrontendModule", "TextualAddition")]
[AutoRegisterService]
public class TextualAdditionModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(@"\bplus\b", "TextualAddition", Priority: 110f)
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(-30f, new TextualAdditionOperationNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    // Textual addition is syntax-only. Its lowering is owned by the canonical semantic Add lowerer.
    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        ArgumentNullException.ThrowIfNull(translator);
    }
}
