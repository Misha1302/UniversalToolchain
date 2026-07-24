namespace UniversalToolchain.PlanFuzz.Cli;

internal sealed class PlanFuzzWorkerCoordinator
{
    private const int MaximumCapturedCharacters = 65_536;
    private const string TruncationMarker = "\n[planfuzz output truncated]";

    private readonly TimeSpan _timeout;

    public PlanFuzzWorkerCoordinator(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            Thrower.Argument(nameof(timeout), "Worker timeout must be positive.");
        _timeout = timeout;
    }

    public async Task<PlanFuzzWorkerResult> ExecuteAsync(
        string testcasePath,
        PlanFuzzTestCase testCase,
        string observationsPath,
        CancellationToken cancellationToken)
    {
        testcasePath = Path.GetFullPath(testcasePath.ArgNotNull());
        observationsPath = Path.GetFullPath(observationsPath.ArgNotNull());
        var startInfo = CreateStartInfo(testcasePath, observationsPath);
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        if (!process.Start())
        {
            return InfrastructureResult(testCase, "worker-start", "Worker process did not start.", PlanFuzzExitCodes.InfrastructureFailure);
        }

        var stdoutTask = ReadBoundedAsync(process.StandardOutput, cancellationToken);
        var stderrTask = ReadBoundedAsync(process.StandardError, cancellationToken);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_timeout);
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            var stdout = await CompleteCaptureAsync(stdoutTask).ConfigureAwait(false);
            var stderr = await CompleteCaptureAsync(stderrTask).ConfigureAwait(false);
            var observations = testCase.Variants
                .Select(variant => PlanFuzzObservation.Timeout(
                    testCase.CaseId,
                    variant,
                    $"Worker exceeded timeout {_timeout.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture)} seconds."))
                .ToArray();
            return new PlanFuzzWorkerResult(observations, PlanFuzzExitCodes.Timeout, stdout, stderr, TimedOut: true);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            _ = await CompleteCaptureAsync(stdoutTask).ConfigureAwait(false);
            _ = await CompleteCaptureAsync(stderrTask).ConfigureAwait(false);
            throw;
        }

        var standardOutput = await stdoutTask.ConfigureAwait(false);
        var standardError = await stderrTask.ConfigureAwait(false);
        var observationsResult = ReadObservations(testCase, observationsPath, process.ExitCode, standardError);
        return new PlanFuzzWorkerResult(
            observationsResult,
            process.ExitCode,
            standardOutput,
            standardError,
            TimedOut: false);
    }

    private static ProcessStartInfo CreateStartInfo(string testcasePath, string observationsPath)
    {
        var assemblyPath = typeof(PlanFuzzCommandHost).Assembly.Location;
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveDotnetHost(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(assemblyPath);
        startInfo.ArgumentList.Add("worker");
        startInfo.ArgumentList.Add("execute-case");
        startInfo.ArgumentList.Add("--case");
        startInfo.ArgumentList.Add(testcasePath);
        startInfo.ArgumentList.Add("--observations");
        startInfo.ArgumentList.Add(observationsPath);
        return startInfo;
    }

    private static string ResolveDotnetHost()
    {
        var explicitHost = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(explicitHost))
            return explicitHost;
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath) &&
            StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet"))
        {
            return processPath;
        }
        return "dotnet";
    }

    private static IReadOnlyList<PlanFuzzObservation> ReadObservations(
        PlanFuzzTestCase testCase,
        string observationsPath,
        int exitCode,
        string standardError)
    {
        if (!File.Exists(observationsPath))
        {
            return InfrastructureObservations(
                testCase,
                "missing-observations",
                $"Worker exited with code {exitCode.ToString(CultureInfo.InvariantCulture)} without an observation set. {Bound(standardError)}");
        }
        try
        {
            var observations = PlanFuzzObservationSetSerializer.Deserialize(File.ReadAllText(observationsPath));
            var expectedIds = testCase.Variants.Select(static variant => variant.VariantId).Order(StringComparer.Ordinal).ToArray();
            var actualIds = observations.Select(static observation => observation.VariantId).Order(StringComparer.Ordinal).ToArray();
            if (!expectedIds.SequenceEqual(actualIds, StringComparer.Ordinal))
            {
                return InfrastructureObservations(
                    testCase,
                    "observation-set-identity",
                    "Worker observation set does not contain exactly the requested testcase variants.");
            }
            foreach (var observation in observations)
            {
                var variant = testCase.GetRequiredVariant(observation.VariantId);
                if (!StringComparer.Ordinal.Equals(observation.CaseId, testCase.CaseId) ||
                    !StringComparer.Ordinal.Equals(observation.BackendId, variant.BackendId))
                {
                    return InfrastructureObservations(
                        testCase,
                        "observation-identity",
                        "Worker observation identity does not match the requested testcase.");
                }
            }
            if (exitCode is PlanFuzzExitCodes.Success or PlanFuzzExitCodes.InfrastructureFailure)
                return observations;
            return InfrastructureObservations(
                testCase,
                "worker-exit",
                $"Worker returned exit code {exitCode.ToString(CultureInfo.InvariantCulture)}. {Bound(standardError)}");
        }
        catch (Exception exception)
        {
            return InfrastructureObservations(
                testCase,
                "observation-set-parse",
                $"Worker observation set could not be parsed: {exception.Message}");
        }
    }

    private static PlanFuzzWorkerResult InfrastructureResult(
        PlanFuzzTestCase testCase,
        string category,
        string message,
        int exitCode) =>
        new(
            InfrastructureObservations(testCase, category, message),
            exitCode,
            string.Empty,
            string.Empty,
            TimedOut: false);

    private static IReadOnlyList<PlanFuzzObservation> InfrastructureObservations(
        PlanFuzzTestCase testCase,
        string category,
        string message) =>
        testCase.Variants
            .Select(variant => PlanFuzzObservation.InfrastructureFailure(testCase.CaseId, variant, category, message))
            .ToArray();

    private static async Task<string> ReadBoundedAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder(capacity: 4_096);
        var buffer = new char[4_096];
        var truncated = false;
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                break;

            var remaining = MaximumCapturedCharacters - builder.Length;
            if (remaining > 0)
                builder.Append(buffer, 0, Math.Min(remaining, read));
            if (read > remaining)
                truncated = true;
        }

        if (truncated)
            builder.Append(TruncationMarker);
        return builder.ToString();
    }

    private static async Task<string> CompleteCaptureAsync(Task<string> captureTask)
    {
        try
        {
            return await captureTask.ConfigureAwait(false);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception)
        {
            // The timeout result remains authoritative if the operating system already reaped the process.
        }
    }

    private static string Bound(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        return normalized.Length <= 512 ? normalized : normalized[..512];
    }
}
