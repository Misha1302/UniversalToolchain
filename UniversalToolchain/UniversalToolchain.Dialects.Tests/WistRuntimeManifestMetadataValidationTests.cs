using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
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
            [new FileDialectRuntimeComponentEntry("FrontendModule", "Arithmetic", [], "frontend.arithmetic", new FileRuntimeComponentActivationEntry(new RuntimeTypeReference("ArithmeticModule", "ArithmeticModule.Module.ArithmeticModuleImpl")))]);
        File.WriteAllText(manifestPath, serializer.Serialize(document));

        var catalog = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([manifestPath]), serializer);

        Assert.That(catalog.TryResolveModule("Arithmetic", out var module), Is.True);
        Assert.That(module!.AssemblySimpleName, Is.EqualTo("ArithmeticModule"));
    }

    [Test]
    public void FileBasedRuntimeComponentCatalog_FailsFastOnDuplicateGlobalModuleAlias()
    {
        var exception = BuildDuplicateAliasException("FrontendModule");
        Assert.That(exception.Message, Does.Contain("module alias 'Alias'"));
    }

    [Test]
    public void FileBasedRuntimeComponentCatalog_FailsFastOnDuplicateGlobalOptimizerAlias()
    {
        var exception = BuildDuplicateAliasException("Optimizer");
        Assert.That(exception.Message, Does.Contain("optimizer alias 'Alias'"));
    }

    [Test]
    public void FileBasedRuntimeComponentCatalog_FailsFastOnDuplicateGlobalBackendAlias()
    {
        var exception = BuildDuplicateAliasException("Backend");
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
            Assert.That(activation?.ActivationAssemblySimpleName, Is.EqualTo("ArithmeticModule"));
            Assert.That(activation?.RegistrarTypeFullName, Is.Null);
            Assert.That(activation?.RegistrarAssemblySimpleName, Is.Null);
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
            Assert.That(cilActivation?.ActivationAssemblySimpleName, Is.EqualTo(typeof(WistCilBackendDeclaration).Assembly.GetName().Name));
            Assert.That(cilActivation?.RegistrarTypeFullName, Is.EqualTo(typeof(WistCilDialectBackendServiceProvider).FullName));
            Assert.That(cilActivation?.RegistrarAssemblySimpleName, Is.EqualTo(typeof(WistCilDialectBackendServiceProvider).Assembly.GetName().Name));
            Assert.That(interpreterActivation?.ActivationTypeFullName, Is.EqualTo(typeof(WistInterpreterBackendDeclaration).FullName));
            Assert.That(interpreterActivation?.ActivationAssemblySimpleName, Is.EqualTo(typeof(WistInterpreterBackendDeclaration).Assembly.GetName().Name));
            Assert.That(interpreterActivation?.RegistrarTypeFullName, Is.EqualTo(typeof(WistInterpreterDialectBackendServiceProvider).FullName));
            Assert.That(interpreterActivation?.RegistrarAssemblySimpleName, Is.EqualTo(typeof(WistInterpreterDialectBackendServiceProvider).Assembly.GetName().Name));
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
        });
    }

    [Test]
    public void MetadataEmitter_UsesMetadataOnlyInspection()
    {
        var sourcePath = Path.Combine(TestSourcePaths.ToolchainRoot, "UniversalToolchain.Dialects.ManifestEmitter", "Program.cs");
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

    private static InvalidOperationException BuildDuplicateAliasException(string kind)
    {
        using var temp = new TempDirectory();
        IRuntimeManifestSerializer serializer = new RuntimeManifestJsonSerializer();

        var first = Path.Combine(temp.Path, "first.dialect.runtime.json");
        var second = Path.Combine(temp.Path, "second.dialect.runtime.json");

        File.WriteAllText(first, serializer.Serialize(new FileDialectRuntimeManifestDocument(
            "AAssembly",
            [new FileDialectRuntimeComponentEntry(kind, "Alias", [], $"{kind.ToLowerInvariant()}.alias", new FileRuntimeComponentActivationEntry(new RuntimeTypeReference("AAssembly", "Test.AliasA")))])));
        File.WriteAllText(second, serializer.Serialize(new FileDialectRuntimeManifestDocument(
            "BAssembly",
            [new FileDialectRuntimeComponentEntry(kind, "Alias", [], $"{kind.ToLowerInvariant()}.alias2", new FileRuntimeComponentActivationEntry(new RuntimeTypeReference("BAssembly", "Test.AliasB")))])));

        return Assert.Throws<InvalidOperationException>(() => { _ = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([first, second]), serializer); })!;
    }

    private static (string Json, FileDialectRuntimeManifestDocument Document) EmitManifest(string assemblyPath)
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, $"{Path.GetFileNameWithoutExtension(assemblyPath)}.dialect.runtime.json");
        var repoRoot = GetRepoRoot();
        var emitterProject = Path.Combine(repoRoot, "UniversalToolchain.Dialects.ManifestEmitter", "UniversalToolchain.Dialects.ManifestEmitter.csproj");
        var dotnetHostPath = ResolveDotnetHostPath();
        var configuration = ResolveBuildConfiguration();
        var platform = ResolveBuildPlatform();
        var platformArgument = string.IsNullOrWhiteSpace(platform) ? string.Empty : $" -p:Platform=\"{platform}\"";

        var start = new ProcessStartInfo(dotnetHostPath, $"run --no-restore --project \"{emitterProject}\" -c {configuration}{platformArgument} -- --assembly \"{assemblyPath}\" --output \"{outputPath}\"")
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

        var completed = process.WaitForExit(120_000);
        if (!completed)
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("Manifest emitter process timed out.");
        }

        Assert.That(process.ExitCode, Is.EqualTo(0), process.StandardError.ReadToEnd());

        var json = File.ReadAllText(outputPath);
        IRuntimeManifestSerializer serializer = new RuntimeManifestJsonSerializer();
        return (json, serializer.Deserialize(json));
    }


    private static string ResolveDotnetHostPath()
    {
        var hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(hostPath))
            return hostPath;

        return Environment.ProcessPath ?? "dotnet";
    }

    private static string ResolveBuildConfiguration()
    {
        var segments = TestContext.CurrentContext.TestDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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

    private static string? ResolveBuildPlatform()
    {
        var segments = TestContext.CurrentContext.TestDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var binIndex = Array.LastIndexOf(segments, "bin");
        if (binIndex < 0 || binIndex + 1 >= segments.Length)
            return null;

        var first = segments[binIndex + 1];
        if (first.Equals("Debug", StringComparison.OrdinalIgnoreCase) || first.Equals("Release", StringComparison.OrdinalIgnoreCase))
            return null;

        return first;
    }

    private static string GetRepoRoot() => TestSourcePaths.ToolchainRoot;

    private static string GetDialectPath(string dialectName) => TestSourcePaths.WistExampleDialectPath(dialectName);

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
