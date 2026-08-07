using System.Text.Json;

namespace Tests.Architecture;

[TestFixture]
public sealed class WistNewArchitectureBaselineTests
{
    [Test]
    public void S00_InvariantLedger_CoversEveryContractInvariantExactlyOnce()
    {
        using var document = ReadMigrationJson("INVARIANT_ENFORCEMENT_MATRIX.json");
        var entries = document.RootElement.GetProperty("invariants")
            .EnumerateArray()
            .Select(static entry => entry.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Length.EqualTo(50));
            Assert.That(entries.Distinct(StringComparer.Ordinal), Has.Count.EqualTo(50));
            Assert.That(entries, Is.EqualTo(Enumerable.Range(1, 50).Select(static index => $"INV-{index:000}")));
        });
    }

    [Test]
    public void S00_PublicBehaviorMatrix_CoversRequiredFacadeOperationsAndDimensions()
    {
        using var document = ReadMigrationJson("WIST_PUBLIC_BEHAVIOR_MATRIX.json");
        var root = document.RootElement;
        var operations = root.GetProperty("operations")
            .EnumerateArray()
            .Select(static entry => entry.GetProperty("operation").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        var dimensions = root.GetProperty("requiredDimensions")
            .EnumerateArray()
            .Select(static entry => entry.GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(operations, Is.SupersetOf(new[] { "Create", "Evaluate", "Validate", "Compile", "TryCompile", "Dispose" }));
            Assert.That(dimensions, Is.SupersetOf(new[]
            {
                "success/failure",
                "public exception category",
                "structured diagnostic code/stage",
                "resource-limit preflight ordering",
                "backend selection",
                "argument/declared-type normalization",
                "optimization report",
                "compiled program metadata",
                "post-dispose behavior"
            }));
            Assert.That(root.GetProperty("surfaceGateIsSufficient").GetBoolean(), Is.False);
        });
    }

    [Test]
    public void S00_TransitionalOracles_AreClassifiedAndHaveRemovalLifecycle()
    {
        using var document = ReadMigrationJson("TRANSITIONAL_ORACLES.json");
        var guards = document.RootElement.GetProperty("guards").EnumerateArray().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(guards.Select(static guard => guard.GetProperty("id").GetString()),
                Is.EquivalentTo(new[] { "wist-migration-gate-tests", "wist-parity-matrix" }));
            Assert.That(guards, Has.All.Matches<JsonElement>(static guard =>
                guard.GetProperty("classification").GetString() == "TRANSITIONAL_ORACLE" &&
                guard.GetProperty("rewriteStage").GetString() == "S10" &&
                guard.GetProperty("retireLegacyPositioningStage").GetString() == "S11" &&
                guard.GetProperty("finalGuardStage").GetString() == "S15" &&
                guard.GetProperty("mustNotOwnTargetArchitecture").GetBoolean()));
        });
    }

    [Test]
    public void S00_ArchitectureInventory_DetectsKnownActiveLegacyOwners()
    {
        var root = FindRepositoryRoot();
        var expected = new[]
        {
            "BasicCoreImpl",
            "WistDialectExecutionWorkflow",
            "WistDialectPlanFactory",
            "SelectedRuntimePlan"
        };
        var productionSources = EnumerateProductionSources(root).ToArray();
        var missing = expected
            .Where(symbol => !productionSources.Any(path => File.ReadAllText(path).Contains(symbol, StringComparison.Ordinal)))
            .ToArray();

        Assert.That(missing, Is.Empty,
            "S00 is a baseline inventory: these legacy owners are intentionally expected to exist before their owning deletion stages.");
    }

    [Test]
    public void S00_DifferentialObservationProjection_DetectsArtificialDivergence()
    {
        var baseline = new Observation(
            "arithmetic",
            "cil",
            "Evaluate",
            true,
            "System.Double",
            "42",
            null,
            [new DiagnosticObservation("UTC-WIST-EXAMPLE", "Info", "Execution")]);
        var equal = baseline with { };
        var divergentValue = baseline with { ResultValue = "43" };
        var divergentDiagnostic = baseline with
        {
            Diagnostics = [new DiagnosticObservation("UTC-WIST-DIFFERENT", "Info", "Execution")]
        };

        Assert.Multiple(() =>
        {
            Assert.That(SemanticallyEqual(baseline, equal), Is.True);
            Assert.That(SemanticallyEqual(baseline, divergentValue), Is.False);
            Assert.That(SemanticallyEqual(baseline, divergentDiagnostic), Is.False);
        });
    }

    [Test]
    public void S00_DifferentialObservationContract_UsesDeterministicSeedsAndSemanticProjection()
    {
        using var document = ReadMigrationJson("DIFFERENTIAL_OBSERVATION_CONTRACT.json");
        var policy = document.RootElement.GetProperty("seedPolicy");
        var seeds = policy.GetProperty("deterministicSeeds").EnumerateArray().Select(static item => item.GetInt32()).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(seeds, Is.EqualTo(new[] { 1701, 20260807, 424242 }));
            Assert.That(policy.GetProperty("comparison").GetString(), Does.Contain("never PlanHash equality"));
        });
    }

    private static bool SemanticallyEqual(Observation left, Observation right) =>
        left.ScenarioId == right.ScenarioId &&
        left.Backend == right.Backend &&
        left.Operation == right.Operation &&
        left.Success == right.Success &&
        left.ResultType == right.ResultType &&
        left.ResultValue == right.ResultValue &&
        left.ExceptionCategory == right.ExceptionCategory &&
        left.Diagnostics.SequenceEqual(right.Diagnostics);

    private static JsonDocument ReadMigrationJson(string fileName) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "eng",
            "wist-new-architecture-migration",
            fileName)));

    private static IEnumerable<string> EnumerateProductionSources(string root) =>
        Directory.EnumerateFiles(Path.Combine(root, "UniversalToolchain"), "*.cs", SearchOption.AllDirectories)
            .Where(static path =>
            {
                var normalized = path.Replace(Path.DirectorySeparatorChar, '/');
                return !normalized.Contains("/bin/", StringComparison.Ordinal)
                       && !normalized.Contains("/obj/", StringComparison.Ordinal)
                       && !normalized.Contains("/Tests/", StringComparison.Ordinal)
                       && !normalized.Contains(".Tests/", StringComparison.Ordinal)
                       && !normalized.Contains("/Experiments/", StringComparison.Ordinal);
            });

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UniversalToolchain", "Tests", "Tests.csproj")))
                return directory.FullName;
            directory = directory.Parent;
        }

        Assert.Fail("Repository root was not found from the test directory.");
        return string.Empty;
    }

    private sealed record Observation(
        string ScenarioId,
        string Backend,
        string Operation,
        bool Success,
        string? ResultType,
        string? ResultValue,
        string? ExceptionCategory,
        IReadOnlyList<DiagnosticObservation> Diagnostics);

    private sealed record DiagnosticObservation(string Code, string Severity, string Stage);
}
