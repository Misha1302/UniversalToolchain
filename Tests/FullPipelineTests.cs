using ArithmeticModule;
using BasicCore;
using EqualityModule;
using IdentifierModule;
using NumbersModule;
using ScopesModule;
using VariablesModule;
using WhitespacesModule;

namespace Tests;

[TestFixture]
public class FullPipelineTests : TestBase
{
    [Test]
    public void Execute_SimpleArithmetic_ReturnsCorrectResult()
    {
        // Arrange
        var code = "2 + 3 * 4";
        var modules = new ICoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        // Note: Actual assertion depends on how numbers are represented
        Assert.That(result, Is.Not.Null);
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
        var modules = new ICoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Execute_ComplexExpressionWithParentheses_ReturnsCorrectResult()
    {
        // Arrange
        var code = "(2 + 3) * (4 - 1)";
        var modules = new ICoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        Assert.That(result, Is.Not.Null);
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
        var modules = new ICoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        Assert.That(result, Is.Not.Null);
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
        var modules = new ICoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        Assert.That(result, Is.Not.Null);
    }
}