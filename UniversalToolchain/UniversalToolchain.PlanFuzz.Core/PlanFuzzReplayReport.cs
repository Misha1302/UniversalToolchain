namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Summarizes strict replay confirmation without conflating clean, flaky, and infrastructure outcomes.
/// </summary>
public sealed class PlanFuzzReplayReport
{
    public PlanFuzzReplayReport(string caseId, IEnumerable<PlanFuzzReplayAttempt> attempts)
    {
        if (string.IsNullOrWhiteSpace(caseId))
            Thrower.Argument(nameof(caseId), "Case ID must not be empty.");
        var snapshot = attempts.ArgNotNull().OrderBy(static attempt => attempt.AttemptNumber).ToArray();
        if (snapshot.Length == 0)
            Thrower.Argument(nameof(attempts), "Replay report must contain at least one attempt.");

        CaseId = caseId;
        Attempts = new ReadOnlyCollection<PlanFuzzReplayAttempt>(snapshot);
        IsConfirmedViolation = snapshot.All(static attempt => attempt.HasViolation && !attempt.HasInfrastructureFailure) &&
                               snapshot.Select(static attempt => attempt.Fingerprint).Distinct(StringComparer.Ordinal).Count() == 1;
        IsClean = snapshot.All(static attempt =>
            !attempt.HasInfrastructureFailure &&
            attempt.OracleResults.All(static result => result.Status == PlanFuzzOracleStatus.Passed));
        IsInfrastructureFailure = snapshot.Any(static attempt => attempt.HasInfrastructureFailure);
        IsFlaky = !IsConfirmedViolation && !IsClean && !IsInfrastructureFailure;
        ConfirmedFingerprint = IsConfirmedViolation ? snapshot[0].Fingerprint : null;
        ConfirmedClassFingerprint = IsConfirmedViolation ? snapshot[0].ClassFingerprint : null;
    }

    public string CaseId { get; }
    public IReadOnlyList<PlanFuzzReplayAttempt> Attempts { get; }
    public bool IsConfirmedViolation { get; }
    public bool IsClean { get; }
    public bool IsFlaky { get; }
    public bool IsInfrastructureFailure { get; }
    public string? ConfirmedFingerprint { get; }
    public string? ConfirmedClassFingerprint { get; }
}
