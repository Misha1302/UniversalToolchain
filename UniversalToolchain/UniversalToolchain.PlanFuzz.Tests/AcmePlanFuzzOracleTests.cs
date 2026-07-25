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
    [Test]
    public void StructuredProgramReductionIsDeterministicAndStrictlyDecreasesComplexity()
    {
        var adapter = new AcmePlanFuzzAdapter();
        var generated = adapter.GenerateCase(
            123,
            0,
            new PlanFuzzCaseGenerationOptions(AcmePlanFuzzConstants.WrongArithmeticFault));
        var model = new AcmePricingProgramModel(13m, 7m, 4m);
        var testCase = PlanFuzzTestCaseTransform.WithProgram(
            generated,
            new PlanFuzzProgram(
                AcmePlanFuzzConstants.ModelKind,
                AcmePlanFuzzConstants.ModelSchemaVersion,
                model.ToPayload(),
                model.RenderSource(),
                PlanFuzzProgramClass.ValidDeterministic));
        var reducer = (IPlanFuzzProgramReducer)adapter;

        var first = reducer.GetProgramReductionCandidates(testCase);
        var second = reducer.GetProgramReductionCandidates(testCase);
        var originalComplexity = reducer.GetProgramComplexity(testCase);
        var factorCandidate = first.Single(static candidate =>
            candidate.CandidateId == "factor-unit-price-to-one");
        var reducedCase = PlanFuzzTestCaseTransform.WithProgram(testCase, factorCandidate.Program);
        var baselineVariant = reducedCase.GetRequiredVariant("baseline.interpreter");
        var faultVariant = reducedCase.GetRequiredVariant("seeded-wrong-arithmetic.compiled");

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(static candidate => candidate.CandidateId),
                Is.EqualTo(second.Select(static candidate => candidate.CandidateId)));
            Assert.That(first, Is.Not.Empty);
            Assert.That(first, Has.All.Property(nameof(PlanFuzzProgramReductionCandidate.Complexity)).LessThan(originalComplexity));
            Assert.That(AcmePricingProgramModel.FromPayload(factorCandidate.Program.Model).UnitPrice, Is.EqualTo(1m));
            Assert.That(adapter.Execute(reducedCase, baselineVariant).Value?.CanonicalValue,
                Is.EqualTo(adapter.Execute(testCase, testCase.GetRequiredVariant("baseline.interpreter")).Value?.CanonicalValue));
            Assert.That(adapter.Execute(reducedCase, faultVariant).Value?.CanonicalValue,
                Is.EqualTo(adapter.Execute(testCase, testCase.GetRequiredVariant("seeded-wrong-arithmetic.compiled")).Value?.CanonicalValue));
        });
    }

}
