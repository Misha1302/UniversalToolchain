using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Tests.Wist;
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
    public void MinimalWorkflow_ComposeText_ProducesRuntimeSelection()
    {
        using var provider = CreateMinimalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var result = workflow.ComposeText("dialect Demo\nuse Arithmetic\nbackend interpreter", "inline");

        Assert.Multiple(() => { Assert.That(result.RuntimeSelection, Is.Not.Null); });
    }

    [Test]
    public void ComposeText_RepeatedRuns_ProducesStableRuntimeSelection()
    {
        using var provider = CreateMinimalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var signatures = new List<string>();

        for (var i = 0; i < 40; i++)
        {
            var result = workflow.ComposeText("dialect Stable\nuse Arithmetic,Numbers,Whitespaces\nenable LocalVariablesOptimization\nbackend interpreter,compiler", $"stable-{i}");
            Assert.That(result.IsSuccess, Is.True, result.ToDeterministicText());
            signatures.Add(WistDialectTestInfrastructure.BuildSelectionAndDiagnosticsSignature(result));
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
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
            .Select(i => Task.Run(() =>
            {
                var dialectIndex = i % dialects.Length;
                var result = workflow.ComposeText(dialects[dialectIndex], $"d{i}");
                return (DialectIndex: dialectIndex, Result: result);
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var signatures = results
            .Select(static x => WistDialectTestInfrastructure.BuildSelectionSignature(x.Result))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(results.All(static x => x.Result.IsSuccess), Is.True, string.Join(Environment.NewLine, results.Select(static x => x.Result.ToDeterministicText())));
            Assert.That(signatures.Count, Is.EqualTo(3));
            Assert.That(results.Where(static x => x.DialectIndex == 0).Select(static x => WistDialectTestInfrastructure.BuildSelectionSignature(x.Result)).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
            Assert.That(results.Where(static x => x.DialectIndex == 1).Select(static x => WistDialectTestInfrastructure.BuildSelectionSignature(x.Result)).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
            Assert.That(results.Where(static x => x.DialectIndex == 2).Select(static x => WistDialectTestInfrastructure.BuildSelectionSignature(x.Result)).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
        });
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
    public void CreateHost_RepeatedCalls_UsesStableConfiguration()
    {
        using var provider = CreateMinimalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var result = workflow.ComposeText("dialect StableHost\nuse Arithmetic,Numbers,Whitespaces\nbackend interpreter,compiler", "stable-host");
        Assert.That(result.IsSuccess, Is.True, result.ToDeterministicText());

        var signatures = new List<string>();
        for (var i = 0; i < 40; i++)
        {
            using var host = workflow.CreateHost(result);
            signatures.Add(WistDialectTestInfrastructure.BuildHostSignature(host));
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
    }

    [Test]
    public void ComposeCreateRun_RepeatedCycles_KeepSelectionsAndResultsIsolated()
    {
        using var provider = CreateMinimalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var signatures = new List<string>();

        for (var i = 0; i < 30; i++)
        {
            var result = workflow.ComposeText("dialect Cycle\nuse Arithmetic,Numbers,Whitespaces\nbackend interpreter", $"cycle-{i}");
            Assert.That(result.IsSuccess, Is.True, result.ToDeterministicText());

            using var host = workflow.CreateHost(result);
            var runResult = host.Run("2 + 5", "interpreter");
            signatures.Add(WistDialectTestInfrastructure.BuildSelectionSignature(result) + "##" + WistDialectTestInfrastructure.BuildHostSignature(host) + "##" + runResult);
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task ComposeCreateRun_ParallelDifferentDialects_DoNotCrossPolluteSelections()
    {
        using var provider = CreateMinimalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var scenarios = new[]
        {
            ("dialect A\nuse Arithmetic,Numbers,Whitespaces\nbackend interpreter", "2 + 5", "interpreter"),
            ("dialect B\nuse Arithmetic,Variables,Scopes,Numbers,Whitespaces\nbackend interpreter,compiler\nenable LocalVariablesOptimization", "2 + 5", "interpreter"),
            ("dialect C\nuse Arithmetic,Numbers,Whitespaces\nbackend compiler", "3 + 4", "compiler")
        };

        var results = await Task.WhenAll(Enumerable.Range(0, 30).Select(i => Task.Run(() =>
        {
            var scenarioIndex = i % scenarios.Length;
            var scenario = scenarios[scenarioIndex];
            var composition = workflow.ComposeText(scenario.Item1, $"scenario-{i}");
            if (!composition.IsSuccess)
                return (ScenarioIndex: scenarioIndex, Signature: "compose-failed:" + composition.ToDeterministicText());

            using var host = workflow.CreateHost(composition);
            var runResult = host.Run(scenario.Item2, scenario.Item3);
            return (ScenarioIndex: scenarioIndex, Signature: WistDialectTestInfrastructure.BuildSelectionSignature(composition) + "##" + WistDialectTestInfrastructure.BuildHostSignature(host) + "##" + runResult);
        })));

        Assert.Multiple(() =>
        {
            Assert.That(results.All(static x => !x.Signature.StartsWith("compose-failed:", StringComparison.Ordinal)), Is.True, string.Join(Environment.NewLine, results.Select(static x => x.Signature)));
            Assert.That(results.Select(static x => x.Signature).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(scenarios.Length));
            Assert.That(results.Where(static x => x.ScenarioIndex == 0).Select(static x => x.Signature).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
            Assert.That(results.Where(static x => x.ScenarioIndex == 1).Select(static x => x.Signature).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
            Assert.That(results.Where(static x => x.ScenarioIndex == 2).Select(static x => x.Signature).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
        });
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
                Directory.Delete(Path, true);
        }
    }
}
