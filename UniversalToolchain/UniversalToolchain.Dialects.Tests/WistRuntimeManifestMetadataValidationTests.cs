using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistRuntimeManifestMetadataValidationTests
{
    [Test]
    public void WistRuntimeManifest_NoHardcodedEntries_RemainsFileBased()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var manifestPath = Path.Combine(temp.Path, "ArithmeticModule.dialect.runtime.json");

        var document = new FileDialectRuntimeManifestDocument(
            "wist",
            "ArithmeticModule",
            [new FileDialectRuntimeComponentEntry("FrontendModule", "Arithmetic", [], "ArithmeticModule.Module.ArithmeticModuleImpl")]);
        File.WriteAllText(manifestPath, serializer.Serialize(document));

        var manifest = new WistRuntimeManifest(new StaticManifestLocator([manifestPath]), serializer);

        Assert.Multiple(() =>
        {
            Assert.That(manifest.Modules.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "Arithmetic" }));
            Assert.That(manifest.Optimizers, Is.Empty);
            Assert.That(manifest.Backends, Is.Empty);
        });
    }

    [Test]
    public void ManifestAggregator_LoadsMultipleSidecarFiles_Deterministically()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();

        var paths = new[]
        {
            Path.Combine(temp.Path, "b.dialect.runtime.json"),
            Path.Combine(temp.Path, "a.dialect.runtime.json")
        };

        File.WriteAllText(paths[0], serializer.Serialize(new FileDialectRuntimeManifestDocument(
            "wist",
            "BAssembly",
            [new FileDialectRuntimeComponentEntry("FrontendModule", "B", [], "B.Type")])));
        File.WriteAllText(paths[1], serializer.Serialize(new FileDialectRuntimeManifestDocument(
            "wist",
            "AAssembly",
            [new FileDialectRuntimeComponentEntry("FrontendModule", "A", [], "A.Type")])));

        var manifest = new WistRuntimeManifest(new StaticManifestLocator(paths), serializer);
        Assert.That(manifest.Modules.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public void ManifestAggregator_DuplicateAliasAcrossAssemblies_FailsFast_WithClearMessage()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();

        var first = Path.Combine(temp.Path, "first.dialect.runtime.json");
        var second = Path.Combine(temp.Path, "second.dialect.runtime.json");

        File.WriteAllText(first, serializer.Serialize(new FileDialectRuntimeManifestDocument(
            "wist",
            "AAssembly",
            [new FileDialectRuntimeComponentEntry("FrontendModule", "Alias", [], "A.Type")])));
        File.WriteAllText(second, serializer.Serialize(new FileDialectRuntimeManifestDocument(
            "wist",
            "BAssembly",
            [new FileDialectRuntimeComponentEntry("FrontendModule", "Alias", [], "B.Type")])));

        var exception = Assert.Throws<InvalidOperationException>(() => new WistRuntimeManifest(new StaticManifestLocator([first, second]), serializer));
        Assert.That(exception!.Message, Does.Contain("Alias").And.Contain("Modules"));
    }

    [Test]
    public void ManifestEmitter_WritesExpectedJson_ForSingleAssembly()
    {
        var testDir = TestContext.CurrentContext.TestDirectory;
        var assemblyPath = Path.Combine(testDir, "ArithmeticModule.dll");
        Assert.That(File.Exists(assemblyPath), Is.True, "ArithmeticModule.dll is required in the test output.");

        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, "ArithmeticModule.dialect.runtime.json");
        var repoRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
        var emitterProject = Path.Combine(repoRoot, "UniversalToolchain.Dialects.ManifestEmitter", "UniversalToolchain.Dialects.ManifestEmitter.csproj");

        var start = new ProcessStartInfo("dotnet", $"run --project \"{emitterProject}\" -- --assembly \"{assemblyPath}\" --dialect-family wist --output \"{outputPath}\"")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = repoRoot
        };

        using var process = Process.Start(start)!;
        process.WaitForExit();

        Assert.That(process.ExitCode, Is.EqualTo(0), process.StandardError.ReadToEnd());

        var serializer = new RuntimeManifestJsonSerializer();
        var document = serializer.Deserialize(File.ReadAllText(outputPath));
        var arithmeticEntry = document.Components.Single(static x => x.CanonicalAlias == "Arithmetic");

        Assert.Multiple(() =>
        {
            Assert.That(document.DialectFamily, Is.EqualTo("wist"));
            Assert.That(document.AssemblySimpleName, Is.EqualTo("ArithmeticModule"));
            Assert.That(arithmeticEntry.Kind, Is.EqualTo("FrontendModule"));
            Assert.That(arithmeticEntry.TypeFullName, Is.EqualTo("ArithmeticModule.Module.ArithmeticModuleImpl"));
        });
    }

    [Test]
    public void MetadataEmitter_UsesMetadataOnlyInspection()
    {
        var testDir = TestContext.CurrentContext.TestDirectory;
        var sourcePath = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", "UniversalToolchain.Dialects.ManifestEmitter", "Program.cs"));
        var source = File.ReadAllText(sourcePath);

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("MetadataLoadContext"));
            Assert.That(source, Does.Not.Contain("AssemblyLoadContext.Default.LoadFromAssemblyPath"));
        });
    }

    [Test]
    public void MinimalPath_Compose_DoesNotLoadFeatureAssemblyBeforeTypeLoad()
    {
        var before = GetLoadedModuleAssemblies();

        var services = new ServiceCollection();
        services.AddWistDialectServices();
        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var composition = workflow.ComposeFile(GetDialectPath("minimal-arithmetic"));
        var after = GetLoadedModuleAssemblies();

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True, composition.ToDeterministicText());
            Assert.That(after, Is.EqualTo(before), "Compose stage should not load additional feature assemblies.");
        });
    }

    [Test]
    public void MinimalPath_CreateHost_LoadsOnlySelectedAssemblies()
    {
        var before = GetLoadedModuleAssemblies();

        var services = new ServiceCollection();
        services.AddWistDialectServices();
        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var composition = workflow.ComposeFile(GetDialectPath("minimal-arithmetic"));
        using var host = workflow.CreateHost(composition);
        var after = GetLoadedModuleAssemblies();
        var loadedByHost = after.Except(before, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True, composition.ToDeterministicText());
            Assert.That(loadedByHost, Does.Not.Contain("VariablesModule"));
            Assert.That(loadedByHost, Does.Not.Contain("IdentifierModule"));
        });
    }

    private static string GetDialectPath(string dialectName)
    {
        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", dialectName, "dialect.wistdialect"));
    }

    private static IReadOnlySet<string> GetLoadedModuleAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(static x => x.GetName().Name)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x!)
            .Where(static x => x.EndsWith("Module", StringComparison.Ordinal) || x == "UniversalToolchain.Dialects.Wist")
            .ToHashSet(StringComparer.Ordinal);
    }

    private sealed class StaticManifestLocator(IReadOnlyList<string> paths) : IRuntimeManifestFileLocator
    {
        public IReadOnlyList<string> GetManifestFilePaths() => paths;
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dialect-tests-{Guid.NewGuid():N}");
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
