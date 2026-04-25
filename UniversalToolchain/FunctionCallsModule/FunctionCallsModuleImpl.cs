namespace FunctionCallsModule;

[DialectModuleAlias("FunctionCalls")]
[DialectCapabilityProvider(typeof(FunctionCallsCapabilityProvider))]
[DialectRuntimeExport("FrontendModule", "FunctionCalls")]
[AutoRegisterService]
public sealed class FunctionCallsModuleImpl : IFrontendCoreModule
{
    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-900, new FunctionCallsNodeCreator());
    }
}
