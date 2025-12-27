namespace Tests;

[TestFixture]
public class ArithmeticModuleTests : TestBase
{
    [TestCase("2 + 3", 5)]
    [TestCase("10 - 4", 6)]
    [TestCase("3 * 4", 12)]
    [TestCase("15 / 3", 5)]
    public void Execute_BasicArithmeticOperations_ReturnsExpectedResult(string code, double expected)
    {
        var result = ExecuteCode(code);


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(expected).Within(1e-9));
    }

    [Test]
    public void Execute_OperatorPrecedence_MultiplicationBeforeAddition()
    {
        var code = "2 + 3 * 4";


        var result = ExecuteCode(code);


        // Should be 14, not 20
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(14).Within(1e-9));
    }

    [Test]
    public void Execute_WithParentheses_RespectsGrouping()
    {
        var code = "(2 + 3) * 4";


        var result = ExecuteCode(code);


        // Should be 20, not 14
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(20).Within(1e-9));
    }
}