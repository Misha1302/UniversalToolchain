namespace SafeMathFunctionsModule;

[DialectModuleAlias("SafeMathFunctions")]
[DialectCapabilityProvider(typeof(SafeMathFunctionsCapabilityProvider))]
[DialectRuntimeExport("FrontendModule", "SafeMathFunctions")]
[AutoRegisterService]
public sealed class SafeMathFunctionsModuleImpl : IFrontendCoreModule
{
}
