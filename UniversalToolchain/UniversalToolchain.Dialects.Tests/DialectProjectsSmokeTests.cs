using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class DialectProjectsSmokeTests
{
    [Test]
    public void ExampleProjects_AreEnumerated_Composed_AndExecutedEndToEnd()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var exampleDirectory = ResolveExampleDirectory("minimal-arithmetic");
        var dialectPath = Path.Combine(exampleDirectory, "dialect.wistdialect");
        var composition = workflow.ComposeFile(dialectPath);

        Assert.That(composition.IsSuccess, Is.True, $"Composition failed for '{dialectPath}'.\n{composition.ToDeterministicText()}");

        using var host = workflow.CreateHost(composition);
        var programPath = Path.Combine(exampleDirectory, "program.wist");
        var result = host.Run(File.ReadAllText(programPath), "interpreter");

        Assert.That(result, Is.Not.Null, "Example 'minimal-arithmetic' returned null.");
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        return services.BuildServiceProvider();
    }

    private static string ResolveExampleDirectory(string name)
    {
        var path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", name));
        if (!Directory.Exists(path))
            Thrower.FileNotFound(path);

        return path;
    }
}
