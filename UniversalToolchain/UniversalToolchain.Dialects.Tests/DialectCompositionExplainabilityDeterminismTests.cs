using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class DialectCompositionExplainabilityDeterminismTests
{
    [Test]
    public void ComposeText_ProjectAndFormat_RepeatedRuns_ProduceStableOutput()
    {
        var signatures = new List<string>();

        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        for (var i = 0; i < 40; i++)
        {
            var result = workflow.ComposeText("dialect Stable\nuse Arithmetic,Numbers,Whitespaces\nbackend interpreter,cil", "stable");
            Assert.That(result.IsSuccess, Is.True);

            var explanation = DialectCompositionExplanationProjector.Project(result);
            var text = DialectCompositionExplanationFormatter.FormatDeterministic(explanation);
            signatures.Add(text);
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }
}