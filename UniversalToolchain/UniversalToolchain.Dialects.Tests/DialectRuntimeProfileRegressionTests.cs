using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

[TestFixture]
public class DialectRuntimeProfileRegressionTests
{
    [Test]
    public void FullDefaultProfile_ShouldComposeAndRun_WithExpectedRuntimeCapabilities()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(GetDialectPath("full-default"));
        using var host = workflow.CreateHost(composition);

        var compilerValue = host.Run(File.ReadAllText(GetProgramPath("full-default")), "compiler");
        var interpreterValue = host.Run(File.ReadAllText(GetProgramPath("full-default")), "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True);
            Assert.That(composition.RuntimeComposition, Is.Not.Null);
            Assert.That(composition.RuntimeComposition!.EnabledBackends.Select(static x => x.CanonicalId), Does.Contain("cil"));
            Assert.That(composition.RuntimeComposition.EnabledBackends.Select(static x => x.CanonicalId), Does.Contain("interpreter"));
            Assert.That(composition.RuntimeComposition.EnabledOptimizers.Select(static x => x.ImplementationType.Name), Does.Contain("LocalVariablesOptimizer"));
            Assert.That(interpreterValue, Is.EqualTo(compilerValue));
        });
    }

    [Test]
    public void MinimalArithmeticProfile_ShouldRejectVariableSyntaxOutsideEnabledModules()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(GetDialectPath("minimal-arithmetic"));
        using var host = workflow.CreateHost(composition);

        var ex = Assert.Catch(() => host.Run("let x = 1\nx", "interpreter"));

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True);
            Assert.That(composition.RuntimeComposition, Is.Not.Null);
            Assert.That(composition.RuntimeComposition!.EnabledBackends.Select(static x => x.CanonicalId), Is.EqualTo(new[] { "interpreter" }));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex!.Message, Does.Contain("token").IgnoreCase);
        });
    }

    [Test]
    public void RestrictedSandboxProfile_ShouldRejectIdentifierBasedProgram_WhenIdentifiersAreExcluded()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(GetDialectPath("restricted-sandbox"));
        using var host = workflow.CreateHost(composition);

        var ex = Assert.Catch(() => host.Run(File.ReadAllText(GetForbiddenProgramPath("restricted-sandbox")), "interpreter"));

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True);
            Assert.That(composition.RuntimeComposition, Is.Not.Null);
            Assert.That(composition.RuntimeComposition!.EnabledBackends.Select(static x => x.CanonicalId), Is.EqualTo(new[] { "interpreter" }));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex!.Message, Does.Contain("token").Or.Contain("Variable").Or.Contain("Identifier").IgnoreCase);
        });
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
