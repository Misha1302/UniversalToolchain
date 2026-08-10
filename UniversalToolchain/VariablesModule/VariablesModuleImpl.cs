using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.ModuleContracts;
using VariablesModule.Contracts;

namespace VariablesModule;
[DialectCapabilityProvider(typeof(VariablesCapabilityProvider))]
[DialectComponentContract("FrontendModule", "Variables")]
[AutoRegisterService]
public class VariablesModuleImpl : IFrontendCoreModule, IModuleContractDescriptorProvider
{
    public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners => _contractDescriptorProvider.NamespaceOwners;

    private static readonly VariablesModuleContractDescriptorProvider _contractDescriptorProvider = new();

    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(":", "Colon"),
        new("let", "Let")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(-1.5f, new VariablesNodeCreator())
    ];

    private readonly ITypeCatalog _typeCatalog;

    public VariablesModuleImpl(ITypeCatalog typeCatalog)
    {
        _typeCatalog = typeCatalog.ArgNotNull();
    }

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new VariablesVisitor(_typeCatalog));

    public IReadOnlyList<IAstBindingRule> GetAstBindingRules() => [new VariablesBindingRule()];

    public IReadOnlyList<IModuleContractFacet> GetFacets() => _contractDescriptorProvider.GetFacets();
}
