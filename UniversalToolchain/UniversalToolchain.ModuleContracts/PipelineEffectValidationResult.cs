namespace UniversalToolchain.ModuleContracts;

public sealed record PipelineEffectValidationResult(
    CompilerFactState OutputFacts,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics,
    IReadOnlyList<VerificationObligation> VerificationObligations)
{
    /// <summary>
    /// Compatibility projection used by the frozen P0--P3 experiment protocol.
    /// New protocol versions consume <see cref="VerificationObligations"/> directly.
    /// </summary>
    public IReadOnlyList<ReverificationRequest> ReverificationRequests => VerificationObligations
        .GroupBy(static obligation => obligation.RuleId)
        .OrderBy(static group => group.Key.Value, StringComparer.Ordinal)
        .Select(static group => new ReverificationRequest(
            group.Key,
            group.Select(static obligation => obligation.FactId)
                .Distinct()
                .OrderBy(static fact => fact.Value, StringComparer.Ordinal)
                .ToArray()))
        .ToArray();
}
