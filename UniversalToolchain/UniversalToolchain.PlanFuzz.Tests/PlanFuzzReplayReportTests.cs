namespace UniversalToolchain.PlanFuzz.Tests;

[TestFixture]
public sealed class PlanFuzzReplayReportTests
{
    [Test]
    public void InconclusiveOracleResultIsNotReportedAsClean()
    {
        var observation = new PlanFuzzObservation(
            "case",
            "variant",
            "backend",
            PlanFuzzExecutionOutcome.Success,
            PlanFuzzValueSnapshot.FromDecimal(1m),
            null,
            null);
        var oracleResult = new PlanFuzzOracleResult(
            "contract",
            PlanFuzzOracleIds.BackendParity,
            1,
            PlanFuzzOracleStatus.Inconclusive,
            "Evidence is incomplete.",
            "incomplete");
        var attempt = new PlanFuzzReplayAttempt(1, [observation], [oracleResult]);

        var report = new PlanFuzzReplayReport("case", [attempt]);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsClean, Is.False);
            Assert.That(report.IsConfirmedViolation, Is.False);
            Assert.That(report.IsInfrastructureFailure, Is.False);
            Assert.That(report.IsFlaky, Is.True);
        });
    }
}
