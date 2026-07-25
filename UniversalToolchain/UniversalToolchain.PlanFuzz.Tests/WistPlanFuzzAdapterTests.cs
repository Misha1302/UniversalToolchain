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
    public void ConstantLeftMultiplicationWithParameterPreservesBackendParity()
    {
        AssertSuccessfulParity(
            WistIntExpression.Multiply(WistIntExpression.Constant(0), WistIntExpression.Parameter()),
            7,
            "0");
    }

    [Test]
    public void FoldedZeroMultiplicationBeforeSubtractionPreservesInt32()
    {
        AssertSuccessfulParity(
            WistIntExpression.Subtract(
                WistIntExpression.Multiply(WistIntExpression.Constant(0), WistIntExpression.Constant(1)),
                WistIntExpression.Constant(1)),
            0,
            "-1");
    }

    [Test]
    public void NegativeLiteralWithExternalParameterPreservesSsaRoute()
    {
        var adapter = new WistPlanFuzzAdapter();
        var model = new WistIntProgramModel(
            WistIntExpression.Add(WistIntExpression.Parameter(), WistIntExpression.Constant(-2)),
            2,
            "test");
        var testCase = adapter.CreateCase(1, 104, 104, model);

        var observations = testCase.Variants.Select(variant => adapter.Execute(testCase, variant)).ToArray();
        var results = new PlanFuzzOracleEngine().Evaluate(testCase, observations);

        Assert.Multiple(() =>
        {
            Assert.That(observations, Has.All.Property(nameof(PlanFuzzObservation.Outcome)).EqualTo(PlanFuzzExecutionOutcome.Success));
            Assert.That(observations.Select(static observation => observation.Value?.CanonicalValue), Has.All.EqualTo("0"));
            Assert.That(results, Has.All.Property(nameof(PlanFuzzOracleResult.Status)).EqualTo(PlanFuzzOracleStatus.Passed));
            Assert.That(observations.Where(static observation => observation.VariantId.StartsWith("compiler.ssa-", StringComparison.Ordinal))
                .Select(static observation => observation.Route?.UsedRoute), Has.All.True);
            Assert.That(observations.Where(static observation => observation.VariantId.StartsWith("compiler.ssa-", StringComparison.Ordinal))
                .Select(static observation => observation.Route?.Diagnostics.Count), Has.All.EqualTo(0));
        });
    }

    [Test]
    public void DiscoveryGenerationDoesNotInjectKnownRegressionsByDefault()
    {
        var adapter = new WistPlanFuzzAdapter();

        var testCase = adapter.GenerateCase(20260724, 0, new PlanFuzzCaseGenerationOptions());
        var model = WistIntProgramModel.FromPayload(testCase.Program.Model);

        Assert.Multiple(() =>
        {
            Assert.That(model.Origin, Is.EqualTo("generated"));
            Assert.That(model.Origin, Does.Not.StartWith("regression-corpus:"));
        });
    }

    [Test]
    public void RegressionCorpusIsIncludedOnlyByExplicitOptIn()
    {
        var adapter = new WistPlanFuzzAdapter();

        var testCase = adapter.GenerateCase(
            20260724,
            0,
            new PlanFuzzCaseGenerationOptions(includeRegressionCorpus: true));
        var model = WistIntProgramModel.FromPayload(testCase.Program.Model);

        Assert.Multiple(() =>
        {
            Assert.That(model.Origin, Is.EqualTo("regression-corpus:issue-302"));
            Assert.That(testCase.Program.SourceText, Is.EqualTo("(0 * x)"));
        });
    }

    [Test]
    public void LevelZeroRejectsUnexpectedExternalParameterNames()
    {
        Assert.That(
            () => WistIntExpression.Parameter("y"),
            Throws.TypeOf<NotSupportedException>());
    }

    [Test]
    public void InterpreterRejectsAnSsaEnabledConfigurationAsInfrastructureFailure()
    {
        var adapter = new WistPlanFuzzAdapter();
        var canonical = adapter.CreateCase(
            1,
            102,
            102,
            new WistIntProgramModel(WistIntExpression.Constant(1), 0, "test"));
        var invalidVariant = new PlanFuzzPlanVariant(
            "interpreter.invalid",
            WistPlanFuzzConstants.PreferConfiguration,
            WistPlanFuzzConstants.InterpreterBackend,
            PlanFuzzVariantRole.Baseline,
            PlanFuzzExpectedRelation.SameSemantics);
        var invalidCase = new PlanFuzzTestCase(
            canonical.SchemaVersion,
            canonical.AdapterId,
            canonical.AdapterVersion,
            canonical.CampaignSeed,
            canonical.CaseIndex,
            canonical.CaseSeed,
            canonical.PrngAlgorithm,
            canonical.Program,
            [invalidVariant],
            []);

        var observation = adapter.Execute(invalidCase, invalidVariant);

        Assert.Multiple(() =>
        {
            Assert.That(observation.Outcome, Is.EqualTo(PlanFuzzExecutionOutcome.InfrastructureFailure));
            Assert.That(observation.Failure?.Category, Is.EqualTo("variant-configuration"));
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
        var surface = new PlanFuzzSurfaceSnapshot(
            PlanFuzzSurfaceSnapshot.CurrentEvidenceContractVersion,
            ["feature:core", "feature:extension"],
            ["contribution:active", "contribution:extension"],
            ["contribution:excluded"],
            ["feature:extension"],
            ["contribution:extension"],
            [
                new PlanFuzzIndependentExtensionEvidence(
                    "extension:test",
                    ["feature:extension"],
                    ["contribution:extension"])
            ],
            ["contribution:active"],
            PlanFuzzActivationTraceStatus.Complete,
            "test-trace-v3",
            "route:test");
        var observation = new PlanFuzzObservation(
            "case",
            "variant",
            "compiler",
            PlanFuzzExecutionOutcome.Success,
            PlanFuzzValueSnapshot.FromInt32(7),
            null,
            null,
            route,
            surface);

        var roundtripped = PlanFuzzObservationSerializer.Deserialize(PlanFuzzObservationSerializer.Serialize(observation));

        Assert.Multiple(() =>
        {
            Assert.That(roundtripped.Route, Is.Not.Null);
            Assert.That(roundtripped.Route!.FellBack, Is.True);
            Assert.That(roundtripped.Route.FallbackKind, Is.EqualTo(PlanFuzzFallbackKind.ClassifiedUnsupportedShape));
            Assert.That(roundtripped.Route.Diagnostics.Single().Code, Is.EqualTo("unsupported"));
            Assert.That(roundtripped.Surface, Is.Not.Null);
            Assert.That(roundtripped.Surface!.SelectedSurfaceIds, Is.EqualTo(new[] { "feature:core", "feature:extension" }));
            Assert.That(roundtripped.Surface.ExcludedOwnerIds, Is.EqualTo(new[] { "contribution:excluded" }));
            Assert.That(roundtripped.Surface.ActivationTraceStatus, Is.EqualTo(PlanFuzzActivationTraceStatus.Complete));
            Assert.That(roundtripped.Surface.IndependentExtensions.Single().ExtensionId, Is.EqualTo("extension:test"));
            Assert.That(roundtripped.Surface.IndependentExtensions.Single().SurfaceIds, Is.EqualTo(new[] { "feature:extension" }));
            Assert.That(roundtripped.Surface.IndependentExtensions.Single().OwnerIds, Is.EqualTo(new[] { "contribution:extension" }));
            Assert.That(roundtripped.Surface.RouteIdentity, Is.EqualTo("route:test"));
        });
    }

    [Test]
    public void ControlledFallbackRejectsUnclassifiedFallback()
    {
        var adapter = new WistPlanFuzzAdapter();
        var testCase = adapter.CreateCase(
            1,
            103,
            103,
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

    private static void AssertSuccessfulParity(WistIntExpression expression, int parameterValue, string expectedValue)
    {
        var adapter = new WistPlanFuzzAdapter();
        var model = new WistIntProgramModel(expression, parameterValue, "test");
        var testCase = adapter.CreateCase(1, 105, 105, model);

        var observations = testCase.Variants.Select(variant => adapter.Execute(testCase, variant)).ToArray();
        var results = new PlanFuzzOracleEngine().Evaluate(testCase, observations);

        Assert.Multiple(() =>
        {
            Assert.That(observations, Has.All.Property(nameof(PlanFuzzObservation.Outcome)).EqualTo(PlanFuzzExecutionOutcome.Success));
            Assert.That(observations.Select(static observation => observation.Value?.TypeIdentity), Has.All.EqualTo("System.Int32"));
            Assert.That(observations.Select(static observation => observation.Value?.CanonicalValue), Has.All.EqualTo(expectedValue));
            Assert.That(results, Has.All.Property(nameof(PlanFuzzOracleResult.Status)).EqualTo(PlanFuzzOracleStatus.Passed));
        });
    }

    [Test]
    public void StructuredWistReductionIsDeterministicAndUsesOnlySimplerModels()
    {
        var adapter = new WistPlanFuzzAdapter();
        var testCase = adapter.CreateCase(
            1,
            106,
            106,
            new WistIntProgramModel(
                WistIntExpression.Multiply(
                    WistIntExpression.Add(WistIntExpression.Parameter(), WistIntExpression.Constant(8)),
                    WistIntExpression.Subtract(WistIntExpression.Constant(5), WistIntExpression.Constant(2))),
                12,
                "test"));
        var reducer = (IPlanFuzzProgramReducer)adapter;

        var first = reducer.GetProgramReductionCandidates(testCase);
        var second = reducer.GetProgramReductionCandidates(testCase);
        var originalComplexity = reducer.GetProgramComplexity(testCase);

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(static candidate => candidate.CandidateId),
                Is.EqualTo(second.Select(static candidate => candidate.CandidateId)));
            Assert.That(first, Is.Not.Empty);
            Assert.That(first, Has.All.Property(nameof(PlanFuzzProgramReductionCandidate.Complexity)).LessThan(originalComplexity));
            Assert.That(first.Select(static candidate => candidate.Program.SourceText), Does.Contain("(x + 8)"));
            Assert.That(first.Select(static candidate => candidate.Program.SourceText), Does.Contain("(5 - 2)"));
        });
    }
}
