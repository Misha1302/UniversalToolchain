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
        Assert.That(build.ExitCode, Is.EqualTo(0), build.StdErr + build.StdOut);
        _cliDllPath = Path.Combine(repoRoot, "UniversalToolchain", "Wistc", "bin", "Release", "net10.0", "Wistc.dll");
    }

    [Test]
    public void RunEval_ShouldReturnExpectedValue_InCompilerMode()
    {
        var result = RunCli("run --eval --mode compiler \"1 + 2\"");
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StdOut, Does.Contain("3"));
    }

    [Test]
    public void RunEval_ShouldReturnExpectedValue_InInterpreterMode()
    {
        var result = RunCli("run --eval --mode interpreter \"1 + 2\"");
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StdOut, Does.Contain("3"));
    }

    [Test]
    public void DialectInspect_ShouldSucceed_ForFullDefaultExample()
    {
        var result = RunCli($"dialect-inspect --file \"{GetDialectPath("full-default")}\"");
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StdOut, Does.Contain("Success: True"));
    }

    [Test]
    public void DialectDemo_ShouldReturnDocumentedFailureContract_ForValidScenario()
    {
        var result = RunCli("dialect-demo --scenario valid");
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.StdOut + result.StdErr, Does.Contain("Compilation error").IgnoreCase);
    }

    [Test]
    public void InvalidInput_ShouldReturnFailureContract()
    {
        var result = RunCli("run --file /tmp/does-not-exist.wist");
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.StdErr, Does.Contain("File was not found").IgnoreCase);
    }

    [Test]
    public void InvalidMode_ShouldReturnFailureContract()
    {
        var result = RunCli("run --eval --mode broken-mode \"1\"");
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
            WorkingDirectory = workingDirectory
        };

        using var process = Process.Start(startInfo)!;
        if (!process.WaitForExit(timeoutMs))
        {
            process.Kill(true);
            process.WaitForExit(5000);
        }

        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        return new CliResult(process.ExitCode, stdOut, stdErr);
    }

    private static string GetDialectPath(string exampleName)
    {
        return Path.Combine(GetRepoRoot(), "UniversalToolchain", "Dialects", "examples", "wist", exampleName, "dialect.wistdialect");
    }

    private static string GetRepoRoot()
    {
        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
    }

    private sealed record CliResult(int ExitCode, string StdOut, string StdErr);
}
