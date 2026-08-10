namespace FunctionCallsModule;
[DialectCapabilityProvider(typeof(FunctionCallsCapabilityProvider))]
[DialectComponentContract("FrontendModule", "FunctionCalls")]
[AutoRegisterService]
public sealed class FunctionCallsModuleImpl : IFrontendCoreModule
{
    private readonly CapabilityCatalog _capabilityCatalog;

    public FunctionCallsModuleImpl(CapabilityCatalog capabilityCatalog)
    {
        _capabilityCatalog = capabilityCatalog.ArgNotNull();
    }

    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(",", "Comma")
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-900, new FunctionCallsNodeCreator());
    }

    public void InitAstTranslator(IAstToBytecodeTranslator translator, IReadOnlyList<IFrontendCoreModule> selectedModules)
    {
        translator = translator.ArgNotNull();
        selectedModules.ArgNotNull();
        translator.Configuration.Visitors.Add(new FunctionCallsAstVisitor(_capabilityCatalog));
    }
}
