namespace UniversalToolchain.ModuleContracts;

public interface ICompilerFactVerifierRuleProvider
{
    IReadOnlyDictionary<CompilerFactId, VerifierRuleId> GetRules();
}

public sealed class CoreCompilerFactVerifierRuleProvider : ICompilerFactVerifierRuleProvider
{
    public IReadOnlyDictionary<CompilerFactId, VerifierRuleId> GetRules() =>
        CompilerFactVerifierRegistry.CreateCoreRules();
}

public sealed class CompilerFactVerifierRegistry
{
    private readonly IReadOnlyDictionary<CompilerFactId, VerifierRuleId> _rulesByFact;

    public CompilerFactVerifierRegistry(IReadOnlyDictionary<CompilerFactId, VerifierRuleId> rulesByFact)
    {
        _rulesByFact = new Dictionary<CompilerFactId, VerifierRuleId>(rulesByFact.ArgNotNull());
    }

    public CompilerFactVerifierRegistry(IEnumerable<ICompilerFactVerifierRuleProvider> providers)
        : this(BuildRules(providers))
    {
    }

    public static CompilerFactVerifierRegistry Core { get; } = new(CreateCoreRules());

    internal static IReadOnlyDictionary<CompilerFactId, VerifierRuleId> CreateCoreRules() =>
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
        };

    public bool TryGetVerifier(CompilerFactId factId, out VerifierRuleId ruleId) =>
        _rulesByFact.TryGetValue(factId, out ruleId);

    private static IReadOnlyDictionary<CompilerFactId, VerifierRuleId> BuildRules(
        IEnumerable<ICompilerFactVerifierRuleProvider> providers)
    {
        providers = providers.ArgNotNull();
        var normalizedProviders = providers
            .Select(static provider => provider.ArgNotNull())
            .OrderBy(static provider => provider.GetType().FullName, StringComparer.Ordinal)
            .ToArray();
        var rules = new Dictionary<CompilerFactId, VerifierRuleId>();
        foreach (var provider in normalizedProviders)
        {
            var providerRules = provider.GetRules().ArgNotNull();
            foreach (var (fact, rule) in providerRules
                         .OrderBy(static pair => pair.Key.Value, StringComparer.Ordinal))
            {
                if (rules.TryGetValue(fact, out var existingRule) && existingRule != rule)
                {
                    throw new InvalidOperationException(
                        $"Compiler fact '{fact}' is routed to conflicting verifier rules '{existingRule}' and '{rule}'.");
                }

                rules[fact] = rule;
            }
        }

        return rules;
    }
}
