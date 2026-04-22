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
        IRuntimeManifestSerializer serializer = new RuntimeManifestJsonSerializer();
        var manifestPath = Path.Combine(temp.Path, "ArithmeticModule.dialect.runtime.json");

        var document = new FileDialectRuntimeManifestDocument(
            "ArithmeticModule",
            [new FileDialectRuntimeComponentEntry("FrontendModule", "Arithmetic", [], "frontend.arithmetic")]);
        File.WriteAllText(manifestPath, serializer.Serialize(document));

        var catalog = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([manifestPath]), serializer);

        Assert.That(catalog.TryResolveModule("Arithmetic", out var module), Is.True);
        Assert.That(module!.AssemblySimpleName, Is.EqualTo("ArithmeticModule"));
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
    public void SelectionResolver_UsesGlobalCatalogWithoutFamilyFiltering()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
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

        var (json, document) = EmitManifest(assemblyPath);
        Assert.That(json, Does.Not.Contain("dialectFamily"));

        Assert.That(document.AssemblySimpleName, Is.EqualTo("ArithmeticModule"));
    }

    [Test]
    public void ManifestEmitter_ModuleEntriesIncludeActivationTypeFullName()
    {
        var testDir = TestContext.CurrentContext.TestDirectory;
        var assemblyPath = Path.Combine(testDir, "ArithmeticModule.dll");
        Assert.That(File.Exists(assemblyPath), Is.True, "ArithmeticModule.dll is required in the test output.");

        var (_, document) = EmitManifest(assemblyPath);
        var arithmetic = document.Components.Single(static x => x.CanonicalAlias == "Arithmetic");
        var activation = arithmetic.Activation;

        Assert.Multiple(() =>
        {
            Assert.That(activation, Is.Not.Null);
            Assert.That(activation?.ActivationTypeFullName, Is.EqualTo("ArithmeticModule.Module.ArithmeticModuleImpl"));
            Assert.That(activation?.RegistrarTypeFullName, Is.Null);
        });
    }

    [Test]
    public void ManifestEmitter_AnnotatedBackendEntriesIncludeActivationAndRegistrarTypeFullNames()
    {
        var testDir = TestContext.CurrentContext.TestDirectory;
        var assemblyPath = Path.Combine(testDir, "UniversalToolchain.Dialects.Wist.dll");
        Assert.That(File.Exists(assemblyPath), Is.True, "UniversalToolchain.Dialects.Wist.dll is required in the test output.");

        var (_, document) = EmitManifest(assemblyPath);
        var cil = document.Components.Single(static x => x.CanonicalAlias == "cil");
        var interpreter = document.Components.Single(static x => x.CanonicalAlias == "interpreter");
        var cilActivation = cil.Activation;
        var interpreterActivation = interpreter.Activation;

        Assert.Multiple(() =>
        {
            Assert.That(cilActivation?.ActivationTypeFullName, Is.EqualTo(typeof(WistCilBackendDeclaration).FullName));
            Assert.That(cilActivation?.RegistrarTypeFullName, Is.EqualTo(typeof(WistCilDialectBackendServiceProvider).FullName));
            Assert.That(interpreterActivation?.ActivationTypeFullName, Is.EqualTo(typeof(WistInterpreterBackendDeclaration).FullName));
            Assert.That(interpreterActivation?.RegistrarTypeFullName, Is.EqualTo(typeof(WistInterpreterDialectBackendServiceProvider).FullName));
        });
    }

    [Test]
    public void ManifestEmitter_BackendEntryWithoutRegistrarAttribute_RemainsValidWithoutRegistrarTypeFullName()
    {
        var assemblyPath = typeof(ManifestEmitterOptionalRegistrarBackendExport).Assembly.Location;

        var (_, document) = EmitManifest(assemblyPath);
        var backend = document.Components.Single(static x => x.CanonicalAlias == "optional-registrar-test-backend");
        var activation = backend.Activation;

        Assert.Multiple(() =>
        {
            Assert.That(activation, Is.Not.Null);
            Assert.That(activation?.ActivationTypeFullName, Is.EqualTo(typeof(ManifestEmitterOptionalRegistrarBackendExport).FullName));
            Assert.That(activation?.RegistrarTypeFullName, Is.Null);
        });
    }

    [Test]
    public void ManifestEmitter_EmissionOrderRemainsDeterministic()
    {
        var testDir = TestContext.CurrentContext.TestDirectory;
        var assemblyPath = Path.Combine(testDir, "UniversalToolchain.Dialects.Wist.dll");
        Assert.That(File.Exists(assemblyPath), Is.True, "UniversalToolchain.Dialects.Wist.dll is required in the test output.");

        var (_, document) = EmitManifest(assemblyPath);
        var componentKeys = document.Components
            .Select(static x => (x.Kind, x.CanonicalAlias, x.ComponentId))
            .ToList();

        var sortedKeys = componentKeys
            .OrderBy(static x => x.Kind, StringComparer.Ordinal)
            .ThenBy(static x => x.CanonicalAlias, StringComparer.Ordinal)
            .ThenBy(static x => x.ComponentId, StringComparer.Ordinal)
            .ToList();

        Assert.That(componentKeys, Is.EqualTo(sortedKeys));
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
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var composition = workflow.ComposeFile(GetDialectPath("minimal-arithmetic"));
        var after = GetLoadedModuleAssemblies();

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True, DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));
            Assert.That(after, Is.EqualTo(before), "Compose stage should not load additional feature assemblies.");
        });
    }

    [Test]
    public void WistWorkflow_UsesGenericCatalog()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        using var provider = services.BuildServiceProvider();

        var catalog = provider.GetService<IRuntimeComponentCatalog>();
        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog, Is.TypeOf<FileBasedRuntimeComponentCatalog>());
    }

    private static InvalidOperationException BuildDuplicateAliasException(string kind, string _)
    {
        using var temp = new TempDirectory();
        IRuntimeManifestSerializer serializer = new RuntimeManifestJsonSerializer();

        var first = Path.Combine(temp.Path, "first.dialect.runtime.json");
        var second = Path.Combine(temp.Path, "second.dialect.runtime.json");

        File.WriteAllText(first, serializer.Serialize(new FileDialectRuntimeManifestDocument(
            "AAssembly",
            [new FileDialectRuntimeComponentEntry(kind, "Alias", [], $"{kind.ToLowerInvariant()}.alias")])));
        File.WriteAllText(second, serializer.Serialize(new FileDialectRuntimeManifestDocument(
            "BAssembly",
            [new FileDialectRuntimeComponentEntry(kind, "Alias", [], $"{kind.ToLowerInvariant()}.alias2")])));

        return Assert.Throws<InvalidOperationException>(() => new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([first, second]), serializer))!;
    }

    private static (string Json, FileDialectRuntimeManifestDocument Document) EmitManifest(string assemblyPath)
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, $"{Path.GetFileNameWithoutExtension(assemblyPath)}.dialect.runtime.json");
        var repoRoot = GetRepoRoot();
        var emitterProject = Path.Combine(repoRoot, "UniversalToolchain.Dialects.ManifestEmitter", "UniversalToolchain.Dialects.ManifestEmitter.csproj");

        var start = new ProcessStartInfo("dotnet", $"run --project \"{emitterProject}\" -- --assembly \"{assemblyPath}\" --output \"{outputPath}\"")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = repoRoot
        };

        using var process = Process.Start(start);
        if (process == null)
        {
            Assert.Fail("Manifest emitter process must start.");
            return (string.Empty, new FileDialectRuntimeManifestDocument(string.Empty, []));
        }

        process.WaitForExit();

        Assert.That(process.ExitCode, Is.EqualTo(0), process.StandardError.ReadToEnd());

        var json = File.ReadAllText(outputPath);
        IRuntimeManifestSerializer serializer = new RuntimeManifestJsonSerializer();
        return (json, serializer.Deserialize(json));
    }

    private static string GetRepoRoot() => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));

    private static string GetDialectPath(string dialectName) => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", dialectName, "dialect.wistdialect"));

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
                Directory.Delete(Path, true);
        }
    }
}

[DialectRuntimeExport("Backend", "optional-registrar-test-backend")]
internal sealed class ManifestEmitterOptionalRegistrarBackendExport;
