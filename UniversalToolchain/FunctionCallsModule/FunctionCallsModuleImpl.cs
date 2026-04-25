namespace FunctionCallsModule;

[DialectModuleAlias("FunctionCalls")]
[DialectCapabilityProvider(typeof(FunctionCallsCapabilityProvider))]
[DialectRuntimeExport("FrontendModule", "FunctionCalls")]
[AutoRegisterService]
public sealed class FunctionCallsModuleImpl : IFrontendCoreModule
{
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
        selectedModules = selectedModules.ArgNotNull();

        var capabilityCatalog = new SelectedCapabilityCatalogBuilder()
            .Build(selectedModules.Select(static x => x.GetType()));

        translator.Configuration.Visitors.Add(new FunctionCallsAstVisitor(capabilityCatalog));
    }
}
