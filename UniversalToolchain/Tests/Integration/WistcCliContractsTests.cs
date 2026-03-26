using System.Diagnostics;

namespace Tests.Integration;

[TestFixture]
public class WistcCliContractsTests
{
    private static string _cliDllPath = string.Empty;

    [OneTimeSetUp]
    public void BuildCli()
    {
        var repoRoot = GetRepoRoot();
        var build = RunProcess("dotnet", "build Wistc/Wistc.csproj -c Release", Path.Combine(repoRoot, "UniversalToolchain"), 180000);
        Assert.That(build.TimedOut, Is.False, $"dotnet build timed out.{Environment.NewLine}{build.StdErr}{Environment.NewLine}{build.StdOut}");
        Assert.That(build.ExitCode, Is.EqualTo(0), build.StdErr + build.StdOut);
        _cliDllPath = Path.Combine(repoRoot, "UniversalToolchain", "Wistc", "bin", "Release", "net10.0", "Wistc.dll");
        Assert.That(File.Exists(_cliDllPath), Is.True, $"CLI assembly not found at '{_cliDllPath}'.");
    }

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
    public void DialectDemo_ShouldReturnDocumentedFailureContract_ForValidScenario()
    {
        var result = RunCli("dialect-demo --scenario valid");
        Assert.That(result.TimedOut, Is.False, "CLI process timed out.");
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.StdOut + result.StdErr, Does.Contain("Compilation error").IgnoreCase);
    }

    [Test]
    public void InvalidInput_ShouldReturnFailureContract()
    {
        var result = RunCli("run --file /tmp/does-not-exist.wist");
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

    private static CliResult RunCli(string args)
    {
        return RunProcess("dotnet", $"\"{_cliDllPath}\" {args}", Path.GetDirectoryName(_cliDllPath)!, 30000);
    }

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

        Task.WaitAll(stdOutTask, stdErrTask);
        return new CliResult(process.ExitCode, stdOutTask.Result, stdErrTask.Result, timedOut);
    }

    private static string GetDialectPath(string exampleName)
    {
        return Path.Combine(GetRepoRoot(), "UniversalToolchain", "Dialects", "examples", "wist", exampleName, "dialect.wistdialect");
    }

    private static string GetRepoRoot()
    {
        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
    }

    private sealed record CliResult(int ExitCode, string StdOut, string StdErr, bool TimedOut);
}
