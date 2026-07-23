namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Provides one oracle contract with the exact observations selected by that contract.
/// </summary>
public sealed class PlanFuzzOracleContext
{
    public PlanFuzzOracleContext(
        PlanFuzzTestCase testCase,
        PlanFuzzOracleContract contract,
        IReadOnlyDictionary<string, PlanFuzzObservation> observations)
    {
        TestCase = testCase.ArgNotNull();
        Contract = contract.ArgNotNull();
        Observations = observations.ArgNotNull();
    }

    public PlanFuzzTestCase TestCase { get; }
    public PlanFuzzOracleContract Contract { get; }
    public IReadOnlyDictionary<string, PlanFuzzObservation> Observations { get; }

    public bool TryGetObservation(string variantId, out PlanFuzzObservation observation) =>
        Observations.TryGetValue(variantId, out observation!);
}
