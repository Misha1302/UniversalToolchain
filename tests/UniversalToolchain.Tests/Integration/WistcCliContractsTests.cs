using System.Diagnostics;

namespace Tests.Integration;

[TestFixture]
public class WistcCliContractsTests
{
    [OneTimeSetUp]
    public void BuildCli()
    {
        var repoRoot = GetRepoRoot();
        var build = RunProcess("dotnet", "build apps/Wist.Cli/Wist.Cli.csproj -c Release", repoRoot, 180000);
        Assert.That(build.TimedOut, Is.False, $"dotnet build timed out.{Environment.NewLine}{build.StdErr}{Environment.NewLine}{build.StdOut}");
        Assert.That(build.ExitCode, Is.EqualTo(0), build.StdErr + build.StdOut);
        _cliDllPath = ResolveCliDllPath(repoRoot);
        Assert.That(File.Exists(_cliDllPath), Is.True, $"CLI assembly not found at '{_cliDllPath}'.");
    }

    private static string _cliDllPath = string.Empty;

    [Test]
    public void RunEval_ShouldReturnExpectedValue_InCompilerMode()
    {
        var result = RunCli("run --eval --mode compiler \"1 + 2\"");
        Assert.That(result.TimedOut, Is.False, "CLI process timed out.");
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StdOut, Does.Contain("3"));
    }

    [Test]
    public void RunEval_ShouldReturnExpectedValue_InInterpreterMode()
    {
        var result = RunCli("run --eval --mode interpreter \"1 + 2\"");
        Assert.That(result.TimedOut, Is.False, "CLI process timed out.");
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StdOut, Does.Contain("3"));
    }

    [Test]
    public void DialectInspect_ShouldSucceed_ForFullDefaultExample()
    {
        var result = RunCli($"dialect-inspect --file \"{GetDialectPath("full-default")}\"");
        Assert.That(result.TimedOut, Is.False, "CLI process timed out.");
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StdOut, Does.Contain("Success: True"));
    }

    [Test]
    public void InvalidInput_ShouldReturnFailureContract()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"wist-missing-{Guid.NewGuid():N}.wist");
        var result = RunCli($"run --file \"{missingPath}\"");
        Assert.That(result.TimedOut, Is.False, "CLI process timed out.");
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.StdErr, Does.Contain("File was not found").IgnoreCase);
    }

    [Test]
    public void InvalidMode_ShouldReturnFailureContract()
    {
        var result = RunCli("run --eval --mode broken-mode \"1\"");
        Assert.That(result.TimedOut, Is.False, "CLI process timed out.");
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.StdErr, Does.Contain("Unknown execution mode"));
    }

    private static CliResult RunCli(string args) => RunProcess("dotnet", $"\"{_cliDllPath}\" {args}", Path.GetDirectoryName(_cliDllPath)!, 30000);

    private static CliResult RunProcess(string fileName, string arguments, string workingDirectory, int timeoutMs)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory
        };

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

    private static string GetDialectPath(string exampleName) => Path.Combine(GetRepoRoot(), "UniversalToolchain", "samples", "dialects", "wist", exampleName, "dialect.wistdialect");

    private static string GetRepoRoot() => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    private static string ResolveCliDllPath(string repoRoot)
    {
        var binDirectory = Path.Combine(repoRoot, "apps", "Wist.Cli", "bin", "Release");
        var candidates = Directory.EnumerateFiles(binDirectory, "Wist.Cli.dll", SearchOption.AllDirectories).ToArray();

        return candidates
            .OrderByDescending(static x => x.Contains($"{Path.DirectorySeparatorChar}net", StringComparison.Ordinal))
            .ThenByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault() ?? Path.Combine(binDirectory, "net10.0", "Wist.Cli.dll");
    }

    private sealed record CliResult(int ExitCode, string StdOut, string StdErr, bool TimedOut);
}