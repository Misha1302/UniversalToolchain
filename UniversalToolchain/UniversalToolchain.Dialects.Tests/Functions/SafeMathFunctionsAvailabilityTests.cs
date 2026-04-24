using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Features;
using UniversalToolchain.Features.Abstractions;
using UniversalToolchain.Features.Core;

namespace UniversalToolchain.Dialects.Tests.Functions;

[TestFixture]
public sealed class SafeMathFunctionsAvailabilityTests
{
    private const string SafeMathDialect = """
                                           dialect SafeMath
                                           use Identifier,NativeTypes,SafeMathFunctions,Scopes,Variables,Whitespaces
                                           backend compiler,interpreter
                                           """;

    private const string WithoutSafeMathDialect = """
                                                  dialect NoSafeMath
                                                  use Identifier,NativeTypes,Scopes,Variables,Whitespaces
                                                  backend compiler,interpreter
                                                  """;

    [Test]
    public void SafeMath_Clamp_NotAvailableWithoutSafeMathModule_ReturnsDiagnostic()
    {
        using var host = CreateHost(WithoutSafeMathDialect);

        var exception = Assert.Throws<Exception>(() => host.Run("clamp(10.0, 0.0, 5.0)", "interpreter"));

        Assert.That(exception!.Message, Does.Contain("WST-FUNC-002").And.Contain("unavailable"));
    }

    [Test]
    public void SafeMath_FunctionCall_DoesNotRequireCSharpInterop()
    {
        using var host = CreateHost(SafeMathDialect);

        var compiler = Convert.ToDouble(host.Run("abs(-1.0)", "compiler"));
        var interpreter = Convert.ToDouble(host.Run("abs(-1.0)", "interpreter"));

        Assert.Multiple(() =>
        {
            Assert.That(compiler, Is.EqualTo(1.0d).Within(1e-9));
            Assert.That(interpreter, Is.EqualTo(1.0d).Within(1e-9));
        });
    }

    [Test]
    public void SafeMath_FeatureProjection_ReportsSafeMathWhenAliasSelected()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(SafeMathDialect, "safe-math-inline");

        Assert.That(composition.IsSuccess, Is.True, DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        ILanguageFeatureCatalog catalog = new WistLanguageFeatureCatalog();
        var explanation = new DialectFeatureExplanationProjector(catalog).Project(composition);

        Assert.Multiple(() =>
        {
            Assert.That(explanation.AvailableFeatures.Select(static x => x.Descriptor.FeatureId.Value), Does.Contain(WistLanguageFeatureIds.SafeMathFunctions.Value));
            Assert.That(explanation.UnavailableFeatures.Select(static x => x.Descriptor.FeatureId.Value), Does.Not.Contain(WistLanguageFeatureIds.SafeMathFunctions.Value));
        });
    }

    private static WistDialectExecutionHost CreateHost(string dialect)
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(dialect, "safe-math-inline");

        Assert.That(composition.IsSuccess, Is.True, DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        return workflow.CreateHost(composition);
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
