using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Presets;
using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.EndToEndExperiments;

internal sealed record EndToEndCase(
    string Id,
    string Stratum,
    string Source,
    IReadOnlyDictionary<string, double> Arguments,
    double ExpectedResult,
    string Backend,
    bool InjectFault,
    string PresetId,
    string MutationId,
    string ExpectedWithoutProtocol,
    string ExpectedDetectionBoundary,
    string ExpectedDiagnosticCode);

internal sealed record EndToEndRecord(
    int SchemaVersion,
    string RunId,
    string CommitSha,
    string CaseId,
    string Stratum,
    string Source,
    IReadOnlyDictionary<string, double> Arguments,
    string PresetId,
    string Backend,
    string Policy,
    int Repetition,
    int Seed,
    bool FaultInjected,
    string MutationId,
    double ExpectedResult,
    double? ActualResult,
    string Classification,
    string FirstDetectionBoundary,
    IReadOnlyList<string> DiagnosticCodes,
    IReadOnlyList<string> DiagnosticStages,
    IReadOnlyList<string> Trace,
    long WholeCompilationElapsedNs,
    int ProcessExitCode,
    string? InfrastructureError);

internal static class Program
{
    private const int SchemaVersion = 1;
    private const int Repetitions = 2;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "--child", StringComparison.Ordinal))
            return RunChild(args);

        var outputDirectory = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.GetFullPath("artifacts/cgo27-end-to-end");
        return await RunParent(outputDirectory);
    }

    private static async Task<int> RunParent(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var cases = BuildCases();
        ValidateCaseCatalog(cases);
        var runId = $"cgo27-e2e-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}-{Environment.ProcessId}";
        var commit = Environment.GetEnvironmentVariable("GITHUB_SHA")
                     ?? Environment.GetEnvironmentVariable("CGO27_EXPERIMENT_COMMIT")
                     ?? "local-uncommitted";
        var records = new List<EndToEndRecord>();

        foreach (var @case in cases.OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            foreach (var policy in PolicyNames())
            {
                for (var repetition = 1; repetition <= Repetitions; repetition++)
                {
                    var seed = StableSeed(@case.Id, policy, repetition);
                    records.Add(await RunFreshProcess(@case, policy, repetition, seed, runId, commit));
                }
            }
        }

        ValidateRecords(cases, records);
        var rawPath = Path.Combine(outputDirectory, "raw-results.jsonl");
        await using (var writer = new StreamWriter(rawPath, false))
        {
            foreach (var record in records
                         .OrderBy(static item => item.CaseId, StringComparer.Ordinal)
                         .ThenBy(static item => item.Policy, StringComparer.Ordinal)
                         .ThenBy(static item => item.Repetition))
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(record, JsonOptions));
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "cases.json"),
            JsonSerializer.Serialize(cases, new JsonSerializerOptions(JsonOptions) { WriteIndented = true }));
        var summary = BuildSummary(cases, records, runId, commit);
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "summary.json"),
            JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonOptions) { WriteIndented = true }));
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "environment.json"),
            JsonSerializer.Serialize(new
            {
                schemaVersion = SchemaVersion,
                runId,
                commitSha = commit,
                framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                processorCount = Environment.ProcessorCount,
                repetitions = Repetitions
            }, new JsonSerializerOptions(JsonOptions) { WriteIndented = true }));

        Console.WriteLine("CGO27_END_TO_END_SUMMARY=" + JsonSerializer.Serialize(summary, JsonOptions));
        return 0;
    }

    private static int RunChild(string[] args)
    {
        if (args.Length != 6)
            return 64;

        var cases = BuildCases().ToDictionary(static item => item.Id, StringComparer.Ordinal);
        if (!cases.TryGetValue(args[1], out var @case))
            return 65;
        var policyName = args[2];
        if (!int.TryParse(args[3], NumberStyles.None, CultureInfo.InvariantCulture, out var repetition) ||
            !int.TryParse(args[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed))
        {
            return 66;
        }
        var runId = args[5];
        var commit = Environment.GetEnvironmentVariable("GITHUB_SHA")
                     ?? Environment.GetEnvironmentVariable("CGO27_EXPERIMENT_COMMIT")
                     ?? "local-uncommitted";
        var record = ExecuteCase(@case, policyName, repetition, seed, runId, commit);
        Console.WriteLine(JsonSerializer.Serialize(record, JsonOptions));
        return 0;
    }

    private static EndToEndRecord ExecuteCase(
        EndToEndCase @case,
        string policyName,
        int repetition,
        int seed,
        string runId,
        string commit)
    {
        var trace = new List<string> { "child-start", "source-selected" };
        var sink = new InMemoryModuleContractDiagnosticSink();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Environment.SetEnvironmentVariable(
                "CGO27_E2E_FAULT",
                @case.InjectFault ? "replace-result" : null);
            var services = new ServiceCollection();
            services.AddWistDialectServices();
            using var compositionProvider = services.BuildServiceProvider();
            var workflow = compositionProvider.GetRequiredService<WistDialectExecutionWorkflow>();
            var preset = WistShippedDialectPresets.GetRequired(@case.PresetId);
            var sourcePath = new WistShippedDialectFileResolver().Resolve(preset);
            var dialectSource = File.ReadAllText(sourcePath);
            var composition = @case.InjectFault
                ? workflow.ComposeText(
                    dialectSource,
                    Path.GetFileName(sourcePath),
                    RuntimeProfileDefinitionBuilder
                        .Create("cgo27-end-to-end-fault")
                        .Describe("Adds the model-authored CGO27 result-integrity mutation optimizer.")
                        .EnableOptimizer("Cgo27Fault")
                        .Build(),
                    RuntimeProfileOverridePolicy.StrictNoConflicts)
                : workflow.ComposeText(dialectSource, Path.GetFileName(sourcePath));
            trace.Add(composition.IsSuccess ? "composition-success" : "composition-failure");
            if (!composition.IsSuccess)
            {
                throw new InvalidOperationException(
                    "Wist dialect composition failed: " +
                    string.Join(" | ", composition.Diagnostics.Select(static item => item.Message)));
            }

            var verificationOptions = new ModuleContractVerificationOptions
            {
                Mode = ModuleContractVerificationMode.Strict,
                PipelineOptions = ModuleContractPipelineProfiles.StrictEnforced with
                {
                    VerificationPolicy = ParsePolicy(policyName)
                },
                DiagnosticSink = sink
            }.SnapshotValidated();
            using var host = workflow.CreateHost(
                composition,
                new WistRuntimeServiceOptions { ModuleContracts = verificationOptions });
            trace.Add("runtime-host-created");
            trace.Add("source-execution-start");
            var arguments = @case.Arguments.ToDictionary(
                static item => item.Key,
                static item => (object?)item.Value,
                StringComparer.Ordinal);
            var result = arguments.Count == 0
                ? host.Run(@case.Source, @case.Backend)
                : host.Run(@case.Source, arguments, @case.Backend);
            var numericResult = Convert.ToDouble(result, CultureInfo.InvariantCulture);
            stopwatch.Stop();
            trace.Add("source-result-produced");
            AppendDiagnosticTrace(trace, sink);
            return CreateRecord(
                @case,
                policyName,
                repetition,
                seed,
                runId,
                commit,
                numericResult,
                Math.Abs(numericResult - @case.ExpectedResult) <= 1e-9 ? "accepted" : "wrong-result",
                "result",
                sink.Batches.SelectMany(static batch => batch.Diagnostics).Select(static diagnostic => diagnostic.Code).Distinct().Order().ToArray(),
                sink.Batches.Select(static batch => batch.Stage).Distinct().ToArray(),
                trace,
                stopwatch.ElapsedTicks,
                null);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            var contractException = FindContractException(exception);
            var classification = contractException == null ? "late-failure" : "rejected";
            var boundary = contractException?.Stage ?? "runtime-or-backend";
            var codes = contractException?.Diagnostics.Select(static diagnostic => diagnostic.Code)
                            .Distinct()
                            .Order(StringComparer.Ordinal)
                            .ToArray()
                        ?? [exception.GetType().FullName ?? exception.GetType().Name];
            trace.Add(contractException == null ? "runtime-failure" : "contract-rejection");
            AppendDiagnosticTrace(trace, sink);
            return CreateRecord(
                @case,
                policyName,
                repetition,
                seed,
                runId,
                commit,
                null,
                classification,
                boundary,
                codes,
                sink.Batches.Select(static batch => batch.Stage).Append(boundary).Distinct().ToArray(),
                trace,
                stopwatch.ElapsedTicks,
                contractException == null ? exception.ToString() : null);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CGO27_E2E_FAULT", null);
        }
    }

    private static EndToEndRecord CreateRecord(
        EndToEndCase @case,
        string policy,
        int repetition,
        int seed,
        string runId,
        string commit,
        double? actualResult,
        string classification,
        string boundary,
        IReadOnlyList<string> codes,
        IReadOnlyList<string> stages,
        IReadOnlyList<string> trace,
        long elapsedTicks,
        string? infrastructureError) =>
        new(
            SchemaVersion,
            runId,
            commit,
            @case.Id,
            @case.Stratum,
            @case.Source,
            @case.Arguments,
            @case.PresetId,
            @case.Backend,
            policy,
            repetition,
            seed,
            @case.InjectFault,
            @case.MutationId,
            @case.ExpectedResult,
            actualResult,
            classification,
            boundary,
            codes,
            stages,
            trace,
            TicksToNanoseconds(elapsedTicks),
            0,
            infrastructureError);

    private static async Task<EndToEndRecord> RunFreshProcess(
        EndToEndCase @case,
        string policy,
        int repetition,
        int seed,
        string runId,
        string commit)
    {
        var processPath = Environment.ProcessPath
                          ?? throw new InvalidOperationException("Current process path is unavailable.");
        var startInfo = new ProcessStartInfo
        {
            FileName = processPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
            startInfo.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
        startInfo.ArgumentList.Add("--child");
        startInfo.ArgumentList.Add(@case.Id);
        startInfo.ArgumentList.Add(policy);
        startInfo.ArgumentList.Add(repetition.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add(seed.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add(runId);
        startInfo.Environment["CGO27_EXPERIMENT_COMMIT"] = commit;
        startInfo.Environment["CGO27_E2E_FAULT"] = @case.InjectFault ? "replace-result" : string.Empty;

        using var process = Process.Start(startInfo)
                            ?? throw new InvalidOperationException("Failed to start end-to-end child process.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"Child process timed out for {@case.Id}/{policy}/r{repetition}.");
        }

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Child process failed for {@case.Id}/{policy}/r{repetition} with exit {process.ExitCode}: {stderr}");
        }

        var jsonLine = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault()
                       ?? throw new InvalidOperationException("Child process emitted no JSON record.");
        var record = JsonSerializer.Deserialize<EndToEndRecord>(jsonLine, JsonOptions)
                     ?? throw new InvalidOperationException("Child process emitted an empty JSON record.");
        return record;
    }

    private static void ValidateCaseCatalog(IReadOnlyList<EndToEndCase> cases)
    {
        if (cases.Count != 30)
            throw new InvalidOperationException($"Expected 30 cases, found {cases.Count}.");
        if (cases.Select(static item => item.Id).Distinct(StringComparer.Ordinal).Count() != 30)
            throw new InvalidOperationException("Case ids must be unique.");
        var strata = cases.GroupBy(static item => item.Stratum).ToDictionary(static group => group.Key, static group => group.Count());
        if (strata.Count != 3 || strata.Values.Any(static count => count != 10))
            throw new InvalidOperationException("Expected exactly three strata with ten cases each.");
        if (cases.Count(static item => item.InjectFault) < 5)
            throw new InvalidOperationException("Expected at least five fault-bearing end-to-end cases.");
        if (cases.Where(static item => item.InjectFault).Any(static item => Math.Abs(item.ExpectedResult - 1d) <= 1e-9))
            throw new InvalidOperationException("Fault-bearing cases must not have the injected result as their oracle.");
    }

    private static void ValidateRecords(IReadOnlyList<EndToEndCase> cases, IReadOnlyList<EndToEndRecord> records)
    {
        var expectedCount = cases.Count * PolicyNames().Count * Repetitions;
        if (records.Count != expectedCount)
            throw new InvalidOperationException($"Expected {expectedCount} raw records, found {records.Count}.");

        foreach (var @case in cases)
        {
            foreach (var policy in PolicyNames())
            {
                var group = records.Where(item => item.CaseId == @case.Id && item.Policy == policy).ToArray();
                if (group.Length != Repetitions)
                    throw new InvalidOperationException($"Missing repetitions for {@case.Id}/{policy}.");
                var signatures = group.Select(static item => JsonSerializer.Serialize(new
                    {
                        item.Classification,
                        item.FirstDetectionBoundary,
                        item.DiagnosticCodes,
                        item.ActualResult,
                        item.InfrastructureError
                    }, JsonOptions)).Distinct(StringComparer.Ordinal).ToArray();
                if (signatures.Length != 1)
                    throw new InvalidOperationException($"Fresh-process replay is unstable for {@case.Id}/{policy}.");

                var first = group[0];
                if (!@case.InjectFault)
                {
                    if (first.Classification != "accepted" || first.ActualResult is null ||
                        Math.Abs(first.ActualResult.Value - @case.ExpectedResult) > 1e-9)
                    {
                        throw new InvalidOperationException($"Valid case {@case.Id}/{policy} did not preserve its result.");
                    }
                    continue;
                }

                if (policy is "P0_STRUCTURAL" or "P1_INVALIDATION")
                {
                    if (first.Classification != "wrong-result" || first.ActualResult is null ||
                        Math.Abs(first.ActualResult.Value - 1d) > 1e-9)
                    {
                        throw new InvalidOperationException(
                            $"Fault case {@case.Id}/{policy} did not demonstrate silent wrong-result behavior.");
                    }
                }
                else
                {
                    if (first.Classification != "rejected" ||
                        first.FirstDetectionBoundary != @case.ExpectedDetectionBoundary ||
                        !first.DiagnosticCodes.Contains(@case.ExpectedDiagnosticCode, StringComparer.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Fault case {@case.Id}/{policy} was not rejected at the expected semantic boundary.");
                    }
                }
            }

            var selective = records.First(item => item.CaseId == @case.Id && item.Policy == "P2_SELECTIVE");
            var always = records.First(item => item.CaseId == @case.Id && item.Policy == "P3_ALWAYS");
            if (selective.Classification != always.Classification ||
                selective.FirstDetectionBoundary != always.FirstDetectionBoundary ||
                !selective.DiagnosticCodes.SequenceEqual(always.DiagnosticCodes, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"P2/P3 correctness parity failed for {@case.Id}.");
            }
        }
    }

    private static object BuildSummary(
        IReadOnlyList<EndToEndCase> cases,
        IReadOnlyList<EndToEndRecord> records,
        string runId,
        string commit) =>
        new
        {
            schemaVersion = SchemaVersion,
            runId,
            commitSha = commit,
            cases = cases.Count,
            strata = cases.GroupBy(static item => item.Stratum)
                .OrderBy(static group => group.Key, StringComparer.Ordinal)
                .ToDictionary(static group => group.Key, static group => group.Count()),
            faultCases = cases.Count(static item => item.InjectFault),
            freshProcessRepetitions = Repetitions,
            rawRecords = records.Count,
            policyOutcomes = PolicyNames().ToDictionary(
                static policy => policy,
                policy => records.Where(item => item.Policy == policy)
                    .GroupBy(static item => item.Classification)
                    .ToDictionary(static group => group.Key, static group => group.Count())),
            p2P3ParityCases = cases.Count,
            externallyAuthored = false,
            corpusLabel = "model-authored-exploratory",
            claimBoundary = "This corpus is source-to-result and fresh-process reproducible, but it is not externally authored."
        };

    private static IReadOnlyList<EndToEndCase> BuildCases()
    {
        var cases = new List<EndToEndCase>();
        Add(cases, "C01", "constant", "2 + 3", 5, "cil", true);
        Add(cases, "C02", "constant", "7 - 2", 5, "cil", true);
        Add(cases, "C03", "constant", "3 * 4", 12, "cil");
        Add(cases, "C04", "constant", "8 + 9", 17, "interpreter");
        Add(cases, "C05", "constant", "20 - 4", 16, "cil");
        Add(cases, "C06", "constant", "6 + 6", 12, "interpreter");
        Add(cases, "C07", "constant", "9 * 2", 18, "cil");
        Add(cases, "C08", "constant", "100 - 37", 63, "interpreter");
        Add(cases, "C09", "constant", "(2 + 3) * 4", 20, "cil");
        Add(cases, "C10", "constant", "42", 42, "interpreter");

        Add(cases, "P01", "parameterized", "x + y", 10, "cil", true, ("x", 2), ("y", 8));
        Add(cases, "P02", "parameterized", "x * y", 21, "interpreter", true, ("x", 3), ("y", 7));
        Add(cases, "P03", "parameterized", "x - y", 11, "cil", false, ("x", 15), ("y", 4));
        Add(cases, "P04", "parameterized", "x + y + z", 9, "interpreter", false, ("x", 2), ("y", 3), ("z", 4));
        Add(cases, "P05", "parameterized", "(x + y) * z", 20, "cil", false, ("x", 2), ("y", 3), ("z", 4));
        Add(cases, "P06", "parameterized", "x + x", 12, "interpreter", false, ("x", 6));
        Add(cases, "P07", "parameterized", "x * 2 + y", 13, "cil", false, ("x", 5), ("y", 3));
        Add(cases, "P08", "parameterized", "(x - y) * z", 21, "interpreter", false, ("x", 9), ("y", 2), ("z", 3));
        Add(cases, "P09", "parameterized", "x + y + z", 6, "cil", false, ("x", 1), ("y", 2), ("z", 3));
        Add(cases, "P10", "parameterized", "x * y - z", 14, "interpreter", false, ("x", 4), ("y", 5), ("z", 6));

        Add(cases, "B01", "backend-crosscheck", "11 + 13", 24, "interpreter", true);
        Add(cases, "B02", "backend-crosscheck", "11 + 13", 24, "cil");
        Add(cases, "B03", "backend-crosscheck", "7 * 8", 56, "interpreter");
        Add(cases, "B04", "backend-crosscheck", "7 * 8", 56, "cil");
        Add(cases, "B05", "backend-crosscheck", "50 - 17", 33, "interpreter");
        Add(cases, "B06", "backend-crosscheck", "50 - 17", 33, "cil");
        Add(cases, "B07", "backend-crosscheck", "(4 + 5) * 3", 27, "interpreter");
        Add(cases, "B08", "backend-crosscheck", "(4 + 5) * 3", 27, "cil");
        Add(cases, "B09", "backend-crosscheck", "90 - 12 * 3", 54, "interpreter");
        Add(cases, "B10", "backend-crosscheck", "90 - 12 * 3", 54, "cil");
        return cases;
    }

    private static void Add(
        ICollection<EndToEndCase> target,
        string id,
        string stratum,
        string source,
        double expected,
        string backend,
        bool fault = false,
        params (string Name, double Value)[] arguments)
    {
        target.Add(new EndToEndCase(
            id,
            stratum,
            source,
            arguments.ToDictionary(static item => item.Name, static item => item.Value, StringComparer.Ordinal),
            expected,
            backend,
            fault,
            "pricing-restricted",
            fault ? "cgo27.replace-result-v1" : "none",
            fault ? "wrong-result" : "accepted",
            fault ? "optimized AIR contract verification" : "result",
            fault ? ModuleContractDiagnosticCodes.MissingBackendCapability : string.Empty));
    }

    private static IReadOnlyList<string> PolicyNames() =>
        ["P0_STRUCTURAL", "P1_INVALIDATION", "P2_SELECTIVE", "P3_ALWAYS"];

    private static ModuleContractVerificationPolicy ParsePolicy(string value) => value switch
    {
        "P0_STRUCTURAL" => ModuleContractVerificationPolicy.P0Structural,
        "P1_INVALIDATION" => ModuleContractVerificationPolicy.P1Invalidation,
        "P2_SELECTIVE" => ModuleContractVerificationPolicy.P2Selective,
        "P3_ALWAYS" => ModuleContractVerificationPolicy.P3Always,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown policy.")
    };

    private static ModuleContractVerificationException? FindContractException(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is ModuleContractVerificationException contractException)
                return contractException;
        }
        return null;
    }

    private static void AppendDiagnosticTrace(ICollection<string> trace, InMemoryModuleContractDiagnosticSink sink)
    {
        foreach (var batch in sink.Batches)
            trace.Add("diagnostic:" + batch.Stage);
    }

    private static int StableSeed(string caseId, string policy, int repetition)
    {
        unchecked
        {
            var hash = 17;
            foreach (var character in caseId + "|" + policy)
                hash = hash * 31 + character;
            return hash * 31 + repetition;
        }
    }

    private static long TicksToNanoseconds(long ticks) =>
        checked((long)Math.Round(ticks * (1_000_000_000d / Stopwatch.Frequency), MidpointRounding.AwayFromZero));
}
