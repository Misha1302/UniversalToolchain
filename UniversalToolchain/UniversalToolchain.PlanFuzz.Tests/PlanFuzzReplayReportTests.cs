namespace UniversalToolchain.PlanFuzz.Tests;

[TestFixture]
public sealed class PlanFuzzReplayReportTests
{
    [Test]
    public void InconclusiveOracleResultIsNotReportedAsCleanOrFlaky()
    {
        var observation = CreateObservation();
        var oracleResult = new PlanFuzzOracleResult(
            "contract",
            PlanFuzzOracleIds.BackendParity,
            1,
            PlanFuzzOracleStatus.Inconclusive,
            "Evidence is incomplete.",
            "incomplete");
        var attempt = new PlanFuzzReplayAttempt(1, [observation], [oracleResult]);

        var report = new PlanFuzzReplayReport("case", [attempt]);

        AssertInconclusive(report);
    }

    [Test]
    public void ViolationWithAnotherInconclusiveOracleIsNotConfirmed()
    {
        var observation = CreateObservation();
        var violation = new PlanFuzzOracleResult(
            "violated-contract",
            PlanFuzzOracleIds.BackendParity,
            1,
            PlanFuzzOracleStatus.Violated,
            "Mismatch.",
            "mismatch");
        var inconclusive = new PlanFuzzOracleResult(
            "inconclusive-contract",
            PlanFuzzOracleIds.ControlledFallback,
            1,
            PlanFuzzOracleStatus.Inconclusive,
            "Route evidence is incomplete.",
            "missing-route");
        var attempt = new PlanFuzzReplayAttempt(1, [observation], [violation, inconclusive]);

        var report = new PlanFuzzReplayReport("case", [attempt]);

        AssertInconclusive(report);
    }

    [Test]
    public void EmptyOracleResultSetIsInconclusiveRatherThanClean()
    {
        var attempt = new PlanFuzzReplayAttempt(1, [CreateObservation()], []);

        var report = new PlanFuzzReplayReport("case", [attempt]);

        AssertInconclusive(report);
    }

    [Test]
    public void StableViolationsWithDifferentExactFingerprintsAreFlaky()
    {
        var first = new PlanFuzzReplayAttempt(
            1,
            [CreateObservation()],
            [CreateViolation("first")]);
        var second = new PlanFuzzReplayAttempt(
            2,
            [CreateObservation()],
            [CreateViolation("second")]);

        var report = new PlanFuzzReplayReport("case", [first, second]);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsConfirmedViolation, Is.False);
            Assert.That(report.IsClean, Is.False);
            Assert.That(report.IsInfrastructureFailure, Is.False);
            Assert.That(report.IsInconclusive, Is.False);
            Assert.That(report.IsFlaky, Is.True);
        });
    }

    private static PlanFuzzObservation CreateObservation() =>
        new(
            "case",
            "variant",
            "backend",
            PlanFuzzExecutionOutcome.Success,
            PlanFuzzValueSnapshot.FromDecimal(1m),
            null,
            null);

    private static PlanFuzzOracleResult CreateViolation(string material) =>
        new(
            "contract",
            PlanFuzzOracleIds.BackendParity,
            1,
            PlanFuzzOracleStatus.Violated,
            "Mismatch.",
            material);

    private static void AssertInconclusive(PlanFuzzReplayReport report)
    {
        Assert.Multiple(() =>
        {
            Assert.That(report.IsClean, Is.False);
            Assert.That(report.IsConfirmedViolation, Is.False);
            Assert.That(report.IsInfrastructureFailure, Is.False);
            Assert.That(report.IsFlaky, Is.False);
            Assert.That(report.IsInconclusive, Is.True);
            Assert.That(report.ConfirmedFingerprint, Is.Null);
            Assert.That(report.ConfirmedClassFingerprint, Is.Null);
        });
    }
}
