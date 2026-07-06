using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Tests.Wist;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class DialectProjectsSmokeTests
{
    private static readonly string[] _expectedExamples =
    [
        "full-default",
        "full-default-native",
        "function-calls-safe-math",
        "minimal-arithmetic",
        "minimal-arithmetic-grouped",
        "minimal-arithmetic-native",
        "pricing-restricted",
        "restricted-sandbox"
    ];

    [Test]
    public void ExampleProjects_AreEnumerated_Composed_AndExecutedEndToEnd()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var exampleDirectories = ResolveExampleDirectories();

        Assert.That(exampleDirectories.Select(Path.GetFileName), Is.EquivalentTo(_expectedExamples));

        foreach (var exampleDirectory in exampleDirectories)
        {
            var dialectPath = Path.Combine(exampleDirectory, "dialect.wistdialect");
            var composition = workflow.ComposeFile(dialectPath);
            Assert.That(composition.IsSuccess, Is.True, $"Composition failed for '{dialectPath}'.\n{DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition))}");

            var selectionSignature = WistDialectTestInfrastructure.BuildSelectionSignature(composition);
            Assert.That(selectionSignature, Is.Not.Empty, $"Runtime selection is empty for '{Path.GetFileName(exampleDirectory)}'.");

            var programPath = Path.Combine(exampleDirectory, "program.wist");
            if (!File.Exists(programPath))
                continue;

            using var host = workflow.CreateHost(composition);
            var result = host.Run(
                File.ReadAllText(programPath),
                host.Configuration.EnabledBackends.Any(x => x.Name == "interpreter")
                    ? "interpreter"
                    : "cil");

            Assert.That(result, Is.Not.Null, $"Example '{Path.GetFileName(exampleDirectory)}' returned null.");
        }
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }

    private static IReadOnlyList<string> ResolveExampleDirectories()
    {
        var root = TestSourcePaths.WistExamplesRoot;

        return Directory.EnumerateDirectories(root)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToList();
    }
}