namespace Tests;

[TestFixture]
public class EdgeCasesTests : TestBase
{
    [Test]
    public void Execute_ZeroValues_HandlesCorrectly()
    {
        // Arrange
        var code = @"
                let zero = 0
                let result = zero * 100 + zero / 1
                result
            ";

        // Act
        var result = ExecuteCode(code);

        // Assert
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(0).Within(1e-9));
    }

    [Test]
    public void Execute_VeryLargeNumbers_HandlesCorrectly()
    {
        // Arrange
        var code = @"
                let big = 1000000
                let veryBig = big * big
                veryBig / big
            ";

        // Act
        var result = ExecuteCode(code);

        // Assert
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(1000000).Within(1e-9));
    }

    [Test]
    public void Execute_DecimalNumbers_ComputesPrecisely()
    {
        // Arrange
        var code = @"
                let a = 0.1
                let b = 0.2
                let c = a + b
                c
            ";

        // Act
        var result = ExecuteCode(code);

        // Assert
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(0.3).Within(1e-9));
    }
}