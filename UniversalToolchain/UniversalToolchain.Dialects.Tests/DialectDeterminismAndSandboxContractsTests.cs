using CommonExceptions;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests;

public class DialectDeterminismAndSandboxContractsTests
{
    [Test]
    public void FullDefaultDialect_ShouldComposeDeterministically_AcrossRepeatedRuns()
    {
        AssertStableProjection("full-default", 20);
    }

    [Test]
    public void MinimalArithmeticDialect_ShouldComposeDeterministically_AcrossRepeatedRuns()
    {
        AssertStableProjection("minimal-arithmetic", 20);
    }

    [Test]
    public void RestrictedSandboxDialect_ShouldComposeDeterministically_AcrossRepeatedRuns()
    {
        AssertStableProjection("restricted-sandbox", 20);
    }

    [Test]
    public void RestrictedSandbox_ShouldNotExposeForbiddenCapabilities()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var result = workflow.ComposeFile(GetDialectPath("restricted-sandbox"));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.RuntimeComposition!.EnabledBackends.Select(x => x.CanonicalId), Is.EqualTo(new[] { "interpreter" }));
        Assert.That(result.RuntimeComposition.OrderedModules.Select(x => x.ImplementationType.Name), Does.Not.Contain("CSharpInteropModuleImpl"));
    }

    [Test]
    public void InvalidDialectInput_ShouldProduceStableDiagnostics()
    {
        const string source = """
dialect Bad
unknown something
backend interpreter
""";

        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var first = Assert.Throws<ParserException>(() => workflow.ComposeText(source, "invalid.wistdialect"));
        var second = Assert.Throws<ParserException>(() => workflow.ComposeText(source, "invalid.wistdialect"));

        Assert.That(second!.Message, Is.EqualTo(first!.Message));
    }

    [Test]
    public void DialectCompositionProjection_ShouldRemainStable_AcrossRepeatedBuilds()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var expected = Project(workflow.ComposeFile(GetDialectPath("full-default")));

        for (var i = 0; i < 20; i++)
        {
            Assert.That(Project(workflow.ComposeFile(GetDialectPath("full-default"))), Is.EqualTo(expected));
        }
    }

    private static void AssertStableProjection(string exampleName, int repetitions)
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var expected = Project(workflow.ComposeFile(GetDialectPath(exampleName)));

        for (var i = 0; i < repetitions; i++)
        {
            Assert.That(Project(workflow.ComposeFile(GetDialectPath(exampleName))), Is.EqualTo(expected));
        }
    }

    private static string Project(DialectFrameworkCompositionResult result)
    {
        return string.Join("|", [
            result.IsSuccess.ToString(),
            string.Join(",", result.SemanticDiagnostics.Select(x => x.Message)),
            string.Join(",", result.ResolutionDiagnostics.Select(x => x.Message)),
            string.Join(",", result.RuntimeComposition?.OrderedModules.Select(x => x.CanonicalId) ?? []),
            string.Join(",", result.RuntimeComposition?.EnabledBackends.Select(x => x.CanonicalId) ?? [])
        ]);
    }

    private static string GetDialectPath(string exampleName)
    {
        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", exampleName, "dialect.wistdialect"));
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        return services.BuildServiceProvider();
    }
}
