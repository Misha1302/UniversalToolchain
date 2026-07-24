namespace UniversalToolchain.PlanFuzz.Tests;

[TestFixture]
public sealed class PlanFuzzFindingClassificationTests
{
    [Test]
    public void ExactReplayFingerprintRemainsCaseSensitiveWhileClassFingerprintGroupsTheSameFailureShape()
    {
        var observation = new PlanFuzzObservation(
            "case",
            "variant",
            "backend",
            PlanFuzzExecutionOutcome.Success,
            PlanFuzzValueSnapshot.FromInt32(1),
            null,
            null);
        var firstResult = new PlanFuzzOracleResult(
            "contract",
            PlanFuzzOracleIds.BackendParity,
            1,
            PlanFuzzOracleStatus.Violated,
            "Mismatch.",
            "interpreter:success:System.Int32:1|compiler:failure",
            "interpreter:success:System.Int32|compiler:failure");
        var secondResult = firstResult with
        {
            FingerprintMaterial = "interpreter:success:System.Int32:2|compiler:failure"
        };

        var first = new PlanFuzzReplayAttempt(1, [observation], [firstResult]);
        var second = new PlanFuzzReplayAttempt(1, [observation], [secondResult]);

        Assert.Multiple(() =>
        {
            Assert.That(first.Fingerprint, Is.Not.EqualTo(second.Fingerprint));
            Assert.That(first.ClassFingerprint, Is.EqualTo(second.ClassFingerprint));
        });
    }

    [Test]
    public void DuplicateRouteDiagnosticsChangeExactEvidenceButNotTheFindingClass()
    {
        var variant = new PlanFuzzPlanVariant(
            "routed",
            "configuration",
            "compiler",
            PlanFuzzVariantRole.EquivalentMutation,
            PlanFuzzExpectedRelation.SameSemantics);
        var contract = new PlanFuzzOracleContract(
            "fallback",
            PlanFuzzOracleIds.ControlledFallback,
            1,
            [variant.VariantId]);
        var testCase = new PlanFuzzTestCase(
            PlanFuzzConstants.CaseSchemaVersion,
            "adapter",
            "1",
            1,
            0,
            1,
            PlanFuzzRandom.AlgorithmId,
            new PlanFuzzProgram(
                "model",
                1,
                PlanFuzzPayload.FromJson("{}"),
                "source",
                PlanFuzzProgramClass.ValidDeterministic),
            [variant],
            [contract]);

        PlanFuzzOracleResult Evaluate(int diagnosticCount)
        {
            var route = new PlanFuzzRouteSnapshot(
                "route",
                "Prefer",
                usedRoute: false,
                fellBack: true,
                PlanFuzzFallbackKind.Unclassified,
                diagnostics: Enumerable.Range(0, diagnosticCount)
                    .Select(static _ => new PlanFuzzRouteDiagnosticSnapshot("same.code", "lowering")));
            var observation = new PlanFuzzObservation(
                testCase.CaseId,
                variant.VariantId,
                variant.BackendId,
                PlanFuzzExecutionOutcome.Success,
                PlanFuzzValueSnapshot.FromInt32(1),
                null,
                null,
                route);
            return new ControlledFallbackOracle().Evaluate(new PlanFuzzOracleContext(
                testCase,
                contract,
                new Dictionary<string, PlanFuzzObservation>(StringComparer.Ordinal)
                {
                    [variant.VariantId] = observation
                }));
        }

        var once = Evaluate(1);
        var repeated = Evaluate(4);

        Assert.Multiple(() =>
        {
            Assert.That(once.FingerprintMaterial, Is.Not.EqualTo(repeated.FingerprintMaterial));
            Assert.That(once.EffectiveClassFingerprintMaterial, Is.EqualTo(repeated.EffectiveClassFingerprintMaterial));
        });
    }
}
