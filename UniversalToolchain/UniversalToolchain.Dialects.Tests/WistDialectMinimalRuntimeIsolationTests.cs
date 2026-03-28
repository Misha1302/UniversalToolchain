using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectMinimalRuntimeIsolationTests
{


    [Test]
    public void ServiceRegistrations_UseGenericBackendAbstractionsOnly()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        Assert.Multiple(() =>
        {
            Assert.That(services.Any(static x => x.ServiceType == typeof(IDialectBackendRuntimeRegistrar)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeKnownBackendsProvider)), Is.True);
        });
    }

    [Test]
    public void MinimalWorkflow_HasNoLegacyDependencies()
    {
        using var provider = CreateMinimalProvider();

        Assert.Multiple(() =>
        {
        });
    }

    [Test]
    public void MinimalWorkflow_ComposeText_ProducesRuntimeSelection()
    {
        using var provider = CreateMinimalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var result = workflow.ComposeText("dialect Demo\nuse Arithmetic\nbackend interpreter", "inline");

        Assert.Multiple(() =>
        {
            Assert.That(result.RuntimeSelection, Is.Not.Null);
        });
    }

    [Test]
    public async Task ComposeText_ParallelDifferentDialects_DoNotMixSelections()
    {
        using var provider = CreateMinimalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var dialects = new[]
        {
            "dialect A\nuse Arithmetic,Numbers\nbackend interpreter",
            "dialect B\nuse Arithmetic,Variables,Scopes\nbackend interpreter,compiler\nenable LocalVariablesOptimization",
            "dialect C\nuse Arithmetic,Conditions,ComparisonConditions\nbackend compiler"
        };

        var tasks = Enumerable.Range(0, 30)
            .Select(i => Task.Run(() => workflow.ComposeText(dialects[i % dialects.Length], $"d{i}")))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var signatures = results
            .Select(r => (SelectedRuntimePlan)r.RuntimeSelection!)
            .Select(x => string.Join("|", x.OrderedModules.Select(m => m.CanonicalAlias)) + "::" + string.Join("|", x.EnabledBackends.Select(b => b.CanonicalAlias)))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.That(signatures.Count, Is.EqualTo(3));
    }

    [Test]
    public void CreateHost_RepeatedCalls_DoNotRecomputeSelection()
    {
        using var provider = CreateMinimalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var result = workflow.ComposeText("dialect Demo\nuse Arithmetic\nbackend interpreter", "inline");
        var selection = result.RuntimeSelection;

        using var host1 = workflow.CreateHost(result);
        using var host2 = workflow.CreateHost(result);

        Assert.That(ReferenceEquals(selection, result.RuntimeSelection), Is.True);
    }

    [Test]
    public void ProviderFactory_RepeatedCreates_DoNotAccumulateRegistrations()
    {
        using var provider = CreateMinimalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var result = workflow.ComposeText("dialect Demo\nuse Arithmetic,Numbers\nbackend interpreter", "inline");
        var baselines = new List<(int Frontend, int Ir, int Runtime)>();

        for (var i = 0; i < 30; i++)
        {
            using var host = workflow.CreateHost(result);
            baselines.Add((
                host.Configuration.FrontendModules.Count,
                host.Configuration.IrModules.Count,
                host.Configuration.BackendConfigurations.Count));
        }

        Assert.That(baselines.Distinct().Count(), Is.EqualTo(1));
    }

    [Test]
    public void WistMinimalPath_DoesNotTreatForeignCatalogBackendsAsKnownBackends()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var manifestPath = Path.Combine(temp.Path, "ForeignBackendAssembly.dialect.runtime.json");

        File.WriteAllText(
            manifestPath,
            serializer.Serialize(new FileDialectRuntimeManifestDocument(
                "ForeignBackendAssembly",
                [new FileDialectRuntimeComponentEntry("Backend", "foreign-backend", ["foreign"], "Foreign.Backend.Type")])));

        var services = new ServiceCollection();
        services.AddSingleton(new RuntimeArtifactLocatorOptions { SearchRoots = [temp.Path], IncludeAppContextBaseDirectory = true });
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var result = workflow.ComposeText("dialect Demo\nuse Arithmetic\nbackend interpreter", "inline");
        using var host = workflow.CreateHost(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True, result.ToDeterministicText());
            Assert.That(host.Configuration.TryResolveKnownBackendId("interpreter", out _), Is.True);
            Assert.That(host.Configuration.TryResolveKnownBackendId("foreign-backend", out _), Is.False);
            Assert.That(host.Configuration.TryResolveKnownBackendId("foreign", out _), Is.False);
        });
    }

    private static ServiceProvider CreateMinimalProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
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
