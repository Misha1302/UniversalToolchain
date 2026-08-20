using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.ModuleContracts;
using IdentifierModule.Contracts;

namespace IdentifierModule;
[DialectCapabilityProvider(typeof(IdentifierCapabilityProvider))]
[DialectComponentContract("FrontendModule", "Identifier")]
[AutoRegisterService]
public class IdentifierModuleImpl : IFrontendCoreModule, IModuleContractDescriptorProvider
{
    public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners => [ContractNamespaceOwner.Reserved("wist", "wist")];

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
