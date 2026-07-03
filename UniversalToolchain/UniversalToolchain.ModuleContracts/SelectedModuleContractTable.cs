namespace UniversalToolchain.ModuleContracts;

public sealed class SelectedModuleContractTable
{
    internal SelectedModuleContractTable(
        ContractSchemaVersion schemaVersion,
        IReadOnlyList<IModuleContractFacet> facets,
        IReadOnlyList<ToolchainDiagnostic> diagnostics)
    {
        SchemaVersion = schemaVersion;
        Facets = facets;
        Diagnostics = diagnostics;
    }

    public ContractSchemaVersion SchemaVersion { get; }

    public IReadOnlyList<IModuleContractFacet> Facets { get; }

    public IReadOnlyList<ToolchainDiagnostic> Diagnostics { get; }

    public IReadOnlyList<ISyntaxContractFacet> SyntaxFacets => Facets.OfType<ISyntaxContractFacet>().ToArray();

    public IReadOnlyList<IAstContractFacet> AstFacets => Facets.OfType<IAstContractFacet>().ToArray();

    public IReadOnlyList<IBytecodeContractFacet> BytecodeFacets => Facets.OfType<IBytecodeContractFacet>().ToArray();

    public IReadOnlyList<IAirContractFacet> AirFacets => Facets.OfType<IAirContractFacet>().ToArray();

    public IReadOnlyList<ICompilerFactOwnershipFacet> CompilerFactOwnershipFacets => Facets.OfType<ICompilerFactOwnershipFacet>().ToArray();

    public IReadOnlyList<IPipelineEffectContractFacet> PipelineEffectFacets => Facets.OfType<IPipelineEffectContractFacet>().ToArray();

    public IReadOnlyList<IVerifierContractFacet> VerifierFacets => Facets.OfType<IVerifierContractFacet>().ToArray();

    public IReadOnlyList<IBackendCapabilityFacet> BackendCapabilityFacets => Facets.OfType<IBackendCapabilityFacet>().ToArray();
}
