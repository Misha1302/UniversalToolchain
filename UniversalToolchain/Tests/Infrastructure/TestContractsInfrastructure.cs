using System.Diagnostics;

namespace Tests.Infrastructure;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"wist-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, true);
    }
}

internal static class TestContractsInfrastructure
{
    public static CliResult RunProcess(string fileName, string arguments, string workingDirectory, int timeoutMs)
    {
        var startInfo = new ProcessStartInfo(ResolveProcessFileName(fileName), arguments)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory
        };
        startInfo.Environment.TryAdd("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1");
        startInfo.Environment.TryAdd("DOTNET_CLI_HOME", ResolveWritableDotnetHome());

        using var process = Process.Start(startInfo)!;
        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        var timedOut = !process.WaitForExit(timeoutMs);
        if (timedOut)
        {
            process.Kill(true);
            process.WaitForExit(5000);
        }
        else
        {
            process.WaitForExit();
        }

        var streamsCompleted = Task.WaitAll([stdOutTask, stdErrTask], TimeSpan.FromSeconds(10));
        var stdOut = streamsCompleted && stdOutTask.IsCompletedSuccessfully ? stdOutTask.Result : string.Empty;
        var stdErr = streamsCompleted && stdErrTask.IsCompletedSuccessfully ? stdErrTask.Result : string.Empty;

        return new CliResult(process.ExitCode, stdOut, stdErr, timedOut);
    }

    private static string ResolveProcessFileName(string fileName)
    {
        if (!string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase))
            return fileName;

        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath) &&
            string.Equals(Path.GetFileName(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return Environment.ProcessPath;
        }

        return fileName;
    }

    private static string ResolveWritableDotnetHome()
    {
        var existing = Environment.GetEnvironmentVariable("DOTNET_CLI_HOME");
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        var home = Path.Combine(Path.GetTempPath(), "wist-dotnet-home");
        Directory.CreateDirectory(home);
        return home;
    }
}

internal sealed record CliResult(int ExitCode, string StdOut, string StdErr, bool TimedOut);
