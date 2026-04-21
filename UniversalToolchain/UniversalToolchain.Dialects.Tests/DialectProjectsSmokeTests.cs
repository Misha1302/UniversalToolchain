using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Tests.Wist;
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
            "full-default-native",
            "minimal-arithmetic",
            "minimal-arithmetic-native",
            "restricted-sandbox",
            "pricing-restricted"
        }));

        foreach (var exampleDirectory in exampleDirectories)
        {
            var exampleName = Path.GetFileName(exampleDirectory) ?? string.Empty;
            var dialectPath = Path.Combine(exampleDirectory, "dialect.wistdialect");
            var composition = workflow.ComposeFile(dialectPath);
            Assert.That(composition.IsSuccess, Is.True, $"Composition failed for '{dialectPath}'.\n{UniversalToolchain.Dialects.Integration.DialectCompositionExplanationFormatter.FormatDeterministic(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationProjector.Project(composition))}");

            var selectionSignature = WistDialectTestInfrastructure.BuildSelectionSignature(composition);
            Assert.That(selectionSignature, Is.EqualTo(ExpectedSelectionSignatures[exampleName]), $"Runtime selection drifted for example '{exampleName}'.");

            using var host = workflow.CreateHost(composition);
            var programPath = Path.Combine(exampleDirectory, "program.wist");
            var result = host.Run(
                File.ReadAllText(programPath),
                host.Configuration.EnabledBackends.Any(x => x.Name == "interpreter")
                    ? "interpreter"
                    : "cil"
            );

            Assert.That(result, Is.Not.Null, $"Example '{exampleName}' returned null.");
        }
    }

    private static readonly IReadOnlyDictionary<string, string> ExpectedSelectionSignatures = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["full-default"] =
            "Arithmetic|BooleanConditions|Comments|ComparisonConditions|Conditions|CSharpInterop|Equality|Identifier|Labels|Loops|Numbers|Scopes|SemicolonAsNewLine|Variables|Whitespaces"
            + "::BooleanOptimization|ComparisonIntrinsicOptimization|LocalVariablesOptimization"
            + "::cil|interpreter",
        ["full-default-native"] =
            "BooleanConditions|Comments|ComparisonConditions|Conditions|CSharpInterop|Equality|Identifier|Labels|Loops|NativeTypes|Scopes|SemicolonAsNewLine|Variables|Whitespaces"
            + "::ArithmeticOptimization|BooleanOptimization|ComparisonIntrinsicOptimization|EGraphOptimization|LocalVariablesOptimization|NativeCilOptimization|NativeTypesOptimization"
            + "::cil|interpreter",
        ["minimal-arithmetic"] =
            "Arithmetic|Numbers|Scopes|Whitespaces"
            + "::"
            + "::interpreter",
        ["minimal-arithmetic-native"] =
            "NativeTypes|Numbers|Scopes|Whitespaces"
            + "::ArithmeticOptimization|EGraphOptimization|NativeCilOptimization|NativeTypesOptimization"
            + "::cil",
        ["pricing-restricted"] =
            "Identifier|NativeTypes|Scopes|Variables|Whitespaces"
            + "::ArithmeticOptimization|EGraphOptimization|NativeCilOptimization|NativeTypesOptimization"
            + "::cil|interpreter",
        ["restricted-sandbox"] =
            "Arithmetic|BooleanConditions|Comments|ComparisonConditions|Conditions|Equality|Numbers|Scopes|Whitespaces"
            + "::"
            + "::interpreter"
    };

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
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist"));
        if (!Directory.Exists(root))
            Thrower.FileNotFound(root);

        return Directory.EnumerateDirectories(root)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToList();
    }
}
