using System.Diagnostics;

namespace UniversalToolchain.Dialects.Tests.RuntimeCatalog;

public sealed class RuntimeManifestEmitterContractTests
{
    [Test]
    public void DirectoryBuildTargets_EmitManifestOnlyWhenProjectOptedIn()
    {
        var sourcePath = Path.Combine(TestSourcePaths.ToolchainRoot, "Directory.Build.targets");
        var source = File.ReadAllText(sourcePath);

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("EmitDialectRuntimeManifest"));
            Assert.That(source, Does.Contain("'$(EmitDialectRuntimeManifest)' == 'true'"));
            Assert.That(source, Does.Not.Contain("GenerateDialectRuntimeManifestsInOutput"));
            Assert.That(source, Does.Not.Contain("bash -lc"));
            Assert.That(source, Does.Not.Contain("*Module"));
            Assert.That(source, Does.Not.Contain("<MSBuild Projects=\"$(DialectRuntimeManifestEmitterProjectPath)\""));
            Assert.That(source, Does.Not.Contain("SetTargetFramework=\"TargetFramework=$(DialectRuntimeManifestEmitterTargetFramework)\""));
        });
    }

    [Test]
    public void MetadataEmitter_UsesMetadataOnlyInspection()
    {
        var sourcePath = Path.Combine(
            TestSourcePaths.ToolchainRoot,
            "UniversalToolchain.Dialects.ManifestEmitter",
            "Program.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("MetadataLoadContext"));
            Assert.That(source, Does.Not.Contain("AssemblyLoadContext.Default.LoadFromAssemblyPath"));
        });
    }

    [Test]
    public void ManifestEmitter_ModuleManifest_IsDeterministicAndKeepsExactActivationMetadata()
    {
        var assemblyPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "ArithmeticModule.dll");
        Assert.That(File.Exists(assemblyPath), Is.True, "ArithmeticModule.dll is required in the test output.");

        var first = EmitManifest(assemblyPath);
        var second = EmitManifest(assemblyPath);
        var arithmetic = first.Document.Components.Single(static component => component.CanonicalAlias == "Arithmetic");

        Assert.Multiple(() =>
        {
            Assert.That(first.Json, Is.EqualTo(second.Json));
            Assert.That(first.Json, Does.Not.Contain("dialectFamily"));
            Assert.That(first.Document.AssemblySimpleName, Is.EqualTo("ArithmeticModule"));
            Assert.That(arithmetic.Activation, Is.Not.Null);
            Assert.That(arithmetic.Activation!.ActivationTypeFullName, Is.EqualTo("ArithmeticModule.Module.ArithmeticModuleImpl"));
            Assert.That(arithmetic.Activation.ActivationAssemblySimpleName, Is.EqualTo("ArithmeticModule"));
        });
    }

    private static (string Json, FileDialectRuntimeManifestDocument Document) EmitManifest(string assemblyPath)
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, $"{Path.GetFileNameWithoutExtension(assemblyPath)}.dialect.runtime.json");
        var emitterProject = Path.Combine(
            TestSourcePaths.ToolchainRoot,
            "UniversalToolchain.Dialects.ManifestEmitter",
            "UniversalToolchain.Dialects.ManifestEmitter.csproj");
        var configuration = ResolveBuildConfiguration();
        var start = new ProcessStartInfo(
            ResolveDotnetHostPath(),
            $"run --no-restore --project \"{emitterProject}\" -c {configuration} -- --assembly \"{assemblyPath}\" --output \"{outputPath}\"")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = TestSourcePaths.ToolchainRoot
        };

        using var process = Process.Start(start);
        if (process == null)
            throw new InvalidOperationException("Manifest emitter process did not start.");

        if (!process.WaitForExit(120_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Manifest emitter process timed out.");
        }

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        Assert.That(
            process.ExitCode,
            Is.EqualTo(0),
            $"Manifest emitter failed.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");

        var json = File.ReadAllText(outputPath);
        IRuntimeManifestSerializer serializer = new RuntimeManifestJsonSerializer();
        return (json, serializer.Deserialize(json));
    }

    private static string ResolveDotnetHostPath()
    {
        var hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        return string.IsNullOrWhiteSpace(hostPath)
            ? Environment.ProcessPath ?? "dotnet"
            : hostPath;
    }

    private static string ResolveBuildConfiguration()
    {
        var segments = TestContext.CurrentContext.TestDirectory.Split(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
        var binIndex = Array.LastIndexOf(segments, "bin");
        if (binIndex >= 0)
        {
            var configuration = segments
                .Skip(binIndex + 1)
                .FirstOrDefault(static segment =>
                    segment.Equals("Debug", StringComparison.OrdinalIgnoreCase) ||
                    segment.Equals("Release", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(configuration))
                return configuration;
        }

        return "Debug";
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"manifest-emitter-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
