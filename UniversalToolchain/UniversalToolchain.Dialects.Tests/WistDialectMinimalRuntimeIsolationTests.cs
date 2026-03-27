using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectMinimalRuntimeIsolationTests
{
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

    private static ServiceProvider CreateMinimalProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServicesMinimal();
        return services.BuildServiceProvider();
    }
}
