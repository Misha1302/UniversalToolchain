namespace UniversalToolchain.PlanFuzz.Tests;

[TestFixture]
public sealed class SurfaceOracleTests
{
    [Test]
    public void ObservationSchemaVersionTwoRemainsReadableWithoutSurfaceEvidence()
    {
        const string json = """
        {
          "schemaVersion": 2,
          "canonicalization": "planfuzz-json-v1",
          "caseId": "case",
          "variantId": "variant",
          "backendId": "backend",
          "outcome": "Success",
          "value": {
            "typeIdentity": "System.Int32",
            "canonicalValue": "1"
          }
        }
        """;

        var observation = PlanFuzzObservationSerializer.Deserialize(json);

        Assert.Multiple(() =>
        {
            Assert.That(observation.Value?.CanonicalValue, Is.EqualTo("1"));
            Assert.That(observation.Surface, Is.Null);
        });
    }

    [Test]
    public void NegativeSurfaceRequiresCompleteTrace()
    {
        var (testCase, variant) = CreateSingleVariantCase(PlanFuzzOracleIds.NegativeSurfacePreservation);
        var observation = Observation(
            testCase,
            variant,
            1,
            new PlanFuzzSurfaceSnapshot(
                ["feature:core"],
                ["contribution:excluded"],
                [],
                [],
                activationTraceComplete: false,
                "test-trace-v1",
                "route:baseline"));

        var result = new PlanFuzzOracleEngine().Evaluate(testCase, [observation]).Single();

        Assert.That(result.Status, Is.EqualTo(PlanFuzzOracleStatus.Inconclusive));
    }

    [Test]
    public void ExtensionNoninterferenceRequiresDeclaredPureAdditiveDelta()
    {
        var variants = new[]
        {
            Variant("baseline"),
            Variant("extended", PlanFuzzVariantRole.EquivalentMutation)
        };
        var contract = new PlanFuzzOracleContract(
            "extension",
            PlanFuzzOracleIds.ExtensionNoninterference,
            1,
            variants.Select(static variant => variant.VariantId));
        var testCase = CreateCase(variants, [contract]);
        var baseline = Observation(
            testCase,
            variants[0],
            1,
            Surface(["feature:core"], [], [], ["contribution:active"]));
        var extended = Observation(
            testCase,
            variants[1],
            1,
            Surface(["feature:core", "feature:extension"], [], [], ["contribution:active"]));

        var result = new PlanFuzzOracleEngine().Evaluate(testCase, [baseline, extended]).Single();

        Assert.That(result.Status, Is.EqualTo(PlanFuzzOracleStatus.NotApplicable));
    }

    [Test]
    public void ExtensionNoninterferenceRejectsRouteChangeEvenWhenValuesMatch()
    {
        var variants = new[]
        {
            Variant("baseline"),
            Variant("extended", PlanFuzzVariantRole.EquivalentMutation)
        };
        var contract = new PlanFuzzOracleContract(
            "extension",
            PlanFuzzOracleIds.ExtensionNoninterference,
            1,
            variants.Select(static variant => variant.VariantId));
        var testCase = CreateCase(variants, [contract]);
        var baseline = Observation(
            testCase,
            variants[0],
            1,
            Surface(["feature:core"], [], [], ["contribution:active"], "route:baseline"));
        var extended = Observation(
            testCase,
            variants[1],
            1,
            Surface(
                ["feature:core", "feature:extension"],
                [],
                ["feature:extension"],
                ["contribution:active"],
                "route:changed"));

        var result = new PlanFuzzOracleEngine().Evaluate(testCase, [baseline, extended]).Single();

        Assert.That(result.Status, Is.EqualTo(PlanFuzzOracleStatus.Violated));
        Assert.That(result.EffectiveClassFingerprintMaterial, Is.EqualTo("extension-route-changed"));
    }

    private static (PlanFuzzTestCase TestCase, PlanFuzzPlanVariant Variant) CreateSingleVariantCase(string oracleId)
    {
        var variant = Variant("baseline");
        var contract = new PlanFuzzOracleContract("surface", oracleId, 1, [variant.VariantId]);
        return (CreateCase([variant], [contract]), variant);
    }

    private static PlanFuzzTestCase CreateCase(
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
            new PlanFuzzProgram("test", 1, PlanFuzzPayload.FromJson("{}"), "1", PlanFuzzProgramClass.ValidDeterministic),
            variants,
            contracts);

    private static PlanFuzzPlanVariant Variant(
        string id,
        PlanFuzzVariantRole role = PlanFuzzVariantRole.Baseline) =>
        new(id, id, "backend", role, PlanFuzzExpectedRelation.SameSemantics);

    private static PlanFuzzObservation Observation(
        PlanFuzzTestCase testCase,
        PlanFuzzPlanVariant variant,
        int value,
        PlanFuzzSurfaceSnapshot surface) =>
        new(
            testCase.CaseId,
            variant.VariantId,
            variant.BackendId,
            PlanFuzzExecutionOutcome.Success,
            PlanFuzzValueSnapshot.FromInt32(value),
            null,
            null,
            null,
            surface);

    private static PlanFuzzSurfaceSnapshot Surface(
        IEnumerable<string> selected,
        IEnumerable<string> excluded,
        IEnumerable<string> independent,
        IEnumerable<string> activated,
        string routeIdentity = "route:baseline") =>
        new(selected, excluded, independent, activated, true, "test-trace-v1", routeIdentity);
}
