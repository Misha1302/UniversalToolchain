using System.Text.Json;

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
    public void ObservationSchemaVersionThreeRemainsReadableButCannotPassCurrentSurfaceOracles()
    {
        const string json = """
        {
          "schemaVersion": 3,
          "canonicalization": "planfuzz-json-v1",
          "caseId": "case",
          "variantId": "baseline",
          "backendId": "backend",
          "outcome": "Success",
          "value": {
            "typeIdentity": "System.Int32",
            "canonicalValue": "1"
          },
          "surface": {
            "selectedSurfaceIds": ["feature:core"],
            "excludedSurfaceIds": ["contribution:excluded"],
            "declaredIndependentSurfaceIds": [],
            "activatedOwnerIds": ["contribution:active"],
            "activationTraceComplete": true,
            "traceKind": "legacy-v1",
            "routeIdentity": "route:legacy"
          }
        }
        """;
        var variant = Variant("baseline");
        var testCase = CreateCase(
            [variant],
            [new PlanFuzzOracleContract("negative", PlanFuzzOracleIds.NegativeSurfacePreservation, 2, [variant.VariantId])]);

        var observation = PlanFuzzObservationSerializer.Deserialize(json);
        var result = new PlanFuzzOracleEngine().Evaluate(testCase, [observation]).Single();
        var roundTrip = PlanFuzzObservationSerializer.Serialize(observation);
        using var roundTripDocument = JsonDocument.Parse(roundTrip);
        var roundTrippedObservation = PlanFuzzObservationSerializer.Deserialize(roundTrip);

        Assert.Multiple(() =>
        {
            Assert.That(observation.Surface?.EvidenceContractVersion, Is.EqualTo(1));
            Assert.That(roundTripDocument.RootElement.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(3));
            Assert.That(roundTrippedObservation.Surface?.EvidenceContractVersion, Is.EqualTo(1));
            Assert.That(result.Status, Is.EqualTo(PlanFuzzOracleStatus.Inconclusive));
            Assert.That(result.FingerprintMaterial, Does.Contain("legacy-evidence"));
        });
    }

    [Test]
    public void SurfaceEvidenceRejectsBlankDuplicateAndContradictoryOwnerIds()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => Surface(
                ["feature:core"], ["contribution:active"], ["contribution:excluded"], [], [], [" "]));
            Assert.Throws<ArgumentException>(() => Surface(
                ["feature:core"], ["contribution:active", "contribution:active"], [], [], [], ["contribution:active"]));
            Assert.Throws<ArgumentException>(() => Surface(
                ["feature:core"], ["contribution:active"], ["contribution:active"], [], [], ["contribution:active"]));
            Assert.Throws<ArgumentException>(() => Surface(
                ["feature:core"], ["contribution:active"], [], [], [], ["contribution:outside"]));
            Assert.Throws<ArgumentException>(() => Surface(
                ["feature:core"], [], [], [], [], []));
            Assert.Throws<ArgumentException>(() => new PlanFuzzSurfaceSnapshot(
                99,
                ["feature:core"],
                ["contribution:active"],
                [],
                [],
                [],
                ["contribution:active"],
                PlanFuzzActivationTraceStatus.Complete,
                "test-trace-v1",
                "route:baseline"));
            Assert.Throws<ArgumentException>(() => new PlanFuzzSurfaceSnapshot(
                PlanFuzzSurfaceSnapshot.CurrentEvidenceContractVersion,
                ["feature:core"],
                ["contribution:active"],
                [],
                [],
                [],
                ["contribution:active"],
                (PlanFuzzActivationTraceStatus)999,
                "test-trace-v1",
                "route:baseline"));
        });
    }

    [Test]
    public void SurfaceEvidenceRejectsIndependentIdsOutsideSelectedDomains()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => Surface(
                ["feature:core"], ["contribution:active"], [], ["feature:extension"], [], ["contribution:active"]));
            Assert.Throws<ArgumentException>(() => Surface(
                ["feature:core"], ["contribution:active"], [], [], ["contribution:extension"], ["contribution:active"]));
        });
    }

    [Test]
    public void NegativeSurfaceViolationDominatesIncompletePeerRegardlessOfVariantOrder()
    {
        var first = Variant("a-incomplete");
        var second = Variant("z-violating");
        var testCase = CreateCase(
            [first, second],
            [new PlanFuzzOracleContract("negative", PlanFuzzOracleIds.NegativeSurfacePreservation, 2, [first.VariantId, second.VariantId])]);
        var incomplete = Observation(
            testCase,
            first,
            1,
            Surface(["feature:core"], ["contribution:active"], ["contribution:excluded"], [], [], ["contribution:active"], PlanFuzzActivationTraceStatus.Partial));
        var violating = Observation(
            testCase,
            second,
            1,
            Surface(["feature:core"], ["contribution:active"], ["contribution:excluded"], [], [], ["contribution:active", "contribution:excluded"]));

        var forward = new NegativeSurfacePreservationOracle().Evaluate(new PlanFuzzOracleContext(
            testCase,
            new PlanFuzzOracleContract("negative", PlanFuzzOracleIds.NegativeSurfacePreservation, 2, [first.VariantId, second.VariantId]),
            new Dictionary<string, PlanFuzzObservation>(StringComparer.Ordinal)
            {
                [first.VariantId] = incomplete,
                [second.VariantId] = violating
            }));
        var reverse = new NegativeSurfacePreservationOracle().Evaluate(new PlanFuzzOracleContext(
            testCase,
            new PlanFuzzOracleContract("negative", PlanFuzzOracleIds.NegativeSurfacePreservation, 2, [second.VariantId, first.VariantId]),
            new Dictionary<string, PlanFuzzObservation>(StringComparer.Ordinal)
            {
                [first.VariantId] = incomplete,
                [second.VariantId] = violating
            }));

        Assert.Multiple(() =>
        {
            Assert.That(forward.Status, Is.EqualTo(PlanFuzzOracleStatus.Violated));
            Assert.That(reverse.Status, Is.EqualTo(PlanFuzzOracleStatus.Violated));
            Assert.That(reverse.FingerprintMaterial, Is.EqualTo(forward.FingerprintMaterial));
        });
    }

    [Test]
    public void NegativeSurfacePassesOnlyCurrentCompleteOwnerEvidence()
    {
        var (testCase, variant) = CreateSingleVariantCase();
        var observation = Observation(
            testCase,
            variant,
            1,
            Surface(["feature:core"], ["contribution:active"], ["contribution:excluded"], [], [], ["contribution:active"]));

        var result = new PlanFuzzOracleEngine().Evaluate(testCase, [observation]).Single();

        Assert.That(result.Status, Is.EqualTo(PlanFuzzOracleStatus.Passed));
    }

    [Test]
    public void ExtensionNoninterferenceDeterminesDirectionStructurallyAndIgnoresContractOrder()
    {
        var baselineVariant = Variant("baseline");
        var extendedVariant = Variant("extended", PlanFuzzVariantRole.EquivalentMutation);
        var testCase = CreateCase([baselineVariant, extendedVariant], []);
        var baseline = Observation(
            testCase,
            baselineVariant,
            1,
            Surface(["feature:core"], ["contribution:active"], ["contribution:extension"], [], [], ["contribution:active"]));
        var extended = Observation(
            testCase,
            extendedVariant,
            1,
            Surface(
                ["feature:core", "feature:extension"],
                ["contribution:active", "contribution:extension"],
                [],
                ["feature:extension"],
                ["contribution:extension"],
                ["contribution:active"]));
        var observations = new Dictionary<string, PlanFuzzObservation>(StringComparer.Ordinal)
        {
            [baselineVariant.VariantId] = baseline,
            [extendedVariant.VariantId] = extended
        };

        var forward = new ExtensionNoninterferenceOracle().Evaluate(new PlanFuzzOracleContext(
            testCase,
            new PlanFuzzOracleContract("extension", PlanFuzzOracleIds.ExtensionNoninterference, 2, [baselineVariant.VariantId, extendedVariant.VariantId]),
            observations));
        var reverse = new ExtensionNoninterferenceOracle().Evaluate(new PlanFuzzOracleContext(
            testCase,
            new PlanFuzzOracleContract("extension", PlanFuzzOracleIds.ExtensionNoninterference, 2, [extendedVariant.VariantId, baselineVariant.VariantId]),
            observations));

        Assert.Multiple(() =>
        {
            Assert.That(forward.Status, Is.EqualTo(PlanFuzzOracleStatus.Passed));
            Assert.That(reverse.Status, Is.EqualTo(PlanFuzzOracleStatus.Passed));
            Assert.That(reverse.FingerprintMaterial, Is.EqualTo(forward.FingerprintMaterial));
        });
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void ExtensionNoninterferenceRejectsAdditionsMissingOneEvidenceDomain(
        bool addSurface,
        bool addOwner)
    {
        var baselineVariant = Variant("baseline");
        var extendedVariant = Variant("extended", PlanFuzzVariantRole.EquivalentMutation);
        var contract = new PlanFuzzOracleContract(
            "extension",
            PlanFuzzOracleIds.ExtensionNoninterference,
            2,
            [baselineVariant.VariantId, extendedVariant.VariantId]);
        var testCase = CreateCase([baselineVariant, extendedVariant], [contract]);
        var baseline = Observation(
            testCase,
            baselineVariant,
            1,
            Surface(["feature:core"], ["contribution:active"], [], [], [], ["contribution:active"]));
        string[] extendedSurfaces = addSurface
            ? ["feature:core", "feature:extension"]
            : ["feature:core"];
        string[] extendedOwners = addOwner
            ? ["contribution:active", "contribution:extension"]
            : ["contribution:active"];
        string[] independentSurfaces = addSurface ? ["feature:extension"] : [];
        string[] independentOwners = addOwner ? ["contribution:extension"] : [];
        var extended = Observation(
            testCase,
            extendedVariant,
            1,
            Surface(
                extendedSurfaces,
                extendedOwners,
                [],
                independentSurfaces,
                independentOwners,
                ["contribution:active"]));

        var result = new PlanFuzzOracleEngine().Evaluate(testCase, [baseline, extended]).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(PlanFuzzOracleStatus.InfrastructureFailure));
            Assert.That(result.FingerprintMaterial, Is.EqualTo("invalid-or-ambiguous-delta"));
        });
    }

    [Test]
    public void ExtensionNoninterferenceRejectsRouteChangeEvenWhenValuesMatch()
    {
        var baselineVariant = Variant("baseline");
        var extendedVariant = Variant("extended", PlanFuzzVariantRole.EquivalentMutation);
        var contract = new PlanFuzzOracleContract(
            "extension",
            PlanFuzzOracleIds.ExtensionNoninterference,
            2,
            [baselineVariant.VariantId, extendedVariant.VariantId]);
        var testCase = CreateCase([baselineVariant, extendedVariant], [contract]);
        var baseline = Observation(
            testCase,
            baselineVariant,
            1,
            Surface(["feature:core"], ["contribution:active"], ["contribution:extension"], [], [], ["contribution:active"]));
        var extended = Observation(
            testCase,
            extendedVariant,
            1,
            Surface(
                ["feature:core", "feature:extension"],
                ["contribution:active", "contribution:extension"],
                [],
                ["feature:extension"],
                ["contribution:extension"],
                ["contribution:active"],
                routeIdentity: "route:changed"));

        var result = new PlanFuzzOracleEngine().Evaluate(testCase, [baseline, extended]).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(PlanFuzzOracleStatus.Violated));
            Assert.That(result.EffectiveClassFingerprintMaterial, Is.EqualTo("extension-route-changed"));
        });
    }

    private static (PlanFuzzTestCase TestCase, PlanFuzzPlanVariant Variant) CreateSingleVariantCase()
    {
        var variant = Variant("baseline");
        var contract = new PlanFuzzOracleContract("surface", PlanFuzzOracleIds.NegativeSurfacePreservation, 2, [variant.VariantId]);
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
        IEnumerable<string> selectedSurfaces,
        IEnumerable<string> selectedOwners,
        IEnumerable<string> excludedOwners,
        IEnumerable<string> independentSurfaces,
        IEnumerable<string> independentOwners,
        IEnumerable<string> activatedOwners,
        PlanFuzzActivationTraceStatus traceStatus = PlanFuzzActivationTraceStatus.Complete,
        string routeIdentity = "route:baseline") =>
        new(
            PlanFuzzSurfaceSnapshot.CurrentEvidenceContractVersion,
            selectedSurfaces,
            selectedOwners,
            excludedOwners,
            independentSurfaces,
            independentOwners,
            activatedOwners,
            traceStatus,
            "test-trace-v2",
            routeIdentity);
}
