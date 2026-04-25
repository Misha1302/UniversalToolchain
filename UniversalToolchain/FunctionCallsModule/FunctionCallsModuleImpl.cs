namespace FunctionCallsModule;

[DialectModuleAlias("FunctionCalls")]
[DialectCapabilityProvider(typeof(FunctionCallsCapabilityProvider))]
[DialectRuntimeExport("FrontendModule", "FunctionCalls")]
[AutoRegisterService]
public sealed class FunctionCallsModuleImpl : IFrontendCoreModule
{
}
