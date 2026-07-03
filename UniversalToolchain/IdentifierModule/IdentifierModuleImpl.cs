using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.ModuleContracts;
using IdentifierModule.Contracts;

namespace IdentifierModule;

[DialectModuleAlias("Identifier")]
[DialectCapabilityProvider(typeof(IdentifierCapabilityProvider))]
[DialectRuntimeExport("FrontendModule", "Identifier")]
[AutoRegisterService]
public class IdentifierModuleImpl : IFrontendCoreModule, IModuleContractDescriptorProvider
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(
            @"[@a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)*(?:<[^<>]*(?:<(?:[^<>]|<<|>>)*>[^<>]*)*>)?",
            "Identifier",
            Priority: 100f
        )
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public IReadOnlyList<IModuleContractFacet> GetFacets() =>
        new IdentifierModuleContractDescriptorProvider().GetFacets();
}
