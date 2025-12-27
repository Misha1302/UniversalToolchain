namespace Tests;

[TestFixture]
public class ComplexArithmeticTests : TestBase
{
    [Test]
    public void Execute_DeeplyNestedParentheses_ComputesCorrectly()
    {
        // Arrange
        var code = "((((2 + 3) * (4 - 1)) + ((5 + 1) * 2)) - 10) / 2";

        // Act
        var result = ExecuteCode(code);

        // Assert
        // ((((5 * 3) + (6 * 2)) - 10) / 2) = ((15 + 12 - 10) / 2) = (17 / 2) = 8.5
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(8.5).Within(1e-9));
    }

    [Test]
    public void Execute_MixedOperationsWithAllOperators_RespectsPrecedence()
    {
        // Arrange
        var code = "10 + 2 * 3 - 8 / 4 + 5 * (6 - 2) / 2";

        // Act
        var result = ExecuteCode(code);

        // Assert
        // Expected: 10 + (2*3) - (8/4) + ((5*(6-2))/2) = 10 + 6 - 2 + (5*4/2) = 10 + 6 - 2 + 10 = 24
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(24).Within(1e-9));
    }

    [Test]
    public void Execute_ComplexExpressionWithMultipleVariables_WorksCorrectly()
    {
        // Arrange
        var code = @"
                let a = 5
                let b = 3
                let c = 2
                let d = 4
                (a * b + c * d) / (a - b) + (c + d) * (b - a)
            ";

        // Act
        var result = ExecuteCode(code);

        // Assert
        // (5*3 + 2*4)/(5-3) + (2+4)*(3-5) = (15+8)/2 + 6*(-2) = 23/2 - 12 = 11.5 - 12 = -0.5
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(-0.5).Within(1e-9));
    }

    [Test]
    public void Execute_ExpressionWithNegativeNumbers_HandlesCorrectly()
    {
        // Arrange
        var code = @"
                let a = -5
                let b = 3
                let c = -2
                a * b + c * (a - b) - (c + a) / b
            ";

        // Act
        var result = ExecuteCode(code);

        // Assert
        // (-5)*3 + (-2)*(-5-3) - (-2-5)/3 = -15 + (-2)*(-8) - (-7)/3 = -15 + 16 + 7/3 = 1 + 2.333... = 3.333...
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(1 + 7.0 / 3).Within(1e-9));
    }

    [Test]
    public void Execute_MultiLevelNestedExpressions_ComputesCorrectly()
    {
        // Arrange
        var code = @"
                let result = (2 + (3 * (4 - (1 + 1)))) * ((5 + 1) / (2 + 1))
                result
            ";

        // Act
        var result = ExecuteCode(code);

        // Assert
        // (2 + (3 * (4 - 2))) * (6 / 3) = (2 + (3*2)) * 2 = (2+6)*2 = 8*2 = 16
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(16).Within(1e-9));
    }
}