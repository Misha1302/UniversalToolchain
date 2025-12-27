namespace Tests;

[TestFixture]
public class FullPipelineTests : TestBase
{
    [Test]
    public void Execute_SimpleArithmetic_ReturnsCorrectResult()
    {
        // Arrange
        var code = "2 + 3 * 4";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new ConditionsModuleImpl()
        };

        // Act
        var result = ExecuteCode(code);

        // Assert
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(14).Within(1e-9));
    }

    [Test]
    public void Execute_VariableAssignmentAndUsage_WorksCorrectly()
    {
        // Arrange
        var code = @"
                let x = 10
                let y = 20
                x + y
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new ConditionsModuleImpl()
        };

        // Act
        var result = ExecuteCode(code);

        // Assert
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(30).Within(1e-9));
    }

    [Test]
    public void Execute_ComplexExpressionWithParentheses_ReturnsCorrectResult()
    {
        // Arrange
        var code = "(2 + 3) * (4 - 1)";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new ConditionsModuleImpl()
        };

        // Act
        var result = ExecuteCode(code);

        // Assert
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(15).Within(1e-9));
    }

    [Test]
    public void Execute_MultipleOperationsWithVariables_WorksCorrectly()
    {
        // Arrange
        var code = @"
                let a = 5
                let b = 3
                let c = a * b + 2
                c - 1
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new ConditionsModuleImpl()
        };

        // Act
        var result = ExecuteCode(code);

        // Assert
        // c = 5*3 + 2 = 15 + 2 = 17
        // c - 1 = 16
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(16).Within(1e-9));
    }

    [Test]
    public void Execute_ExampleProgramFromDocumentation_CompletesSuccessfully()
    {
        // Arrange
        var code = @"
                let a = 10
                let b = 20
                let c = a * b - 5
                b = b + 1
                c = c - 15
                c
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new ConditionsModuleImpl()
        };

        // Act
        var result = ExecuteCode(code);

        // Assert
        // c = 10*20 - 5 = 200 - 5 = 195
        // b = 20 + 1 = 21
        // c = 195 - 15 = 180
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(180).Within(1e-9));
    }
}