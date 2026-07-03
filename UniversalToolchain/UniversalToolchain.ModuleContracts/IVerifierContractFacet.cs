namespace UniversalToolchain.ModuleContracts;

public interface IVerifierContractFacet : IModuleContractFacet
{
    IReadOnlyList<VerifierRuleContribution> Rules { get; }
}
