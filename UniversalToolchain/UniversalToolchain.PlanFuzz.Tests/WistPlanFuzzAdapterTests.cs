using UniversalToolchain.PlanFuzz.Adapter.Wist;

namespace UniversalToolchain.PlanFuzz.Tests;

[TestFixture]
public sealed class WistPlanFuzzAdapterTests
{
    [Test]
    public void ConstantInt32ExpressionPassesBackendRouteAndFallbackOracles()
    {
        var adapter = new WistPlanFuzzAdapter();
        var model = new WistIntProgramModel(
            WistIntExpression.Add(WistIntExpression.Constant(2), WistIntExpression.Constant(3)),
            0,
            "test");
        var testCase = adapter.CreateCase(1, 100, 100, model);

        var observations = testCase.Variants.Select(variant => adapter.Execute(testCase, variant)).ToArray();
        var results = new PlanFuzzOracleEngine().Evaluate(testCase, observations);

        Assert.Multiple(() =>
        {
            Assert.That(observations, Has.All.Property(nameof(PlanFuzzObservation.Outcome)).EqualTo(PlanFuzzExecutionOutcome.Success));
            Assert.That(observations.Select(static observation => observation.Value?.CanonicalValue), Has.All.EqualTo("5"));
            Assert.That(results, Has.All.Property(nameof(PlanFuzzOracleResult.Status)).EqualTo(PlanFuzzOracleStatus.Passed));
            Assert.That(observations.Single(static observation => observation.VariantId == "compiler.ssa-prefer").Route?.UsedRoute, Is.True);
            Assert.That(observations.Single(static observation => observation.VariantId == "compiler.ssa-require").Route?.UsedRoute, Is.True);
        });
    }

    [Test]
    public void ExternalInt32ParameterPassesTheDocumentedSsaSubset()
    {
        var adapter = new WistPlanFuzzAdapter();
        var model = new WistIntProgramModel(
            WistIntExpression.Add(WistIntExpression.Parameter(), WistIntExpression.Constant(3)),
            39,
            "test");
        var testCase = adapter.CreateCase(1, 101, 101, model);

        var observations = testCase.Variants.Select(variant => adapter.Execute(testCase, variant)).ToArray();
        var results = new PlanFuzzOracleEngine().Evaluate(testCase, observations);

        Assert.Multiple(() =>
        {
            Assert.That(observations, Has.All.Property(nameof(PlanFuzzObservation.Outcome)).EqualTo(PlanFuzzExecutionOutcome.Success));
            Assert.That(observations.Select(static observation => observation.Value?.CanonicalValue), Has.All.EqualTo("42"));
            Assert.That(results, Has.All.Property(nameof(PlanFuzzOracleResult.Status)).EqualTo(PlanFuzzOracleStatus.Passed));
            Assert.That(observations.Where(static observation => observation.VariantId.StartsWith("compiler.ssa-", StringComparison.Ordinal))
                .Select(static observation => observation.Route?.Diagnostics.Count), Has.All.EqualTo(0));
        });
    }

    [Test]
    public void ObservationRouteRoundtripPreservesEvidence()
    {
        var route = new PlanFuzzRouteSnapshot(
            "route",
            "Prefer",
            usedRoute: false,
            fellBack: true,
            PlanFuzzFallbackKind.ClassifiedUnsupportedShape,
            "profile",
            4,
            4,
            ["fold"],
            [new PlanFuzzRouteDiagnosticSnapshot("unsupported", "emission")]);
        var observation = new PlanFuzzObservation(
            "case",
            "variant",
            "compiler",
            PlanFuzzExecutionOutcome.Success,
            PlanFuzzValueSnapshot.FromInt32(7),
            null,
            null,
            route);

        var roundtripped = PlanFuzzObservationSerializer.Deserialize(PlanFuzzObservationSerializer.Serialize(observation));

        Assert.Multiple(() =>
        {
            Assert.That(roundtripped.Route, Is.Not.Null);
            Assert.That(roundtripped.Route!.FellBack, Is.True);
            Assert.That(roundtripped.Route.FallbackKind, Is.EqualTo(PlanFuzzFallbackKind.ClassifiedUnsupportedShape));
            Assert.That(roundtripped.Route.Diagnostics.Single().Code, Is.EqualTo("unsupported"));
        });
    }

    [Test]
    public void ControlledFallbackRejectsUnclassifiedFallback()
    {
        var adapter = new WistPlanFuzzAdapter();
        var testCase = adapter.CreateCase(
            1,
            102,
            102,
            new WistIntProgramModel(WistIntExpression.Constant(1), 0, "test"));
        var variant = testCase.GetRequiredVariant("compiler.ssa-prefer");
        var observation = new PlanFuzzObservation(
            testCase.CaseId,
            variant.VariantId,
            variant.BackendId,
            PlanFuzzExecutionOutcome.Success,
            PlanFuzzValueSnapshot.FromInt32(1),
            null,
            null,
            new PlanFuzzRouteSnapshot(
                WistPlanFuzzConstants.SsaRouteId,
                "Prefer",
                usedRoute: false,
                fellBack: true,
                PlanFuzzFallbackKind.Unclassified,
                diagnostics: [new PlanFuzzRouteDiagnosticSnapshot("internal.failure", "optimization")]));
        var contract = new PlanFuzzOracleContract(
            "fallback",
            PlanFuzzOracleIds.ControlledFallback,
            1,
            [variant.VariantId]);
        var context = new PlanFuzzOracleContext(
            testCase,
            contract,
            new Dictionary<string, PlanFuzzObservation>(StringComparer.Ordinal)
            {
                [variant.VariantId] = observation
            });

        var result = new ControlledFallbackOracle().Evaluate(context);

        Assert.That(result.Status, Is.EqualTo(PlanFuzzOracleStatus.Violated));
    }
}
