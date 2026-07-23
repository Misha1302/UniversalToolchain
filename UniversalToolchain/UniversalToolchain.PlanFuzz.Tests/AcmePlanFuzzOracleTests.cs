using UniversalToolchain.PlanFuzz.Adapter.Acme;

namespace UniversalToolchain.PlanFuzz.Tests;

[TestFixture]
public sealed class AcmePlanFuzzOracleTests
{
    [Test]
    public void CleanAcmeCasePassesBackendPlanAndLockOracles()
    {
        var adapter = new AcmePlanFuzzAdapter();
        var testCase = adapter.GenerateCase(123, 0, new PlanFuzzCaseGenerationOptions());
        var observations = testCase.Variants.Select(variant => adapter.Execute(testCase, variant)).ToArray();
        var results = new PlanFuzzOracleEngine().Evaluate(testCase, observations);

        Assert.That(results, Has.All.Property(nameof(PlanFuzzOracleResult.Status)).EqualTo(PlanFuzzOracleStatus.Passed));
        Assert.That(observations.Select(static item => item.Plan?.PlanHash).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
    }

    [Test]
    public void WrongArithmeticSeededFaultIsDetectedWithoutCorruptingBaseline()
    {
        var adapter = new AcmePlanFuzzAdapter();
        var testCase = adapter.GenerateCase(
            123,
            0,
            new PlanFuzzCaseGenerationOptions(AcmePlanFuzzConstants.WrongArithmeticFault));
        var observations = testCase.Variants.Select(variant => adapter.Execute(testCase, variant)).ToArray();
        var results = new PlanFuzzOracleEngine().Evaluate(testCase, observations);

        Assert.Multiple(() =>
        {
            Assert.That(results.Single(result => result.ContractId == "backend-parity.baseline").Status,
                Is.EqualTo(PlanFuzzOracleStatus.Passed));
            Assert.That(results.Single(result => result.ContractId == "backend-parity.seeded-wrong-arithmetic").Status,
                Is.EqualTo(PlanFuzzOracleStatus.Violated));
            Assert.That(results.Single(result => result.ContractId == "canonical-lock.all").Status,
                Is.EqualTo(PlanFuzzOracleStatus.Passed));
        });
    }
}
