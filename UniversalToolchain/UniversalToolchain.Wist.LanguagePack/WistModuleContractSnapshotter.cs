using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.Wist.LanguagePack;

/// <summary>
/// Projects module-contract metadata into detached value objects before it is stored in a runtime artifact.
/// Contract providers are allowed to expose interface implementations backed by mutable collections, so keeping
/// either the facet instance or one of its nested lists would let post-execution mutation rewrite provenance.
/// </summary>
internal static class WistModuleContractSnapshotter
{
    public static IReadOnlyList<ContractNamespaceOwner> CaptureNamespaceOwners(
        IReadOnlyList<ContractNamespaceOwner> namespaceOwners)
    {
        ArgumentNullException.ThrowIfNull(namespaceOwners);
        return namespaceOwners.Select(CloneNamespaceOwner).ToArray();
    }

    public static IReadOnlyList<IModuleContractFacet> CaptureFacets(
        IReadOnlyList<IModuleContractFacet> facets)
    {
        ArgumentNullException.ThrowIfNull(facets);
        return facets.Select(CloneFacet).ToArray();
    }

    private static ContractNamespaceOwner CloneNamespaceOwner(ContractNamespaceOwner owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (!owner.ReservesPrefix)
            return ContractNamespaceOwner.External(owner.Name);

        if (owner.NamespacePrefix == null)
        {
            throw new InvalidOperationException(
                $"Contract namespace owner '{owner.Name}' reserves a prefix but does not expose one.");
        }

        return ContractNamespaceOwner.Reserved(owner.Name, owner.NamespacePrefix);
    }

    private static IModuleContractFacet CloneFacet(IModuleContractFacet facet)
    {
        ArgumentNullException.ThrowIfNull(facet);
        return facet.Kind switch
        {
            ContractFacetKind.Syntax when facet is ISyntaxContractFacet syntax => CloneSyntax(syntax),
            ContractFacetKind.Ast when facet is IAstContractFacet ast => CloneAst(ast),
            ContractFacetKind.Bytecode when facet is IBytecodeContractFacet bytecode => CloneBytecode(bytecode),
            ContractFacetKind.Air when facet is IAirContractFacet air => CloneAir(air),
            ContractFacetKind.CompilerFacts when facet is ICompilerFactOwnershipFacet facts => CloneCompilerFacts(facts),
            ContractFacetKind.PipelineEffects when facet is IPipelineEffectContractFacet effects => ClonePipelineEffects(effects),
            ContractFacetKind.Verifier when facet is IVerifierContractFacet verifier => CloneVerifier(verifier),
            ContractFacetKind.BackendCapability when facet is IBackendCapabilityFacet backend => CloneBackendCapabilities(backend),
            _ => throw new InvalidOperationException(
                $"Contract facet '{facet.GetType().FullName}' declares kind '{facet.Kind}' but does not implement the corresponding supported contract shape.")
        };
    }

    private static SyntaxContractFacet CloneSyntax(ISyntaxContractFacet facet) =>
        new(
            facet.ModuleId,
            facet.Lexemes
                .Select(static lexeme => new LexemeContract(lexeme.LexemeId, lexeme.PatternDescription))
                .ToArray(),
            facet.ParserNodes
                .Select(static node => new ParserNodeContract(
                    node.Produces,
                    node.Priority,
                    node.MayConsume.ToArray()))
                .ToArray())
        {
            SchemaVersion = facet.SchemaVersion
        };

    private static AstContractFacet CloneAst(IAstContractFacet facet) =>
        new(
            facet.ModuleId,
            facet.AstOwnership
                .Select(static ownership => new AstOwnershipContract(
                    ownership.NodeKind,
                    ownership.Mode,
                    ownership.OwnerModule,
                    ownership.CooperatingModules.ToArray()))
                .ToArray())
        {
            SchemaVersion = facet.SchemaVersion
        };

    private static BytecodeContractFacet CloneBytecode(IBytecodeContractFacet facet) =>
        new(
            facet.ModuleId,
            facet.BytecodeEmissions
                .Select(static emission => new BytecodeEmissionContract(
                    emission.SourceNode,
                    emission.MayEmitTags.ToArray(),
                    emission.MayEmitPatterns.ToArray(),
                    new StackEffect(
                        emission.DeclaredStackEffect.PopCount,
                        emission.DeclaredStackEffect.PushCount),
                    emission.SideEffects))
                .ToArray())
        {
            SchemaVersion = facet.SchemaVersion
        };

    private static AirContractFacet CloneAir(IAirContractFacet facet) =>
        new(
            facet.ModuleId,
            facet.AirEmissions
                .Select(static emission => new AirEmissionContract(
                    emission.SourcePattern,
                    emission.MayEmitPatterns.ToArray(),
                    emission.MayEmitIntrinsics.ToArray(),
                    emission.RequiredCapabilities.ToArray()))
                .ToArray())
        {
            SchemaVersion = facet.SchemaVersion
        };

    private static CompilerFactOwnershipFacet CloneCompilerFacts(ICompilerFactOwnershipFacet facet) =>
        new(
            facet.ModuleId,
            facet.Facts
                .Select(static fact => new CompilerFactOwnershipContract(fact.FactId, fact.OwnerModule))
                .ToArray())
        {
            SchemaVersion = facet.SchemaVersion
        };

    private static PipelineEffectFacet ClonePipelineEffects(IPipelineEffectContractFacet facet) =>
        new(
            facet.ModuleId,
            facet.Effects
                .Select(static effect => new PipelineEffectContract(
                    effect.EffectId,
                    effect.Stage,
                    effect.Requires.ToArray(),
                    effect.Produces.ToArray(),
                    effect.Preserves.ToArray(),
                    effect.Invalidates.ToArray()))
                .ToArray())
        {
            SchemaVersion = facet.SchemaVersion
        };

    private static VerifierContractFacet CloneVerifier(IVerifierContractFacet facet) =>
        new(
            facet.ModuleId,
            facet.Rules
                .Select(static rule => new VerifierRuleContribution(
                    rule.RuleId,
                    rule.BytecodePatterns.ToArray(),
                    rule.AirPatterns.ToArray()))
                .ToArray())
        {
            SchemaVersion = facet.SchemaVersion
        };

    private static BackendCapabilityFacet CloneBackendCapabilities(IBackendCapabilityFacet facet) =>
        new(
            facet.ModuleId,
            facet.Capabilities
                .Select(static capability => new BackendCapabilityContract(
                    capability.CapabilityId,
                    capability.SupportedIntrinsics.ToArray()))
                .ToArray())
        {
            SchemaVersion = facet.SchemaVersion
        };
}
