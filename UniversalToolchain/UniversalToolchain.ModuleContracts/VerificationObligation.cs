namespace UniversalToolchain.ModuleContracts;

/// <summary>
/// A named semantic fact that must be discharged by its canonical verifier owner before the
/// artifact crosses the first eligible boundary under an obligation-enforcing policy.
/// </summary>
public sealed record VerificationObligation(
    CompilerFactId FactId,
    VerifierRuleId RuleId,
    string CanonicalOwner,
    CompilerPipelineStage CreationBoundary,
    CompilerPipelineStage FirstEligibleBoundary);
