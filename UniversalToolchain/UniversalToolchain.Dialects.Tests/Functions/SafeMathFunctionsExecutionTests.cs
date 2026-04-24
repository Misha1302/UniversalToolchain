using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Features.Core;

namespace UniversalToolchain.Dialects.Tests.Functions;

[TestFixture]
public sealed class SafeMathFunctionsExecutionTests
{
    private const string SafeMathDialect = """
                                           dialect SafeMath
                                           use Identifier,NativeTypes,SafeMathFunctions,Scopes,Variables,Whitespaces
                                           backend compiler,interpreter
                                           """;

    [Test]
    public void SafeMath_Min_CompilerAndInterpreter_ReturnSameResult()
    {
        using var host = CreateHost();

        AssertParity(host, "min(10.0, 5.0)", 5.0d);
    }

    [Test]
    public void SafeMath_Max_CompilerAndInterpreter_ReturnSameResult()
    {
        using var host = CreateHost();

        AssertParity(host, "max(10.0, 5.0)", 10.0d);
    }

    [Test]
    public void SafeMath_Abs_CompilerAndInterpreter_ReturnSameResult()
    {
        using var host = CreateHost();

        AssertParity(host, "abs(-7.5)", 7.5d);
    }

    [Test]
    public void SafeMath_Clamp_CompilerAndInterpreter_ReturnSameResult()
    {
        using var host = CreateHost();

        AssertParity(host, "clamp(10.0, 0.0, 5.0)", 5.0d);
    }

    private static void AssertParity(WistDialectExecutionHost host, string program, double expected)
    {
        var compiler = ToDouble(host.Run(program, "compiler"));
        var interpreter = ToDouble(host.Run(program, "interpreter"));

        Assert.Multiple(() =>
        {
            Assert.That(compiler, Is.EqualTo(expected).Within(1e-9));
            Assert.That(interpreter, Is.EqualTo(expected).Within(1e-9));
            Assert.That(interpreter, Is.EqualTo(compiler).Within(1e-9));
        });
    }

    private static WistDialectExecutionHost CreateHost()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(SafeMathDialect, "safe-math-inline");

        Assert.That(composition.IsSuccess, Is.True, DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        return workflow.CreateHost(composition);
    }

    private static double ToDouble(object? value)
    {
        return value switch
        {
            RealNumberImpl number => number.GetValue(),
            int intValue => intValue,
            long longValue => longValue,
            double doubleValue => doubleValue,
            float floatValue => floatValue,
            decimal decimalValue => (double)decimalValue,
            _ => Convert.ToDouble(value, CultureInfo.InvariantCulture)
        };
    }
}
