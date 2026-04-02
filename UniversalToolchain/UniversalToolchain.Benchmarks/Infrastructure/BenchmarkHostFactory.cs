using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Benchmarks.Infrastructure;

public static class BenchmarkHostFactory
{
    private const string DialectText = """
                                      dialect Benchmarks
                                      use Arithmetic,Numbers,Variables,Conditions
                                      backend compiler
                                      """;

    public static WistDialectExecutionHost CreateWistHost()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(DialectText, "benchmarks-inline");
        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(composition.ToDeterministicText());

        return workflow.CreateHost(composition);
    }
}
