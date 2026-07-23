namespace UniversalToolchain.PlanFuzz.Cli;

/// <summary>
/// Hosts the first deterministic PlanFuzz command surface without dynamic adapter discovery.
/// </summary>
public static class PlanFuzzCommandHost
{
    public static async Task<int> RunAsync(string[] arguments, CancellationToken cancellationToken = default)
    {
        arguments = arguments.ArgNotNull();
        try
        {
            var commandLine = PlanFuzzCommandLine.Parse(arguments);
            if (commandLine.Positionals.Count == 0 || commandLine.HasOption("--help"))
            {
                WriteUsage();
                return PlanFuzzExitCodes.Success;
            }

            return commandLine.Positionals[0] switch
            {
                "list-adapters" => ListAdapters(),
                "generate" => Generate(commandLine),
                "inspect" => Inspect(commandLine),
                "replay" => await ReplayAsync(commandLine, cancellationToken).ConfigureAwait(false),
                "campaign" => await CampaignAsync(commandLine, cancellationToken).ConfigureAwait(false),
                "worker" => await WorkerAsync(commandLine).ConfigureAwait(false),
                _ => UnknownCommand(commandLine.Positionals[0])
            };
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return PlanFuzzExitCodes.InvalidCase;
        }
        catch (NotSupportedException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return PlanFuzzExitCodes.InvalidCase;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("PlanFuzz operation was cancelled.");
            return PlanFuzzExitCodes.InfrastructureFailure;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"PlanFuzz unhandled failure: {exception.Message}");
            return PlanFuzzExitCodes.UnhandledFailure;
        }
    }

    private static int ListAdapters()
    {
        foreach (var descriptor in CreateRegistry().Descriptors)
        {
            Console.WriteLine($"{descriptor.AdapterId}\t{descriptor.AdapterVersion}\t{descriptor.LanguageId}");
        }
        return PlanFuzzExitCodes.Success;
    }

    private static int Generate(PlanFuzzCommandLine commandLine)
    {
        var adapter = ResolveAdapter(commandLine.GetOptional("--adapter") ?? AcmePlanFuzzConstants.AdapterId);
        var seed = commandLine.GetUInt64("--seed", 1);
        var index = commandLine.GetInt64("--index", 0, 0);
        var output = Path.GetFullPath(commandLine.GetRequired("--out"));
        var testCase = adapter.GenerateCase(
            seed,
            index,
            new PlanFuzzCaseGenerationOptions(commandLine.GetOptional("--fault")));
        PlanFuzzAtomicFile.WriteAllText(output, PlanFuzzTestCaseSerializer.Serialize(testCase));
        Console.WriteLine(testCase.CaseId);
        return PlanFuzzExitCodes.Success;
    }

    private static int Inspect(PlanFuzzCommandLine commandLine)
    {
        var testCase = PlanFuzzTestCaseSerializer.Deserialize(File.ReadAllText(commandLine.GetRequired("--case")));
        Console.WriteLine($"caseId: {testCase.CaseId}");
        Console.WriteLine($"adapter: {testCase.AdapterId}@{testCase.AdapterVersion}");
        Console.WriteLine($"programClass: {testCase.Program.ProgramClass}");
        Console.WriteLine($"source: {testCase.Program.SourceText}");
        Console.WriteLine("variants:");
        foreach (var variant in testCase.Variants)
            Console.WriteLine($"  {variant.VariantId}: {variant.BackendId}, {variant.ConfigurationId}, {variant.Role}");
        Console.WriteLine("oracles:");
        foreach (var oracle in testCase.OracleContracts)
            Console.WriteLine($"  {oracle.ContractId}: {oracle.OracleId}@{oracle.OracleVersion}");
        return PlanFuzzExitCodes.Success;
    }

    private static async Task<int> ReplayAsync(PlanFuzzCommandLine commandLine, CancellationToken cancellationToken)
    {
        var testcasePath = commandLine.GetRequired("--case");
        var output = commandLine.GetOptional("--output") ?? Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(testcasePath)).NotNull(),
            "replay");
        var repeat = commandLine.GetInt32("--repeat", 3, 1);
        var timeoutSeconds = commandLine.GetInt32("--timeout-seconds", 30, 1);
        var report = await new PlanFuzzReplayCoordinator(TimeSpan.FromSeconds(timeoutSeconds))
            .ReplayAsync(testcasePath, output, repeat, cancellationToken)
            .ConfigureAwait(false);
        Console.WriteLine($"caseId: {report.CaseId}");
        Console.WriteLine($"confirmedViolation: {report.IsConfirmedViolation}");
        Console.WriteLine($"clean: {report.IsClean}");
        Console.WriteLine($"flaky: {report.IsFlaky}");
        Console.WriteLine($"infrastructureFailure: {report.IsInfrastructureFailure}");
        if (report.ConfirmedFingerprint != null)
            Console.WriteLine($"fingerprint: {report.ConfirmedFingerprint}");
        if (report.IsInfrastructureFailure)
            return PlanFuzzExitCodes.InfrastructureFailure;
        return report.IsConfirmedViolation ? PlanFuzzExitCodes.Finding : PlanFuzzExitCodes.Success;
    }

    private static async Task<int> CampaignAsync(PlanFuzzCommandLine commandLine, CancellationToken cancellationToken)
    {
        var adapter = ResolveAdapter(commandLine.GetOptional("--adapter") ?? AcmePlanFuzzConstants.AdapterId);
        var seed = commandLine.GetUInt64("--seed", 1);
        var count = commandLine.GetInt32("--cases", 1, 1);
        var confirmationCount = commandLine.GetInt32("--repeat", 1, 1);
        var timeoutSeconds = commandLine.GetInt32("--timeout-seconds", 30, 1);
        var output = commandLine.GetRequired("--output");
        var summary = await new PlanFuzzCampaignRunner(adapter, TimeSpan.FromSeconds(timeoutSeconds))
            .RunAsync(
                seed,
                count,
                confirmationCount,
                output,
                commandLine.GetOptional("--fault"),
                cancellationToken)
            .ConfigureAwait(false);
        if (summary.InfrastructureFailures > 0)
            return PlanFuzzExitCodes.InfrastructureFailure;
        return summary.ConfirmedFindings > 0 ? PlanFuzzExitCodes.Finding : PlanFuzzExitCodes.Success;
    }

    private static Task<int> WorkerAsync(PlanFuzzCommandLine commandLine)
    {
        if (commandLine.Positionals.Count != 2 ||
            !StringComparer.Ordinal.Equals(commandLine.Positionals[1], "execute-case"))
        {
            return Task.FromResult(UnknownCommand(string.Join(' ', commandLine.Positionals)));
        }
        var testcasePath = commandLine.GetRequired("--case");
        var observationsPath = commandLine.GetRequired("--observations");
        try
        {
            var testCase = PlanFuzzTestCaseSerializer.Deserialize(File.ReadAllText(testcasePath));
            var adapter = ResolveAdapter(testCase.AdapterId);
            var observations = testCase.Variants
                .Select(variant => adapter.Execute(testCase, variant))
                .ToArray();
            PlanFuzzAtomicFile.WriteAllText(
                observationsPath,
                PlanFuzzObservationSetSerializer.Serialize(testCase.CaseId, observations));
            return Task.FromResult(observations.Any(static observation =>
                    observation.Outcome == PlanFuzzExecutionOutcome.InfrastructureFailure)
                ? PlanFuzzExitCodes.InfrastructureFailure
                : PlanFuzzExitCodes.Success);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return Task.FromResult(PlanFuzzExitCodes.InvalidCase);
        }
    }

    private static PlanFuzzAdapterRegistry CreateRegistry() =>
        new PlanFuzzAdapterRegistry().Add(new AcmePlanFuzzAdapter());

    private static IPlanFuzzLanguageAdapter ResolveAdapter(string adapterId) =>
        CreateRegistry().GetRequired(adapterId);

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown PlanFuzz command '{command}'.");
        WriteUsage();
        return PlanFuzzExitCodes.Usage;
    }

    private static void WriteUsage()
    {
        Console.WriteLine("PlanFuzz research CLI");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  list-adapters");
        Console.WriteLine("  generate --adapter acme --seed 1 --index 0 --out case.json [--fault SF-001-wrong-backend-arithmetic]");
        Console.WriteLine("  inspect --case case.json");
        Console.WriteLine("  replay --case case.json --output artifacts/replay --repeat 3 --timeout-seconds 30");
        Console.WriteLine("  campaign --adapter acme --seed 1 --cases 100 --output artifacts/campaign [--repeat 3]");
        Console.WriteLine("  worker execute-case --case case.json --observations observations.json");
    }
}
