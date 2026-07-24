namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Stores one stable route diagnostic used by route and fallback oracles.
/// </summary>
public sealed record PlanFuzzRouteDiagnosticSnapshot
{
    public PlanFuzzRouteDiagnosticSnapshot(string code, string? stage)
    {
        if (string.IsNullOrWhiteSpace(code))
            Thrower.Argument(nameof(code), "Route diagnostic code must not be empty.");
        Code = code;
        Stage = string.IsNullOrWhiteSpace(stage) ? null : stage.Trim();
    }

    public string Code { get; }
    public string? Stage { get; }
}
