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
    public void MinimalArithmeticProfile_ShouldComposeWithInterpreterOnly_AndExpectedModuleShape()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(GetDialectPath("minimal-arithmetic"));
        using var host = workflow.CreateHost(composition);

        var moduleTypes = composition.RuntimeComposition!.OrderedModules.Select(static x => x.ImplementationType.Name).ToArray();
        var backendIds = composition.RuntimeComposition.EnabledBackends.Select(static x => x.CanonicalId).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True);
            Assert.That(composition.RuntimeComposition, Is.Not.Null);
            Assert.That(backendIds, Is.EqualTo(new[] { "interpreter" }));
            Assert.That(moduleTypes, Is.EquivalentTo(new[] { "ArithmeticModuleImpl", "NumbersModuleImpl", "ScopesModuleImpl", "WhitespaceModuleImpl" }));
            Assert.That(moduleTypes, Does.Not.Contain("IdentifierModuleImpl"));
            Assert.That(moduleTypes, Does.Not.Contain("VariablesModuleImpl"));
            Assert.That(Assert.Catch(() => host.Run("1 + 1", "compiler")), Is.Not.Null);
        });
    }

    [Test]
    public void RestrictedSandboxProfile_ShouldDisableCompilerAndInteropCapabilities()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(GetDialectPath("restricted-sandbox"));
        using var host = workflow.CreateHost(composition);

        var moduleTypes = composition.RuntimeComposition!.OrderedModules.Select(static x => x.ImplementationType.Name).ToArray();
        var backendIds = composition.RuntimeComposition.EnabledBackends.Select(static x => x.CanonicalId).ToArray();
        var compilerFailure = Assert.Catch(() => host.Run("1 + 1", "compiler"));

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True);
            Assert.That(composition.RuntimeComposition, Is.Not.Null);
            Assert.That(backendIds, Is.EqualTo(new[] { "interpreter" }));
            Assert.That(moduleTypes, Does.Not.Contain("CSharpInteropModuleImpl"));
            Assert.That(moduleTypes, Does.Not.Contain("IdentifierModuleImpl"));
            Assert.That(moduleTypes, Does.Not.Contain("VariablesModuleImpl"));
            Assert.That(compilerFailure, Is.Not.Null);
        });
    }

    private static string GetDialectPath(string dialectName)
        => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", dialectName, "dialect.wistdialect"));

    private static string GetProgramPath(string dialectName)
        => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", dialectName, "program.wist"));

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        return services.BuildServiceProvider();
    }
}
