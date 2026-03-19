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
        var exampleDirectories = ResolveExampleDirectories();

        Assert.That(exampleDirectories.Select(Path.GetFileName), Is.EquivalentTo(new[]
        {
            "full-default",
            "minimal-arithmetic",
            "restricted-sandbox"
        }));

        foreach (var exampleDirectory in exampleDirectories)
        {
            var dialectPath = Path.Combine(exampleDirectory, "dialect.wistdialect");
            var composition = workflow.ComposeFile(dialectPath);
            Assert.That(composition.IsSuccess, Is.True, $"Composition failed for '{dialectPath}'.\n{composition.ToDeterministicText()}");

            using var host = workflow.CreateHost(composition);
            var exampleName = Path.GetFileName(exampleDirectory);
            var programPath = Path.Combine(exampleDirectory, "program.wist");
            var result = host.Run(File.ReadAllText(programPath), "interpreter");

            Assert.That(result, Is.Not.Null, $"Example '{exampleName}' returned null.");
        }
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        return services.BuildServiceProvider();
    }

    private static IReadOnlyList<string> ResolveExampleDirectories()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist"));
        if (!Directory.Exists(root))
        {
            Thrower.FileNotFound(root);
        }

        return Directory.EnumerateDirectories(root)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToList();
    }
}
