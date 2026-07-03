namespace UniversalToolchain.ModuleContracts;

public sealed class CompilerFactVerifierRegistry
{
    private readonly IReadOnlyDictionary<CompilerFactId, VerifierRuleId> _rulesByFact;

    public CompilerFactVerifierRegistry(IReadOnlyDictionary<CompilerFactId, VerifierRuleId> rulesByFact)
    {
        _rulesByFact = rulesByFact.ArgNotNull();
    }

    public static CompilerFactVerifierRegistry Core { get; } = new(
        new Dictionary<CompilerFactId, VerifierRuleId>
        {
            [KnownCoreCompilerFacts.BytecodeVerified] = KnownCoreVerifierRules.BytecodeContract,
            [KnownCoreCompilerFacts.AirSchemaValid] = KnownCoreVerifierRules.AirContract,
            [KnownCoreCompilerFacts.AirBranchTargetsValid] = KnownCoreVerifierRules.AirContract,
            [KnownCoreCompilerFacts.AirStackBalanced] = KnownCoreVerifierRules.AirContract,
            [KnownCoreCompilerFacts.AirBranchStackCompatible] = KnownCoreVerifierRules.AirContract,
            [KnownCoreCompilerFacts.AirIntrinsicsSupported] = KnownCoreVerifierRules.AirContract,
            [KnownCoreCompilerFacts.AirVerified] = KnownCoreVerifierRules.AirContract,
            [KnownCoreCompilerFacts.BackendInputVerified] = KnownCoreVerifierRules.BackendInputContract
        });

    public bool TryGetVerifier(CompilerFactId factId, out VerifierRuleId ruleId) =>
        _rulesByFact.TryGetValue(factId, out ruleId);
}
