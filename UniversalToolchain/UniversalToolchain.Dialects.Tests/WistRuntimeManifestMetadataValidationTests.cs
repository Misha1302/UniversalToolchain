using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistRuntimeManifestMetadataValidationTests
{
    [Test]
    public void FileBasedRuntimeComponentCatalog_ResolvesEntriesWithoutDialectFamily()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var manifestPath = Path.Combine(temp.Path, "ArithmeticModule.dialect.runtime.json");

        var document = new FileDialectRuntimeManifestDocument(
            "ArithmeticModule",
            [new FileDialectRuntimeComponentEntry("FrontendModule", "Arithmetic", [], "ArithmeticModule.Module.ArithmeticModuleImpl")]);
        File.WriteAllText(manifestPath, serializer.Serialize(document));

        var catalog = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([manifestPath]), serializer);

        Assert.That(catalog.TryResolveModule("Arithmetic", out var module), Is.True);
        Assert.That(module!.TypeReference.AssemblySimpleName, Is.EqualTo("ArithmeticModule"));
    }

    [Test]
    public void FileBasedRuntimeComponentCatalog_FailsFastOnDuplicateGlobalModuleAlias()
    {
        var exception = BuildDuplicateAliasException("FrontendModule", "Modules");
        Assert.That(exception.Message, Does.Contain("module alias 'Alias'"));
    }

    [Test]
    public void FileBasedRuntimeComponentCatalog_FailsFastOnDuplicateGlobalOptimizerAlias()
    {
        var exception = BuildDuplicateAliasException("Optimizer", "Optimizers");
        Assert.That(exception.Message, Does.Contain("optimizer alias 'Alias'"));
    }

    [Test]
    public void FileBasedRuntimeComponentCatalog_FailsFastOnDuplicateGlobalBackendAlias()
    {
        var exception = BuildDuplicateAliasException("Backend", "Backends");
        Assert.That(exception.Message, Does.Contain("backend alias 'Alias'"));
    }

    [Test]
    public void FileBasedRuntimeComponentCatalog_EnumeratesModulesOptimizersAndBackends_Deterministically()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var manifestA = Path.Combine(temp.Path, "a.dialect.runtime.json");
        var manifestB = Path.Combine(temp.Path, "b.dialect.runtime.json");

        File.WriteAllText(manifestA, serializer.Serialize(new FileDialectRuntimeManifestDocument(
            "BAssembly",
            [
                new FileDialectRuntimeComponentEntry("Backend", "interpreter", [], "Runtime.Backends.Interpreter"),
                new FileDialectRuntimeComponentEntry("Optimizer", "LocalVariablesOptimization", [], "Runtime.Optimizers.Local"),
                new FileDialectRuntimeComponentEntry("FrontendModule", "Numbers", [], "Runtime.Modules.Numbers")
            ])));
        File.WriteAllText(manifestB, serializer.Serialize(new FileDialectRuntimeManifestDocument(
            "AAssembly",
            [
                new FileDialectRuntimeComponentEntry("Backend", "cil", ["compiler"], "Runtime.Backends.Cil"),
                new FileDialectRuntimeComponentEntry("FrontendModule", "Arithmetic", [], "Runtime.Modules.Arithmetic"),
                new FileDialectRuntimeComponentEntry("Optimizer", "AlphaOptimization", [], "Runtime.Optimizers.Alpha")
            ])));

        var catalog = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([manifestA, manifestB]), serializer);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.GetModulesInDeterministicOrder().Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "Arithmetic", "Numbers" }));
            Assert.That(catalog.GetOptimizersInDeterministicOrder().Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "AlphaOptimization", "LocalVariablesOptimization" }));
            Assert.That(catalog.GetBackendsInDeterministicOrder().Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "cil", "interpreter" }));
        });
    }

    [Test]
    public void SelectionResolver_UsesGlobalCatalogWithoutFamilyFiltering()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        using var provider = services.BuildServiceProvider();

        var compiler = provider.GetRequiredService<DialectDslCompiler>();
        var builder = provider.GetRequiredService<IDialectCompiledDialectBuildPlanBuilder>();
        var resolver = provider.GetRequiredService<SelectedRuntimePlanResolver>();

        const string source = """
                              dialect Demo
                              use Arithmetic,Numbers
                              backend interpreter
                              """;

        var buildPlan = builder.Build(compiler.Compile(source));
        var selected = resolver.Resolve(buildPlan);

        Assert.Multiple(() =>
        {
            Assert.That(selected.IsResolved, Is.True, string.Join(Environment.NewLine, selected.Diagnostics.Select(static x => x.ToString())));
            Assert.That(selected.OrderedModules.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "Arithmetic", "Numbers" }));
            Assert.That(selected.EnabledBackends.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "interpreter" }));
        });
    }

    [Test]
    public void ManifestEmitter_DoesNotWriteDialectFamilyField()
    {
        var testDir = TestContext.CurrentContext.TestDirectory;
        var assemblyPath = Path.Combine(testDir, "ArithmeticModule.dll");
        Assert.That(File.Exists(assemblyPath), Is.True, "ArithmeticModule.dll is required in the test output.");

        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, "ArithmeticModule.dialect.runtime.json");
        var repoRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
        var emitterProject = Path.Combine(repoRoot, "UniversalToolchain.Dialects.ManifestEmitter", "UniversalToolchain.Dialects.ManifestEmitter.csproj");

        var start = new ProcessStartInfo("dotnet", $"run --project \"{emitterProject}\" -- --assembly \"{assemblyPath}\" --output \"{outputPath}\"")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = repoRoot
        };

        using var process = Process.Start(start)!;
        process.WaitForExit();

        Assert.That(process.ExitCode, Is.EqualTo(0), process.StandardError.ReadToEnd());

        var json = File.ReadAllText(outputPath);
        Assert.That(json, Does.Not.Contain("dialectFamily"));

        var serializer = new RuntimeManifestJsonSerializer();
        var document = serializer.Deserialize(json);
        Assert.That(document.AssemblySimpleName, Is.EqualTo("ArithmeticModule"));
    }

    [Test]
    public void DirectoryBuildTargets_EmitManifestOnlyWhenProjectOptedIn()
    {
        var testDir = TestContext.CurrentContext.TestDirectory;
        var sourcePath = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", "Directory.Build.targets"));
        var source = File.ReadAllText(sourcePath);

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("EmitDialectRuntimeManifest"));
            Assert.That(source, Does.Contain("'$(EmitDialectRuntimeManifest)' == 'true'"));
            Assert.That(source, Does.Not.Contain("GenerateDialectRuntimeManifestsInOutput"));
            Assert.That(source, Does.Not.Contain("bash -lc"));
            Assert.That(source, Does.Not.Contain("*Module"));
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
    public void MinimalPath_Compose_DoesNotLoadFeatureAssembliesBeforeHostCreation()
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
    public void WistWorkflow_UsesGenericCatalog()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        using var provider = services.BuildServiceProvider();

        var catalog = provider.GetService<IRuntimeComponentCatalog>();
        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog, Is.TypeOf<FileBasedRuntimeComponentCatalog>());
    }

    private static InvalidOperationException BuildDuplicateAliasException(string kind, string _)
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();

        var first = Path.Combine(temp.Path, "first.dialect.runtime.json");
        var second = Path.Combine(temp.Path, "second.dialect.runtime.json");

        File.WriteAllText(first, serializer.Serialize(new FileDialectRuntimeManifestDocument(
            "AAssembly",
            [new FileDialectRuntimeComponentEntry(kind, "Alias", [], "A.Type")])));
        File.WriteAllText(second, serializer.Serialize(new FileDialectRuntimeManifestDocument(
            "BAssembly",
            [new FileDialectRuntimeComponentEntry(kind, "Alias", [], "B.Type")])));

        return Assert.Throws<InvalidOperationException>(() => new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([first, second]), serializer))!;
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
