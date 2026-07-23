namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Evaluates testcase-declared oracle contracts through an explicit generic oracle registry.
/// </summary>
public sealed class PlanFuzzOracleEngine
{
    private readonly IReadOnlyDictionary<string, IPlanFuzzOracle> _oracles;

    public PlanFuzzOracleEngine(IEnumerable<IPlanFuzzOracle>? oracles = null)
    {
        var selected = (oracles ?? CreateDefaults()).ToArray();
        if (selected.Select(static oracle => oracle.OracleId).Distinct(StringComparer.Ordinal).Count() != selected.Length)
            Thrower.InvalidOpEx("PlanFuzz oracle IDs must be unique.");
        _oracles = new ReadOnlyDictionary<string, IPlanFuzzOracle>(
            selected.ToDictionary(static oracle => oracle.OracleId, StringComparer.Ordinal));
    }

    public IReadOnlyList<PlanFuzzOracleResult> Evaluate(
        PlanFuzzTestCase testCase,
        IEnumerable<PlanFuzzObservation> observations)
    {
        testCase = testCase.ArgNotNull();
        var observationMap = observations.ArgNotNull().ToDictionary(static observation => observation.VariantId, StringComparer.Ordinal);
        var results = new List<PlanFuzzOracleResult>();
        foreach (var contract in testCase.OracleContracts)
        {
            if (!_oracles.TryGetValue(contract.OracleId, out var oracle))
            {
                results.Add(new PlanFuzzOracleResult(
                    contract.ContractId,
                    contract.OracleId,
                    contract.OracleVersion,
                    PlanFuzzOracleStatus.InfrastructureFailure,
                    $"Oracle '{contract.OracleId}' is not registered.",
                    "oracle-not-registered"));
                continue;
            }
            if (oracle.OracleVersion != contract.OracleVersion)
            {
                results.Add(new PlanFuzzOracleResult(
                    contract.ContractId,
                    contract.OracleId,
                    contract.OracleVersion,
                    PlanFuzzOracleStatus.InfrastructureFailure,
                    $"Oracle version mismatch: testcase requires {contract.OracleVersion}, runtime provides {oracle.OracleVersion}.",
                    "oracle-version-mismatch"));
                continue;
            }
            results.Add(oracle.Evaluate(new PlanFuzzOracleContext(testCase, contract, observationMap)));
        }
        return new ReadOnlyCollection<PlanFuzzOracleResult>(results);
    }

    private static IEnumerable<IPlanFuzzOracle> CreateDefaults() =>
    [
        new BackendParityOracle(),
        new PlanDeterminismOracle(),
        new CanonicalLockConsistencyOracle()
    ];
}
