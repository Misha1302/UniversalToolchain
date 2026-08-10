using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Dialects.Abstractions;
using LabelsModule.Contracts;
using UniversalToolchain.ModuleContracts;

namespace LabelsModule.Module;
[DialectCapabilityProvider(typeof(LabelsCapabilityProvider))]
[DialectComponentContract("FrontendModule", "Labels")]
[AutoRegisterService]
public class LabelsModuleImpl : IFrontendCoreModule, IModuleContractDescriptorProvider
{
    public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners => _contractDescriptorProvider.NamespaceOwners;

    private static readonly LabelsModuleContractDescriptorProvider _contractDescriptorProvider = new();

    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(":", "Colon"),
        new("goto", "Goto", Priority: -10f)
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(-2f, new LabelsNodeCreator()),
        new(-2f, new GotoNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        var labelsSharedData = new LabelsSharedData();
        translator.AddVisitors(new LabelsVisitor(labelsSharedData), new GotoVisitor(labelsSharedData));
    }

    public IReadOnlyList<IModuleContractFacet> GetFacets() => _contractDescriptorProvider.GetFacets();
}
