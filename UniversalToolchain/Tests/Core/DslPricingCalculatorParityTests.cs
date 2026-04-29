using Example.Scenarios;

namespace Tests.Core;

[TestFixture]
public class DslPricingCalculatorParityTests
{
    [Test]
    public void DslPricingCalculator_FastNativePointer_MatchesCompilerAndInterpreter()
    {
        using var calculator = new DslPricingCalculator();

        var compiler = calculator.CalculateWithCompiler("price * 0.9 + fee", 100.0, 5.0);
        var interpreter = calculator.CalculateWithInterpreter("price * 0.9 + fee", 100.0, 5.0);
        var fastNativePointer = calculator.CalculateWithFastInvoker("price * 0.9 + fee", 100.0, 5.0);

        Assert.That(fastNativePointer, Is.EqualTo(compiler));
        Assert.That(fastNativePointer, Is.EqualTo(interpreter));
    }
}
