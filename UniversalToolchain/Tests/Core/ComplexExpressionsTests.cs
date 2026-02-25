using NumbersModule.Core;

namespace Tests.Core;

[TestFixture]
public class ComplexExpressionsTests : TestBase
{
    [Test]
    public void Execute_NestedArithmeticWithVariables_ReturnsCorrectResult()
    {
        var code = @"
                let a = 10
                let b = 2
                let c = 5
                (a + b) * c - (a / b)
            ";


        var result = ExecuteCode(code);


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(55).Within(1e-9));
    }

    [Test]
    public void Execute_MultipleVariableReassignments_UpdatesValuesCorrectly()
    {
        var code = @"
                let x = 1
                let y = 2
                x = x + y
                y = x * y
                x = y - x
                y
            ";


        var result = ExecuteCode(code);


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(6).Within(1e-9));
    }

    [Test]
    public void Execute_ComplexExpressionWithAllOperators_WorksCorrectly()
    {
        var code = @"
                let a = 12
                let b = 4
                let c = 2
                a + b * c - (a / c) + b
            ";


        var result = ExecuteCode(code);


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(18).Within(1e-9));
    }
}