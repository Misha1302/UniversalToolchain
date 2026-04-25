namespace RuleDeclarationsModule;

[DialectModuleAlias("RuleDeclarations")]
[DialectCapabilityProvider(typeof(RuleDeclarationsCapabilityProvider))]
[DialectRuntimeExport("FrontendModule", "RuleDeclarations")]
[AutoRegisterService]
public sealed class RuleDeclarationsModuleImpl : IFrontendCoreModule
{
}
