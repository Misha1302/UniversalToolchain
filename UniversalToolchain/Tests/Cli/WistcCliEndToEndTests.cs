using Tests.TestInfrastructure;

namespace Tests.Cli;

[TestFixture]
public class WistcCliEndToEndTests
{
    private static string _cliDllPath = string.Empty;

    [OneTimeSetUp]
    public void BuildCli()
    {
        var repoRoot = GetRepoRoot();
        var build = TestContractsInfrastructure.RunProcess("dotnet", "build Wistc/Wistc.csproj -c Release", Path.Combine(repoRoot, "UniversalToolchain"), 180000);
        Assert.That(build.TimedOut, Is.False, $"dotnet build timed out.{Environment.NewLine}{build.StdErr}{Environment.NewLine}{build.StdOut}");
        Assert.That(build.ExitCode, Is.EqualTo(0), build.StdErr + build.StdOut);

        _cliDllPath = ResolveCliDllPath(repoRoot);
        Assert.That(File.Exists(_cliDllPath), Is.True, $"CLI assembly not found at '{_cliDllPath}'.");
    }

    [Test]
    [TestCase("compiler")]
    [TestCase("interpreter")]
    public void RunEval_ShouldSucceed_ForSupportedModes(string mode)
    {
        var result = RunCli($"run --eval --mode {mode} \"1 + 2\"");

        AssertSuccess(result);
        Assert.That(result.StdOut, Does.Contain("3"));
    }

    [Test]
    public void RunFile_ShouldSucceed()
    {
        using var temp = new TempDirectory();
        var filePath = Path.Combine(temp.Path, "program.wist");
        File.WriteAllText(filePath, "1 + 2");

        var result = RunCli($"run --file \"{filePath}\" --mode interpreter");

        AssertSuccess(result);
        Assert.That(result.StdOut, Does.Contain("3"));
    }

    [Test]
    public void RunDialectFile_ShouldSucceed()
    {
        using var temp = new TempDirectory();
        var dialectPath = WriteMinimalDialect(temp.Path, "minimal.wistdialect");

        var result = RunCli($"run --dialect-file \"{dialectPath}\" --eval --mode interpreter \"1 + 2\"");

        AssertSuccess(result);
    }

    [Test]
    public void DialectInspect_ShouldSucceed_ForValidDialect()
    {
        using var temp = new TempDirectory();
        var dialectPath = WriteMinimalDialect(temp.Path, "valid.wistdialect");

        var result = RunCli($"dialect-inspect --file \"{dialectPath}\"");

        Assert.Multiple(() =>
        {
            Assert.That(result.TimedOut, Is.False, "CLI process timed out.");
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("Success: True"));
            Assert.That(result.StdErr, Is.Empty);
        });
    }

    [Test]
    public void RunFile_ShouldFail_ForInvalidPath()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"wist-missing-{Guid.NewGuid():N}.wist");
        var result = RunCli($"run --file \"{missingPath}\"");

        AssertFailure(result, "File was not found");
    }

    [Test]
    public void RunEval_ShouldFail_ForInvalidMode()
    {
        var result = RunCli("run --eval --mode broken-mode \"1\"");

        AssertFailure(result, "Unknown execution mode");
    }

    [Test]
    public void RunDialectFile_ShouldFail_ForInvalidPath()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"dialect-missing-{Guid.NewGuid():N}.wistdialect");
        var result = RunCli($"run --dialect-file \"{missingPath}\" --eval --mode interpreter \"1\"");

        AssertFailure(result, "File was not found");
    }

    [Test]
    public void RunDialectFile_ShouldRejectUseNativeMathOption()
    {
        using var temp = new TempDirectory();
        var dialectPath = WriteMinimalDialect(temp.Path, "minimal.wistdialect");

        var result = RunCli($"run --dialect-file \"{dialectPath}\" --use-native-math --eval --mode interpreter \"1\"");

        AssertFailure(result, "cannot be combined with --dialect-file", "--use-native-math");
    }

    [Test]
    [TestCase("--include-module Numbers", "--include-module")]
    [TestCase("--exclude-module Numbers", "--exclude-module")]
    public void RunDialectFile_ShouldRejectManualModuleOverrides(string option, string optionName)
    {
        using var temp = new TempDirectory();
        var dialectPath = WriteMinimalDialect(temp.Path, "minimal.wistdialect");

        var result = RunCli($"run --dialect-file \"{dialectPath}\" {option} --eval --mode interpreter \"1\"");

        AssertFailure(result, "cannot be combined with --dialect-file", optionName);
    }

    private static void AssertSuccess(CliResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.TimedOut, Is.False, "CLI process timed out.");
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StdErr, Is.Empty);
        });
    }

    private static void AssertFailure(CliResult result, string stderrContains, string? stderrContainsAdditional = null)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.TimedOut, Is.False, "CLI process timed out.");
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.StdErr, Does.Contain(stderrContains));
            if (!string.IsNullOrWhiteSpace(stderrContainsAdditional))
                Assert.That(result.StdErr, Does.Contain(stderrContainsAdditional));
        });
    }

    private static CliResult RunCli(string args)
    {
        return TestContractsInfrastructure.RunProcess("dotnet", $"\"{_cliDllPath}\" {args}", Path.GetDirectoryName(_cliDllPath)!, 30000);
    }

    private static string WriteMinimalDialect(string root, string fileName)
    {
        var dialectPath = Path.Combine(root, fileName);
        File.WriteAllText(dialectPath, "dialect Minimal\nuse Whitespaces,Arithmetic,Numbers\nbackend interpreter");
        return dialectPath;
    }

    private static string ResolveCliDllPath(string repoRoot)
    {
        var binDirectory = Path.Combine(repoRoot, "UniversalToolchain", "Wistc", "bin", "Release");
        var candidates = Directory.EnumerateFiles(binDirectory, "Wistc.dll", SearchOption.AllDirectories).ToArray();

        return candidates
            .OrderByDescending(static x => x.Contains($"{Path.DirectorySeparatorChar}net", StringComparison.Ordinal))
            .ThenByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault() ?? Path.Combine(binDirectory, "net10.0", "Wistc.dll");
    }

    private static string GetRepoRoot()
    {
        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
    }
}
