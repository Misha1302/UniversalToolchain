using Tests.TestInfrastructure;

namespace Tests.Cli;

[TestFixture]
public class WistcCliContractsTests
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
    public void DialectInspect_ShouldSucceed_ForMinimalValidDialect()
    {
        using var temp = new TempDirectory();
        var dialectPath = Path.Combine(temp.Path, "valid.wistdialect");
        File.WriteAllText(dialectPath, "dialect Valid\nuse Whitespaces,Arithmetic,Numbers\nbackend interpreter");

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
    public void DialectInspect_ShouldFail_ForInvalidDialect()
    {
        using var temp = new TempDirectory();
        var dialectPath = Path.Combine(temp.Path, "invalid.wistdialect");
        File.WriteAllText(dialectPath, "dialect Broken\nuse MissingModule\nbackend interpreter");

        var result = RunCli($"dialect-inspect --file \"{dialectPath}\"");

        Assert.Multiple(() =>
        {
            Assert.That(result.TimedOut, Is.False, "CLI process timed out.");
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.StdOut, Does.Contain("Success: False"));
            Assert.That(result.StdOut, Does.Contain("R001"));
        });
    }

    [Test]
    public void RunCommand_ShouldSucceed_ForDefaultHostPath()
    {
        var result = RunCli("run --eval --mode compiler \"2 + 3\"");

        Assert.Multiple(() =>
        {
            Assert.That(result.TimedOut, Is.False, "CLI process timed out.");
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StdErr, Is.Empty);
        });
    }

    [Test]
    public void RunCommand_ShouldSucceed_ForDialectFilePath()
    {
        using var temp = new TempDirectory();
        var dialectPath = Path.Combine(temp.Path, "minimal.wistdialect");
        File.WriteAllText(dialectPath, "dialect Minimal\nuse Whitespaces,Arithmetic,Numbers\nbackend interpreter");

        var result = RunCli($"run --dialect-file \"{dialectPath}\" --eval --mode interpreter \"2 + 3\"");

        Assert.Multiple(() =>
        {
            Assert.That(result.TimedOut, Is.False, "CLI process timed out.");
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StdErr, Is.Empty);
        });
    }

    [Test]
    public void DialectFilePath_ShouldRejectUseNativeMathOption()
    {
        using var temp = new TempDirectory();
        var dialectPath = Path.Combine(temp.Path, "minimal.wistdialect");
        File.WriteAllText(dialectPath, "dialect Minimal\nuse Whitespaces,Arithmetic,Numbers\nbackend interpreter");

        var result = RunCli($"run --dialect-file \"{dialectPath}\" --use-native-math --eval --mode interpreter \"1\"");

        Assert.Multiple(() =>
        {
            Assert.That(result.TimedOut, Is.False, "CLI process timed out.");
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.StdErr, Does.Contain("cannot be combined with --dialect-file"));
            Assert.That(result.StdErr, Does.Contain("--use-native-math"));
        });
    }

    [Test]
    public void DialectFilePath_ShouldRejectManualIncludeExcludeModuleOptions()
    {
        using var temp = new TempDirectory();
        var dialectPath = Path.Combine(temp.Path, "minimal.wistdialect");
        File.WriteAllText(dialectPath, "dialect Minimal\nuse Whitespaces,Arithmetic,Numbers\nbackend interpreter");

        var includeResult = RunCli($"run --dialect-file \"{dialectPath}\" --include-module Numbers --eval --mode interpreter \"1\"");
        var excludeResult = RunCli($"run --dialect-file \"{dialectPath}\" --exclude-module Numbers --eval --mode interpreter \"1\"");

        Assert.Multiple(() =>
        {
            Assert.That(includeResult.ExitCode, Is.EqualTo(1));
            Assert.That(includeResult.StdErr, Does.Contain("--include-module"));
            Assert.That(includeResult.StdErr, Does.Contain("cannot be combined with --dialect-file"));

            Assert.That(excludeResult.ExitCode, Is.EqualTo(1));
            Assert.That(excludeResult.StdErr, Does.Contain("--exclude-module"));
            Assert.That(excludeResult.StdErr, Does.Contain("cannot be combined with --dialect-file"));
        });
    }

    private static CliResult RunCli(string args) => TestContractsInfrastructure.RunProcess("dotnet", $"\"{_cliDllPath}\" {args}", Path.GetDirectoryName(_cliDllPath)!, 30000);

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