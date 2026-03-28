using CommonExceptions;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

[TestFixture]
public class DialectDeterminismAndSandboxContractsTests
{
    [Test]
    public void FullDefaultDialect_ShouldComposeDeterministically_AcrossRepeatedRuns()
        => AssertRepeatedProjectionIsStable("full-default", 20);

    [Test]
    public void MinimalArithmeticDialect_ShouldComposeDeterministically_AcrossRepeatedRuns()
        => AssertRepeatedProjectionIsStable("minimal-arithmetic", 20);

    [Test]
    public void RestrictedSandboxDialect_ShouldComposeDeterministically_AcrossRepeatedRuns()
        => AssertRepeatedProjectionIsStable("restricted-sandbox", 20);

    [Test]
    public void RestrictedSandbox_ShouldNotExposeForbiddenCapabilities()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(GetDialectPath("restricted-sandbox"));
        var legacyComposition = provider.GetRequiredService<LegacyWistDialectCompositionService>().ComposeText(File.ReadAllText(GetDialectPath("restricted-sandbox")), "restricted-sandbox");

        Assert.That(composition.IsSuccess, Is.True);
        Assert.That(legacyComposition.RuntimeComposition!.EnabledBackends.Select(x => x.CanonicalId), Is.EqualTo(new[] { "interpreter" }));
        Assert.That(legacyComposition.RuntimeComposition.OrderedModules.Select(x => x.CanonicalId), Does.Not.Contain("CSharpInterop"));
    }

    [Test]
    public void InvalidDialectInput_ShouldProduceStableDiagnosticContract()
    {
        const string source = """
                              dialect Invalid
                              unknown directive
                              backend interpreter
                              """;

        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var first = Assert.Throws<ParserException>(() => workflow.ComposeText(source, "invalid-1.wistdialect"));
        var second = Assert.Throws<ParserException>(() => workflow.ComposeText(source, "invalid-2.wistdialect"));

        Assert.That(second!.Message.Split('\n')[0], Is.EqualTo(first!.Message.Split('\n')[0]));
    }

    [Test]
    public void DialectCompositionProjection_ShouldRemainStable_AcrossRepeatedBuilds()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var baseline = Project(workflow.ComposeFile(GetDialectPath("full-default")));

        for (var i = 0; i < 20; i++)
            Assert.That(Project(workflow.ComposeFile(GetDialectPath("full-default"))), Is.EqualTo(baseline));
    }

    private static void AssertRepeatedProjectionIsStable(string dialectName, int repetitions)
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var baseline = Project(workflow.ComposeFile(GetDialectPath(dialectName)));

        for (var i = 0; i < repetitions; i++)
            Assert.That(Project(workflow.ComposeFile(GetDialectPath(dialectName))), Is.EqualTo(baseline));
    }

    private static string Project(DialectFrameworkCompositionResult result)
    {
        return string.Join("|", [
            result.IsSuccess.ToString(),
            string.Join(",", result.SemanticDiagnostics.Select(x => $"{x.Code}:{x.Message}")),
            string.Join(",", result.ResolutionDiagnostics.Select(x => $"{x.Code}:{x.Message}")),
            string.Join(",", (result.RuntimeSelection as SelectedRuntimePlan)?.OrderedModules.Select(x => x.CanonicalAlias) ?? []),
            string.Join(",", (result.RuntimeSelection as SelectedRuntimePlan)?.EnabledBackends.Select(x => x.CanonicalAlias) ?? []),
            string.Join(",", (result.RuntimeSelection as SelectedRuntimePlan)?.EnabledOptimizers.Select(x => x.CanonicalAlias) ?? [])
        ]);
    }

    private static string GetDialectPath(string dialectName)
        => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", dialectName, "dialect.wistdialect"));

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServicesLegacy();
        return services.BuildServiceProvider();
    }
}
