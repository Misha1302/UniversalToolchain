namespace UniversalToolchain.ModuleContracts;

public static class KnownCoreCompilerFacts
{
    public static CompilerFactId SourceAvailable { get; } = new("core.source.available");

    public static CompilerFactId LexemesGenerated { get; } = new("core.lexemes.generated");

    public static CompilerFactId AstParsed { get; } = new("core.ast.parsed");

    public static CompilerFactId AstBound { get; } = new("core.ast.bound");

    public static CompilerFactId BytecodeGenerated { get; } = new("core.bytecode.generated");

    public static CompilerFactId BytecodeMetadataValid { get; } = new("core.bytecode.metadata-valid");

    public static CompilerFactId BytecodeVerified { get; } = new("core.bytecode.verified");

    public static CompilerFactId AirGenerated { get; } = new("core.air.generated");

    public static CompilerFactId AirSchemaValid { get; } = new("core.air.schema-valid");

    public static CompilerFactId AirBranchTargetsValid { get; } = new("core.air.branch-targets-valid");

    public static CompilerFactId AirStackBalanced { get; } = new("core.air.stack-balanced");

    public static CompilerFactId AirBranchStackCompatible { get; } = new("core.air.branch-stack-compatible");

    public static CompilerFactId AirIntrinsicsSupported { get; } = new("core.air.intrinsics-supported");

    public static CompilerFactId AirBackendCapabilitiesResolved { get; } = new("core.air.backend-capabilities-resolved");

    public static CompilerFactId AirVerified { get; } = new("core.air.verified");

    public static CompilerFactId BackendInputVerified { get; } = new("core.backend.input-verified");

    public static CompilerFactId ExecutionSemanticContractReady { get; } = new("core.execution.semantic-contract-ready");

    public static CompilerFactOwnershipFacet CreateOwnershipFacet() =>
        new(
            KnownCoreModuleIds.CompilerFacts,
            [
                Own(SourceAvailable),
                Own(LexemesGenerated),
                Own(AstParsed),
                Own(AstBound),
                Own(BytecodeGenerated),
                Own(BytecodeMetadataValid),
                Own(BytecodeVerified),
                Own(AirGenerated),
                Own(AirSchemaValid),
                Own(AirBranchTargetsValid),
                Own(AirStackBalanced),
                Own(AirBranchStackCompatible),
                Own(AirIntrinsicsSupported),
                Own(AirBackendCapabilitiesResolved),
                Own(AirVerified),
                Own(BackendInputVerified),
                Own(ExecutionSemanticContractReady)
            ]);

    private static CompilerFactOwnershipContract Own(CompilerFactId factId) =>
        new(factId, KnownCoreModuleIds.CompilerFacts);
}
