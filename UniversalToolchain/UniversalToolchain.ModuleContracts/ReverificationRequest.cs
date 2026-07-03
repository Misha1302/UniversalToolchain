namespace UniversalToolchain.ModuleContracts;

public sealed record ReverificationRequest(
    VerifierRuleId RuleId,
    IReadOnlyList<CompilerFactId> InvalidatedFacts);
