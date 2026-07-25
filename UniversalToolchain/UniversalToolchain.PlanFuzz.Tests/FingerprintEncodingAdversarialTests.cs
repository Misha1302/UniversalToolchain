namespace UniversalToolchain.PlanFuzz.Tests;

[TestFixture]
public sealed class FingerprintEncodingAdversarialTests
{
    [Test]
    public void ExtensionNoninterferenceDistinguishesCommaContainingOwnerFromTwoOwners()
    {
        var singleOwner = EvaluateExtensionActivationViolation(
            ["feature:s,t"],
            ["contribution:a,b"]);
        var twoOwners = EvaluateExtensionActivationViolation(
            ["feature:s", "feature:t"],
            ["contribution:a", "contribution:b"]);

        Assert.Multiple(() =>
        {
            Assert.That(singleOwner.Status, Is.EqualTo(PlanFuzzOracleStatus.Violated));
            Assert.That(twoOwners.Status, Is.EqualTo(PlanFuzzOracleStatus.Violated));
            Assert.That(singleOwner.FingerprintMaterial, Is.Not.EqualTo(twoOwners.FingerprintMaterial));
            Assert.That(singleOwner.EffectiveClassFingerprintMaterial, Is.EqualTo(twoOwners.EffectiveClassFingerprintMaterial));
        });
    }

    [Test]
    public void NegativeSurfaceDistinguishesCommaContainingOwnerFromTwoOwners()
    {
        var singleOwner = EvaluateNegativeSurfaceViolation(["contribution:a,b"]);
        var twoOwners = EvaluateNegativeSurfaceViolation(["contribution:a", "contribution:b"]);

        Assert.Multiple(() =>
        {
            Assert.That(singleOwner.Status, Is.EqualTo(PlanFuzzOracleStatus.Violated));
            Assert.That(twoOwners.Status, Is.EqualTo(PlanFuzzOracleStatus.Violated));
            Assert.That(singleOwner.FingerprintMaterial, Is.Not.EqualTo(twoOwners.FingerprintMaterial));
            Assert.That(singleOwner.EffectiveClassFingerprintMaterial, Is.Not.EqualTo(twoOwners.EffectiveClassFingerprintMaterial));
        });
    }

    private static PlanFuzzOracleResult EvaluateExtensionActivationViolation(
        string[] addedSurfaces,
        string[] addedOwners)
    {
        var baselineVariant = Variant("baseline", PlanFuzzVariantRole.Baseline);
        var extendedVariant = Variant("extended", PlanFuzzVariantRole.EquivalentMutation);
        var contract = new PlanFuzzOracleContract(
            "extension",
            PlanFuzzOracleIds.ExtensionNoninterference,
            3,
            [baselineVariant.VariantId, extendedVariant.VariantId]);
        var testCase = Case([baselineVariant, extendedVariant], [contract]);
        var baseline = Observation(
            testCase,
            baselineVariant,
            Surface(
                ["feature:core"],
                ["contribution:z"],
                addedOwners,
                [],
                [],
                [],
                ["contribution:z"]));
        var extended = Observation(
            testCase,
            extendedVariant,
            Surface(
                ["feature:core", .. addedSurfaces],
                ["contribution:z", .. addedOwners],
                [],
                addedSurfaces,
                addedOwners,
                [new PlanFuzzIndependentExtensionEvidence("extension:test", addedSurfaces, addedOwners)],
                ["contribution:z", .. addedOwners]));

        return new PlanFuzzOracleEngine().Evaluate(testCase, [baseline, extended]).Single();
    }

    private static PlanFuzzOracleResult EvaluateNegativeSurfaceViolation(string[] owners)
    {
        var variant = Variant("negative", PlanFuzzVariantRole.Baseline);
        var contract = new PlanFuzzOracleContract(
            "negative",
            PlanFuzzOracleIds.NegativeSurfacePreservation,
            2,
            [variant.VariantId]);
        var testCase = Case([variant], [contract]);
        var observation = Observation(
            testCase,
            variant,
            Surface(
                ["feature:core"],
                ["contribution:z"],
                owners,
                [],
                [],
                [],
                ["contribution:z", .. owners]));

        return new PlanFuzzOracleEngine().Evaluate(testCase, [observation]).Single();
    }

    private static PlanFuzzPlanVariant Variant(string id, PlanFuzzVariantRole role) =>
        new(id, id, "backend", role, PlanFuzzExpectedRelation.SameSemantics);

    private static PlanFuzzTestCase Case(
        IEnumerable<PlanFuzzPlanVariant> variants,
        IEnumerable<PlanFuzzOracleContract> contracts) =>
        new(
            PlanFuzzConstants.CaseSchemaVersion,
            "test-adapter",
            "1.0.0",
            1,
            0,
            1,
            PlanFuzzRandom.AlgorithmId,
            new PlanFuzzProgram(
                "test",
                1,
                PlanFuzzPayload.FromJson("{}"),
                "1",
                PlanFuzzProgramClass.ValidDeterministic),
            variants,
            contracts);

    private static PlanFuzzSurfaceSnapshot Surface(
        IEnumerable<string> selectedSurfaces,
        IEnumerable<string> selectedOwners,
        IEnumerable<string> excludedOwners,
        IEnumerable<string> independentSurfaces,
        IEnumerable<string> independentOwners,
        IEnumerable<PlanFuzzIndependentExtensionEvidence> bindings,
        IEnumerable<string> activatedOwners) =>
        new(
            PlanFuzzSurfaceSnapshot.CurrentEvidenceContractVersion,
            selectedSurfaces,
            selectedOwners,
            excludedOwners,
            independentSurfaces,
            independentOwners,
            bindings,
            activatedOwners,
            PlanFuzzActivationTraceStatus.Complete,
            "test-trace-v3",
            "route:baseline");

    private static PlanFuzzObservation Observation(
        PlanFuzzTestCase testCase,
        PlanFuzzPlanVariant variant,
        PlanFuzzSurfaceSnapshot surface) =>
        new(
            testCase.CaseId,
            variant.VariantId,
            variant.BackendId,
            PlanFuzzExecutionOutcome.Success,
            PlanFuzzValueSnapshot.FromInt32(1),
            null,
            null,
            null,
            surface);
}
