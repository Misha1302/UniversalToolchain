using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistMinimalRuntimeMemorySmokeTests
{
    [Test]
    public void MinimalPath_RepeatedComposeAndHostCreation_DoesNotGrowLoadedAssemblySetUnexpectedly()
    {
        using var provider = CreateMinimalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var before = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName().Name).ToHashSet(StringComparer.Ordinal);

        for (var i = 0; i < 20; i++)
        {
            var result = workflow.ComposeText("dialect Demo\nuse Arithmetic,Numbers,Whitespaces\nbackend interpreter", $"s{i}");
            using var host = workflow.CreateHost(result);
            _ = host.Run("2 + 3", "interpreter");
        }

        var after = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName().Name).ToHashSet(StringComparer.Ordinal);
        Assert.That(after.Count - before.Count, Is.LessThanOrEqualTo(5));
    }

    [Test]
    public void MinimalPath_RepeatedOperations_DoNotGrowServiceCounts()
    {
        using var provider = CreateMinimalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var result = workflow.ComposeText("dialect Demo\nuse Arithmetic,Numbers,Whitespaces\nbackend interpreter", "inline");
        (int modules, int ir, int backends)? baseline = null;

        for (var i = 0; i < 20; i++)
        {
            using var host = workflow.CreateHost(result);
            var current = (host.Configuration.FrontendModules.Count, host.Configuration.IrModules.Count, host.Configuration.BackendConfigurations.Count);
            baseline ??= current;
            Assert.That(current, Is.EqualTo(baseline.Value));
        }
    }

    private static ServiceProvider CreateMinimalProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServicesMinimal();
        return services.BuildServiceProvider();
    }
}
