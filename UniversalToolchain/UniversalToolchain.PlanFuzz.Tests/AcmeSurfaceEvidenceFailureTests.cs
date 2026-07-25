using UniversalToolchain.PlanFuzz.Adapter.Acme;

namespace UniversalToolchain.PlanFuzz.Tests;

[TestFixture]
public sealed class AcmeSurfaceEvidenceFailureTests
{
    [Test]
    public void SurfaceEvidenceFailureIsNormalizedWithoutEscapingAdapter()
    {
        var adapter = new AcmePlanFuzzAdapter();
        var generated = adapter.GenerateCase(123, 0, new PlanFuzzCaseGenerationOptions());
        var variant = new PlanFuzzPlanVariant(
            "surface-evidence-failure.interpreter",
            AcmePlanFuzzConstants.SurfaceEvidenceFailureConfiguration,
            AcmePlanFuzzConstants.InterpreterBackend,
            PlanFuzzVariantRole.SeededFault,
            PlanFuzzExpectedRelation.ExpectedDifference);

        var observation = adapter.Execute(generated, variant);

        Assert.Multiple(() =>
        {
            Assert.That(observation.Outcome, Is.EqualTo(PlanFuzzExecutionOutcome.InfrastructureFailure));
            Assert.That(observation.Failure?.Stage, Is.EqualTo("observation"));
            Assert.That(observation.Failure?.Category, Is.EqualTo("surface-evidence"));
            Assert.That(observation.Plan, Is.Not.Null);
            Assert.That(observation.Surface, Is.Null);
        });
    }
}
