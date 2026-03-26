using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectRuntimeSelectionPipelineTests
{
    [Test]
    public void SelectionResolver_SameDialect_RepeatedRuns_ProduceSameSelectionOrder()
    {
        using var provider = CreateProvider();
        var compiler = provider.GetRequiredService<DialectDslCompiler>();
        var buildPlanBuilder = provider.GetRequiredService<IDialectCompiledDialectBuildPlanBuilder>();
        var resolver = provider.GetRequiredService<DialectRuntimeSelectionResolver>();

        var sourceText = File.ReadAllText(Path.Combine(ResolveExampleDirectory("full-default"), "dialect.wistdialect"));
        var expectedSignature = string.Empty;

        for (var i = 0; i < 50; i++)
        {
            var buildPlan = buildPlanBuilder.Build(compiler.Compile(sourceText));
            var selection = resolver.Resolve(buildPlan);
            var signature = string.Join("|", selection.OrderedModules.Select(x => x.CanonicalAlias)) + ";" +
                            string.Join("|", selection.EnabledOptimizers.Select(x => x.CanonicalAlias)) + ";" +
                            string.Join("|", selection.EnabledBackends.Select(x => x.CanonicalId.Value));

            if (i == 0)
                expectedSignature = signature;
            else
                Assert.That(signature, Is.EqualTo(expectedSignature));
        }
    }

    [Test]
    public void SelectionResolver_MissingAlias_AddsDiagnostic()
    {
        var catalog = new DialectRuntimeCatalogBuilder().Build();
        var resolver = new DialectRuntimeSelectionResolver(catalog);

        var plan = new UniversalToolchain.Dialects.Abstractions.DialectBuildPlan(
            "test",
            null,
            ["UnknownModule"],
            [WistDialectBackendIds.Interpreter],
            [],
            [],
            [],
            null,
            [],
            new UniversalToolchain.Dialects.Abstractions.DialectValidationResult([]));

        var selection = resolver.Resolve(plan);

        Assert.That(selection.Diagnostics.Any(x => x.Code == "R001"), Is.True);
    }

    [Test]
    public void DialectRuntimeProviderFactory_RepeatedCreates_DoNotAccumulateServices()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var composition = workflow.ComposeFile(Path.Combine(ResolveExampleDirectory("minimal-arithmetic"), "dialect.wistdialect"));
        Assert.That(composition.IsSuccess, Is.True);

        var resolver = provider.GetRequiredService<DialectRuntimeSelectionResolver>();
        var factory = provider.GetRequiredService<DialectRuntimeProviderFactory>();
        var selection = resolver.Resolve(composition.BuildPlan!);

        var baselines = (frontend: 0, ir: 0, backends: 0);
        for (var i = 0; i < 20; i++)
        {
            var scopedProvider = factory.CreateProvider(selection, composition.BuildPlan!);
            using var disposableProvider = (IDisposable)scopedProvider;
            var frontendCount = scopedProvider.GetServices<BasicCore.Contracts.IFrontendCoreModule>().Count();
            var irCount = scopedProvider.GetServices<BasicCore.Contracts.IIRProcessingModule>().Count();
            var backendCount = scopedProvider.GetServices<WistDialectBackendRuntime>().Count();

            if (i == 0)
                baselines = (frontendCount, irCount, backendCount);
            else
                Assert.That((frontendCount, irCount, backendCount), Is.EqualTo(baselines));
        }
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        return services.BuildServiceProvider();
    }

    private static string ResolveExampleDirectory(string name)
    {
        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", name));
    }
}
