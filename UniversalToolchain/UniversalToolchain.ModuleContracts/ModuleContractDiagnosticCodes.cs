namespace UniversalToolchain.ModuleContracts;

public static class ModuleContractDiagnosticCodes
{
    public const string DuplicateFacet = "UT-CONTRACT-001";
    public const string DuplicateId = "UT-CONTRACT-002";
    public const string InvalidNamespaceOwnership = "UT-CONTRACT-003";
    public const string DeprecatedAlias = "UT-CONTRACT-004";
    public const string SchemaDowngrade = "UT-CONTRACT-005";
    public const string LegacyImplicitModule = "UT-CONTRACT-006";
    public const string NewModuleMissingDescriptor = "UT-CONTRACT-007";
    public const string DeclaredModuleMissingDescriptor = "UT-CONTRACT-008";
    public const string MissingFacetKindOrder = "UT-CONTRACT-009";
    public const string ZeroAstOwner = "UT-MOD-OWNERSHIP-001";
    public const string MultipleAstOwners = "UT-MOD-OWNERSHIP-002";
    public const string LowererOwnershipMismatch = "UT-MOD-OWNERSHIP-003";
    public const string DuplicateCompilerFactOwner = "UT-COMPILER-FACT-001";
    public const string UnknownCompilerFact = "UT-COMPILER-FACT-002";
    public const string ForeignCompilerFactProduction = "UT-COMPILER-FACT-003";
    public const string InvalidPipelineEffect = "UT-PIPELINE-EFFECT-001";
    public const string MissingRequiredCompilerFact = "UT-PIPELINE-EFFECT-002";
    public const string CompilerFactReverificationRequired = "UT-PIPELINE-EFFECT-003";
    public const string MissingPipelineOrder = "UT-PIPELINE-EFFECT-004";
    public const string UnknownBytecodeTag = "UT-BYTECODE-TAG-001";
    public const string UndeclaredBytecodeProducer = "UT-BYTECODE-TAG-002";
    public const string UnknownBytecodePattern = "UT-BYTECODE-PATTERN-001";
    public const string BytecodeStackEffectMismatch = "UT-BYTECODE-STACK-001";
    public const string InvalidBytecodeContractMetadata = "UT-BYTECODE-METADATA-001";
    public const string InvalidAirOperandSchema = "UT-AIR-SCHEMA-001";
    public const string MissingAirBranchTarget = "UT-AIR-BRANCH-001";
    public const string DuplicateAirLabel = "UT-AIR-BRANCH-002";
    public const string UnsupportedAirIntrinsic = "UT-AIR-INTRINSIC-001";
    public const string InterpreterBackendIntrinsicViolation = "UT-AIR-INTRINSIC-002";
    public const string MissingBackendCapability = "UT-AIR-CAPABILITY-001";
    public const string UnknownBackendCapability = "UT-BACKEND-CAPABILITY-001";
    public const string MultipleBackendCapabilityFacets = "UT-BACKEND-CAPABILITY-002";
}
