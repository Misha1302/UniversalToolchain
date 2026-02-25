using NumbersModule.Core;

namespace Tests.Arithmetic;

[TestFixture]
public class AdvancedArithmeticTests : TestBase
{
    [Test]
    public void Execute_FloatingPointPrecision_HandlesDecimalsCorrectly()
    {
        var code = @"
                let a = 0.1
                let b = 0.2
                let c = 0.3
                (a + b) * 10 - c * 10
            ";


        var result = ExecuteCode(code);


        // (0.1 + 0.2) * 10 - 0.3 * 10 = 0.3 * 10 - 3 = 3 - 3 = 0
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(0).Within(1e-9));
    }

    [Test]
    public void Execute_LargeNumberOperations_HandlesCorrectly()
    {
        var code = @"
                let big = 1000000
                let veryBig = big * big
                let huge = veryBig / big * 2
                huge - big
            ";


        var result = ExecuteCode(code);


        // 1000000 * 1000000 = 1000000000000
        // 1000000000000 / 1000000 * 2 = 1000000 * 2 = 2000000
        // 2000000 - 1000000 = 1000000
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(1000000).Within(1e-9));
    }

    [Test]
    public void Execute_ComplexExpressionWithAllDataTypes_WorksCorrectly()
    {
        var code = @"
                let intVal = 10
                let decimalVal = 2.5
                let negativeVal = -3
                let zero = 0
                
                (intVal * decimalVal + negativeVal * 2) / (decimalVal - 1) + zero
            ";


        var result = ExecuteCode(code);


        // (10*2.5 + (-3)*2) / (2.5-1) + 0 = (25 - 6) / 1.5 = 19 / 1.5 = 12.666...
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(19.0 / 1.5).Within(1e-9));
    }
}