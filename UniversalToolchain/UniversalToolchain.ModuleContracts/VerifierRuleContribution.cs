namespace UniversalToolchain.ModuleContracts;

public sealed record VerifierRuleContribution(
    VerifierRuleId RuleId,
    IReadOnlyList<BytecodePatternId> BytecodePatterns,
    IReadOnlyList<AirPatternId> AirPatterns);
