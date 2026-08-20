using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.ModuleContracts;
using ScopesModule.Contracts;

namespace ScopesModule.Module;
[DialectCapabilityProvider(typeof(ScopesCapabilityProvider))]
[DialectComponentContract("FrontendModule", "Scopes")]
[AutoRegisterService]
public class ScopesModuleImpl : IFrontendCoreModule, IModuleContractDescriptorProvider
{
    public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners => [ContractNamespaceOwner.Reserved("wist", "wist")];

    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(@"\(", "OpenPar"),
        new(@"\)", "ClosePar")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(-100_000f, new ScopesCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new ScopeAstVisitor());

    public IReadOnlyList<IModuleContractFacet> GetFacets() =>
        new ScopesModuleContractDescriptorProvider().GetFacets();
}
