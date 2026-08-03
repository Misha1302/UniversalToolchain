using System.Diagnostics;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public sealed class RuntimeSharedAssemblyFreshProcessTests
{
    [Test]
    public void PreloadOrders_ProduceIdenticalPlanAndExecutionReceipts()
    {
        var receipts = new[] { "contract-first", "unrelated-first", "none" }
            .Select(static scenario => RunFreshProcess(scenario))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(receipts.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
            Assert.That(receipts[0], Does.Contain("DIALECT_INSPECT=PASS"));
            Assert.That(receipts[0], Does.Contain("TYPE_IDENTITY=PASS"));
            Assert.That(receipts[0], Does.Contain("INTERPRETER_RESULT=255"));
            Assert.That(receipts[0], Does.Contain("CIL_RESULT=255"));
            Assert.That(receipts[0], Does.Contain("BACKEND_PARITY=PASS"));
            Assert.That(receipts[0], Does.Contain("NEGATIVE_SURFACE=PASS"));
        });
    }

    [Test]
    public void HostileSameNameDefaultPreload_DoesNotBecomeAuthority()
    {
        var configuration = ResolveBuildConfiguration();
        var platform = ResolveBuildPlatform();
        var canonical = ResolveFixtureAssembly("CanonicalRuntimeFixture", "RuntimeHostileFixture.dll", configuration, platform);
        var hostile = ResolveFixtureAssembly("HostileRuntimeFixture", "RuntimeHostileFixture.dll", configuration, platform);
        var receipt = RunFreshProcess("hostile", canonical, hostile);

        Assert.That(receipt, Is.EqualTo("HOSTILE_PRELOAD=PASS" + Environment.NewLine));
    }

    [Test]
    public void UnregisteredDefaultContextDependency_IsRejectedFailClosed()
    {
        var configuration = ResolveBuildConfiguration();
        var platform = ResolveBuildPlatform();
        var runtime = ResolveFixtureAssembly(
            "UnregisteredDependencyRuntimeFixture",
            "RuntimeUnregisteredDependencyFixture.dll",
            configuration,
            platform);
        var receipt = RunFreshProcess("unregistered-default-fallback", runtime);

        Assert.That(receipt, Is.EqualTo("UNREGISTERED_DEFAULT_FALLBACK=PASS" + Environment.NewLine));
    }

    private static string RunFreshProcess(params string[] arguments)
    {
        var configuration = ResolveBuildConfiguration();
        var platform = ResolveBuildPlatform();
        var helperAssembly = ResolveHelperAssembly(configuration, platform);
        var start = new ProcessStartInfo(ResolveDotnetHostPath())
        {
            WorkingDirectory = TestSourcePaths.ToolchainRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(helperAssembly);
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Fresh-process host did not start.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(120_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Fresh-process runtime boundary scenario timed out.");
        }

        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        Assert.That(process.ExitCode, Is.EqualTo(0), stderr + Environment.NewLine + stdout);
        return stdout.Replace("\r\n", "\n", StringComparison.Ordinal);
    }


    private static string ResolveHelperAssembly(string configuration, string? platform)
    {
        var bin = Path.Combine(
            TestSourcePaths.ToolchainRoot,
            "UniversalToolchain.Dialects.Tests",
            "FreshProcess",
            "RuntimeSharedAssemblyFreshProcessHost",
            "bin");
        var candidates = Directory.EnumerateFiles(
                bin,
                "UniversalToolchain.Dialects.FreshProcessHost.dll",
                SearchOption.AllDirectories)
            .Where(path => path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => segment.Equals(configuration, StringComparison.OrdinalIgnoreCase)))
            .Where(path => string.IsNullOrWhiteSpace(platform) ||
                           path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                               .Any(segment => segment.Equals(platform, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();
        return candidates.Length == 1
            ? Path.GetFullPath(candidates[0])
            : throw new InvalidOperationException(
                $"Expected exactly one fresh-process host for {configuration}/{platform ?? "default"}, found {candidates.Length}: {string.Join(", ", candidates)}");
    }

    private static string ResolveFixtureAssembly(
        string projectName,
        string assemblyFileName,
        string configuration,
        string? platform)
    {
        var bin = Path.Combine(
            TestSourcePaths.ToolchainRoot,
            "UniversalToolchain.Dialects.Tests",
            "FreshProcess",
            projectName,
            "bin");
        if (!Directory.Exists(bin))
            throw new DirectoryNotFoundException($"Fixture output directory not found: {bin}");

        var candidates = Directory.EnumerateFiles(bin, assemblyFileName, SearchOption.AllDirectories)
            .Where(path => path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => segment.Equals(configuration, StringComparison.OrdinalIgnoreCase)))
            .Where(path => string.IsNullOrWhiteSpace(platform) ||
                           path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                               .Any(segment => segment.Equals(platform, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();
        return candidates.Length == 1
            ? Path.GetFullPath(candidates[0])
            : throw new InvalidOperationException(
                $"Expected exactly one {projectName} fixture for {configuration}/{platform ?? "default"}, found {candidates.Length}: {string.Join(", ", candidates)}");
    }

    private static string ResolveDotnetHostPath()
    {
        var hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        return string.IsNullOrWhiteSpace(hostPath) ? Environment.ProcessPath ?? "dotnet" : hostPath;
    }

    private static string ResolveBuildConfiguration()
    {
        var segments = TestContext.CurrentContext.TestDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.FirstOrDefault(static segment =>
                   segment.Equals("Debug", StringComparison.OrdinalIgnoreCase) ||
                   segment.Equals("Release", StringComparison.OrdinalIgnoreCase))
               ?? "Debug";
    }

    private static string? ResolveBuildPlatform()
    {
        var segments = TestContext.CurrentContext.TestDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var binIndex = Array.LastIndexOf(segments, "bin");
        if (binIndex < 0 || binIndex + 1 >= segments.Length)
            return null;

        var first = segments[binIndex + 1];
        return first.Equals("Debug", StringComparison.OrdinalIgnoreCase) ||
               first.Equals("Release", StringComparison.OrdinalIgnoreCase)
            ? null
            : first;
    }
}
