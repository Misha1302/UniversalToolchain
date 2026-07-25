namespace UniversalToolchain.PlanFuzz.Tests;

[TestFixture]
public sealed class O005AdversarialReviewTests
{
    [Test]
    public void SchemaVersionFourRejectsForgedCurrentEvidenceContractVersion()
    {
        const string json = """
        {
          "schemaVersion": 4,
          "canonicalization": "planfuzz-json-v1",
          "caseId": "case",
          "variantId": "baseline",
          "backendId": "backend",
          "outcome": "Success",
          "value": { "typeIdentity": "System.Int32", "canonicalValue": "1" },
          "surface": {
            "evidenceContractVersion": 3,
            "selectedSurfaceIds": ["feature:core"],
            "selectedOwnerIds": ["contribution:active"],
            "excludedOwnerIds": [],
            "declaredIndependentSurfaceIds": [],
            "declaredIndependentOwnerIds": [],
            "activatedOwnerIds": ["contribution:active"],
            "activationTraceStatus": "Complete",
            "traceKind": "forged-schema-v4-evidence-v3",
            "routeIdentity": "route:baseline"
          }
        }
        """;

        Assert.That(
            () => PlanFuzzObservationSerializer.Deserialize(json),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void ExtensionNoninterferenceRejectsBindingReassociationHiddenByDelimiterCollision()
    {
        const string collisionSurface = "b|surfaces:c";
        var baselineVariant = new PlanFuzzPlanVariant(
            "baseline",
            "baseline",
            "backend",
            PlanFuzzVariantRole.Baseline,
            PlanFuzzExpectedRelation.SameSemantics);
        var extendedVariant = new PlanFuzzPlanVariant(
            "extended",
            "extended",
            "backend",
            PlanFuzzVariantRole.EquivalentMutation,
            PlanFuzzExpectedRelation.SameSemantics);
        var contract = new PlanFuzzOracleContract(
            "extension",
            PlanFuzzOracleIds.ExtensionNoninterference,
            3,
            [baselineVariant.VariantId, extendedVariant.VariantId]);
        var testCase = new PlanFuzzTestCase(
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
            [baselineVariant, extendedVariant],
            [contract]);

        var baselineSurface = CreateSurface(
        [
            new PlanFuzzIndependentExtensionEvidence(
                "a",
                [collisionSurface],
                ["contribution:d"]),
            new PlanFuzzIndependentExtensionEvidence(
                "q|surfaces:b",
                ["c"],
                ["contribution:e"])
        ]);
        var extendedSurface = CreateSurface(
        [
            new PlanFuzzIndependentExtensionEvidence(
                "a|surfaces:b",
                ["c"],
                ["contribution:d"]),
            new PlanFuzzIndependentExtensionEvidence(
                "q",
                [collisionSurface],
                ["contribution:e"])
        ]);
        var baseline = CreateObservation(testCase, baselineVariant, baselineSurface);
        var extended = CreateObservation(testCase, extendedVariant, extendedSurface);

        var result = new PlanFuzzOracleEngine().Evaluate(testCase, [baseline, extended]).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(PlanFuzzOracleStatus.InfrastructureFailure));
            Assert.That(result.FingerprintMaterial, Is.EqualTo("invalid-or-ambiguous-delta"));
        });
    }

    private static PlanFuzzSurfaceSnapshot CreateSurface(
        IEnumerable<PlanFuzzIndependentExtensionEvidence> bindings) =>
        new(
            PlanFuzzSurfaceSnapshot.CurrentEvidenceContractVersion,
            ["feature:core", "b|surfaces:c", "c"],
            ["contribution:active", "contribution:d", "contribution:e"],
            [],
            ["b|surfaces:c", "c"],
            ["contribution:d", "contribution:e"],
            bindings,
            ["contribution:active"],
            PlanFuzzActivationTraceStatus.Complete,
            "test-trace-v3",
            "route:baseline");

    private static PlanFuzzObservation CreateObservation(
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
