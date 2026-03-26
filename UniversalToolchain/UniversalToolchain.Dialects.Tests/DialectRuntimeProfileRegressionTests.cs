using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class DialectRuntimeProfileRegressionTests
{
    [Test]
    public void FullDefaultProfile_ShouldAllowExpectedScenario()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var result = workflow.ComposeFile(GetDialectPath("full-default"));
        using var host = workflow.CreateHost(result);

        var value = host.Run("NumbersModule.Core.RealNumberImpl.Add(2, 5)", "compiler");

        Assert.That(value, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void MinimalArithmeticProfile_ShouldRejectOutOfProfileScenario()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var result = workflow.ComposeFile(GetDialectPath("minimal-arithmetic"));

        using var host = workflow.CreateHost(result);
        var ex = Assert.Catch(() => host.Run("let x = 1\nx = x + 1\nx", "interpreter"));

        Assert.That(ex!.Message, Does.Contain("Invalid token").IgnoreCase);
    }

    [Test]
    public void RestrictedSandboxProfile_ShouldRejectOutOfProfileScenario()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var result = workflow.ComposeFile(GetDialectPath("restricted-sandbox"));
        using var host = workflow.CreateHost(result);

        var ex = Assert.Catch(() => host.Run("System.Math.Abs(-5)", "interpreter"));

        Assert.That(ex!.Message, Does.Contain("Invalid token").IgnoreCase);
    }

    [Test]
    public void RuntimeAliasResolution_ShouldRemainDeterministic()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var first = workflow.ComposeFile(GetDialectPath("full-default"));
        var second = workflow.ComposeFile(GetDialectPath("full-default"));

        Assert.That(first.RuntimeComposition!.EnabledBackends.Select(x => x.CanonicalId), Is.EqualTo(second.RuntimeComposition!.EnabledBackends.Select(x => x.CanonicalId)));
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
