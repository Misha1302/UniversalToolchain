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
            Assert.That(entries.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(50));
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
    public void S00_ArchitectureInventory_DetectsKnownActiveLegacyOwnersAndCallsites()
    {
        using var inventory = ReadMigrationJson("LEGACY_ARCHITECTURE_INVENTORY.json");
        var owners = inventory.RootElement.GetProperty("owners").EnumerateArray().ToArray();
        var root = FindRepositoryRoot();
        var failures = new List<string>();

        foreach (var owner in owners)
        {
            var symbol = owner.GetProperty("symbol").GetString()!;
            var definition = owner.GetProperty("definition").GetString()!;
            var definitionPath = Path.Combine(root, definition.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(definitionPath))
            {
                failures.Add($"Missing production definition for {symbol}: {definition}");
                continue;
            }

            if (!File.ReadAllText(definitionPath).Contains(symbol, StringComparison.Ordinal))
                failures.Add($"Definition file does not contain {symbol}: {definition}");

            if (owner.TryGetProperty("knownProductionCallsites", out var callsites))
            {
                foreach (var callsite in callsites.EnumerateArray())
                {
                    var relativePath = callsite.GetString()!;
                    var callsitePath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(callsitePath))
                    {
                        failures.Add($"Missing known production callsite for {symbol}: {relativePath}");
                        continue;
                    }

                    if (!File.ReadAllText(callsitePath).Contains(symbol, StringComparison.Ordinal))
                        failures.Add($"Known production callsite no longer references {symbol}: {relativePath}");
                }
            }

            if (!owner.TryGetProperty("retiredProductionCallsites", out var retiredCallsites))
                continue;

            foreach (var retiredCallsite in retiredCallsites.EnumerateArray())
            {
                var relativePath = retiredCallsite.GetProperty("path").GetString()!;
                var retiredStage = retiredCallsite.GetProperty("retiredStage").GetString()!;
                var callsitePath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(callsitePath))
                {
                    failures.Add($"Missing retired production callsite path for {symbol} at {retiredStage}: {relativePath}");
                    continue;
                }

                if (File.ReadAllText(callsitePath).Contains(symbol, StringComparison.Ordinal))
                    failures.Add($"Retired production callsite still references {symbol} after {retiredStage}: {relativePath}");
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(owners, Has.Length.EqualTo(7));
            Assert.That(owners.Select(static owner => owner.GetProperty("symbol").GetString()).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(owners.Length));
            Assert.That(failures, Is.Empty,
                "S00 freezes active legacy ownership while stage-specific cutovers are converted to explicit negative guards.");
        });
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
