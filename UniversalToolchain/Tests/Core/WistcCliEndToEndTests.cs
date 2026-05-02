using Tests.Infrastructure;

namespace Tests.Core;

[TestFixture]
public class WistcCliEndToEndTests
{
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

    private static string _cliDllPath = string.Empty;

    [Test]
    [TestCase("compiler")]
    [TestCase("interpreter")]
    public void RunEval_ShouldSucceed_ForSupportedBackends(string backend)
    {
        var result = RunCli($"run --eval --backend {backend} \"1 + 2\"");

        AssertSuccess(result);
        Assert.That(result.StdOut, Does.Contain("3"));
    }

    [Test]
    public void RunFile_ShouldSucceed()
    {
        using var temp = new TempDirectory();
        var filePath = Path.Combine(temp.Path, "program.wist");
        File.WriteAllText(filePath, "1 + 2");

        var result = RunCli($"run --file \"{filePath}\" --backend interpreter");

        AssertSuccess(result);
        Assert.That(result.StdOut, Does.Contain("3"));
    }

    [Test]
    public void RunDialectFile_ShouldSucceed()
    {
        using var temp = new TempDirectory();
        var dialectPath = WriteMinimalDialect(temp.Path, "minimal.wistdialect");

        var result = RunCli($"run --dialect-file \"{dialectPath}\" --eval --backend interpreter \"1 + 2\"");

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
    public void RunEval_ShouldFail_ForInvalidBackend()
    {
        var result = RunCli("run --eval --backend broken-backend \"1\"");

        AssertFailure(result, "Unknown backend");
    }

    [Test]
    public void RunDialectFile_ShouldFail_ForInvalidPath()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"dialect-missing-{Guid.NewGuid():N}.wistdialect");
        var result = RunCli($"run --dialect-file \"{missingPath}\" --eval --backend interpreter \"1\"");

        AssertFailure(result, "File was not found");
    }

    [Test]
    [TestCase("--use-native-math", "use-native-math")]
    [TestCase("--include-module Numbers", "include-module")]
    [TestCase("--exclude-module Numbers", "exclude-module")]
    public void RunDialectFile_ShouldRejectRemovedRawDialectMutationOptions(string option, string optionName)
    {
        using var temp = new TempDirectory();
        var dialectPath = WriteMinimalDialect(temp.Path, "minimal.wistdialect");

        var result = RunCli($"run --dialect-file \"{dialectPath}\" {option} --eval --backend interpreter \"1\"");

        AssertFailure(result, $"Option '{optionName}' is unknown");
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

    private static CliResult RunCli(string args) => TestContractsInfrastructure.RunProcess("dotnet", $"\"{_cliDllPath}\" {args}", Path.GetDirectoryName(_cliDllPath)!, 30000);

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

    private static string GetRepoRoot() => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
}