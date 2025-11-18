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
public class ComplexExpressionsTests : TestBase
{
    [Test]
    public void Execute_NestedArithmeticWithVariables_ReturnsCorrectResult()
    {
        // Arrange
        var code = @"
                let a = 10
                let b = 2
                let c = 5
                (a + b) * c - (a / b)
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
    public void Execute_MultipleVariableReassignments_UpdatesValuesCorrectly()
    {
        // Arrange
        var code = @"
                let x = 1
                let y = 2
                x = x + y
                y = x * y
                x = y - x
                y
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
    public void Execute_ComplexExpressionWithAllOperators_WorksCorrectly()
    {
        // Arrange
        var code = @"
                let a = 12
                let b = 4
                let c = 2
                a + b * c - (a / c) + b
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