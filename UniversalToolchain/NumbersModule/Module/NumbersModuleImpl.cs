using UniversalToolchain.Capabilities.Abstractions;
using NumbersModule.Contracts;
using UniversalToolchain.ModuleContracts;

namespace NumbersModule.Module;
[DialectCapabilityProvider(typeof(NumbersCapabilityProvider))]
[DialectComponentContract("FrontendModule", "Numbers")]
[AutoRegisterService]
public class NumbersModuleImpl : IFrontendCoreModule, IModuleContractDescriptorProvider
{
    public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners => _contractDescriptorProvider.NamespaceOwners;

    private static readonly NumbersModuleContractDescriptorProvider _contractDescriptorProvider = new();

    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(
            @"\d+(?:_?\d+)*(?:\.\d+(?:_?\d+)*)?(?:[eE][+-]?\d+(?:_?\d+)*)?",
            "Number",
            Priority: -20f
        )
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new NumberAstVisitor());

    public IReadOnlyList<IModuleContractFacet> GetFacets() => _contractDescriptorProvider.GetFacets();
}
