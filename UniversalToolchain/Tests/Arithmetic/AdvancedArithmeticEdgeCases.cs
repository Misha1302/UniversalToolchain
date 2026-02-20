namespace Tests;

[TestFixture]
public class AdvancedArithmeticEdgeCases : TestBase
{
    [Test]
    public void Execute_VerySmallNumbersPrecision_HandlesTinyValues()
    {
        var code = @"
                let a = 1e-15
                let b = 2e-15
                let c = 3e-15
                (a + b) * 1e15 - c * 1e15
            ";


        var result = ExecuteCode(code);


        // (1e-15 + 2e-15) * 1e15 - 3e-15 * 1e15 = (3e-15)*1e15 - 3e-15*1e15 = 3 - 3 = 0
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(0).Within(1e-12));
    }

    [Test]
    public void Execute_LargeExponentOperations_HandlesScientificNotation()
    {
        var code = @"
                let avogadro = 6.022e23
                let moles = 2.5
                let molecules = avogadro * moles
                molecules / avogadro
            ";


        var result = ExecuteCode(code);


        // Should return 2.5
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(2.5).Within(1e-9));
    }

    [Test]
    public void Execute_ComplexPolynomial_ComputesHigherOrderEquation()
    {
        var code = @"
                let x = 2.5
                let result = 3*x*x*x - 2*x*x + 5*x - 7
                result
            ";


        var result = ExecuteCode(code);


        // 3*(2.5)^3 - 2*(2.5)^2 + 5*2.5 - 7 = 46.875 - 12.5 + 12.5 - 7 = 39.375
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(39.875).Within(1e-9));
    }
}