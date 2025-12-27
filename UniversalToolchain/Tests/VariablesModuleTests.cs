namespace Tests;

[TestFixture]
public class VariablesModuleTests : TestBase
{
    [Test]
    public void Execute_VariableDeclaration_CanBeUsedInExpression()
    {
        // Arrange
        const string code =
            """
            let x = 5
            x * 2
            """;

        // Act
        var result = ExecuteCode(code);

        // Assert
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(10).Within(1e-9));
    }

    [Test]
    public void Execute_VariableReassignment_UpdatesValue()
    {
        // Arrange
        var code = """
                   let x = 5
                   x = 10
                   x
                   """;

        // Act
        var result = ExecuteCode(code);

        // Assert
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(10).Within(1e-9));
    }

    [Test]
    public void Execute_MultipleVariables_WorkIndependently()
    {
        // Arrange
        var code = """
                   let a = 5
                   let b = 3
                   a * b
                   """;

        // Act
        var result = ExecuteCode(code);

        // Assert
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(15).Within(1e-9));
    }
}