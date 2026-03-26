using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

[TestFixture]
public class DialectRuntimeProfileRegressionTests
{
    [Test]
    public void FullDefaultProfile_ShouldAllowExpectedScenario()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(GetDialectPath("full-default"));
        using var host = workflow.CreateHost(composition);

        var compilerValue = host.Run(File.ReadAllText(GetProgramPath("full-default")), "compiler");
        var interpreterValue = host.Run(File.ReadAllText(GetProgramPath("full-default")), "interpreter");

        Assert.That(composition.IsSuccess, Is.True);
        Assert.That(interpreterValue, Is.EqualTo(compilerValue));
    }

    [Test]
    public void MinimalArithmeticProfile_ShouldRejectOutOfProfileScenario()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(GetDialectPath("minimal-arithmetic"));
        using var host = workflow.CreateHost(composition);

        var ex = Assert.Catch(() => host.Run("let x = 1\nx", "interpreter"));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Does.Contain("token").IgnoreCase);
    }

    [Test]
    public void RestrictedSandboxProfile_ShouldRejectOutOfProfileScenario()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(GetDialectPath("restricted-sandbox"));
        using var host = workflow.CreateHost(composition);

        var ex = Assert.Catch(() => host.Run(File.ReadAllText(GetForbiddenProgramPath("restricted-sandbox")), "interpreter"));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Does.Contain("token").Or.Contain("Variable").Or.Contain("Identifier").IgnoreCase);
    }

    [Test]
    public void RuntimeAliasResolution_ShouldRemainDeterministic()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var first = workflow.ComposeFile(GetDialectPath("full-default"));
        var second = workflow.ComposeFile(GetDialectPath("full-default"));

        Assert.That(first.RuntimeComposition!.EnabledBackends.Select(x => x.CanonicalId), Is.EqualTo(second.RuntimeComposition!.EnabledBackends.Select(x => x.CanonicalId)));
        Assert.That(first.RuntimeComposition.OrderedModules.Select(x => x.CanonicalId), Is.EqualTo(second.RuntimeComposition.OrderedModules.Select(x => x.CanonicalId)));
    }

    private static string GetDialectPath(string dialectName)
        => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", dialectName, "dialect.wistdialect"));

    private static string GetProgramPath(string dialectName)
        => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", dialectName, "program.wist"));

    private static string GetForbiddenProgramPath(string dialectName)
        => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", dialectName, "forbidden-program.wist"));

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        return services.BuildServiceProvider();
    }
}
