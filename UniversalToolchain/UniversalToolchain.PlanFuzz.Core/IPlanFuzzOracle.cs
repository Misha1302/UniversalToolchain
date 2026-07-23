namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Evaluates one generic relation over a contract-selected observation set.
/// </summary>
public interface IPlanFuzzOracle
{
    string OracleId { get; }
    int OracleVersion { get; }
    PlanFuzzOracleResult Evaluate(PlanFuzzOracleContext context);
}
