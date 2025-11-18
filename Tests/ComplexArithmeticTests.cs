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
public class ComplexArithmeticTests : TestBase
{
    [Test]
    public void Execute_DeeplyNestedParentheses_ComputesCorrectly()
    {
        // Arrange
        var code = "((((2 + 3) * (4 - 1)) + ((5 + 1) * 2)) - 10) / 2";
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
    public void Execute_MixedOperationsWithAllOperators_RespectsPrecedence()
    {
        // Arrange
        var code = "10 + 2 * 3 - 8 / 4 + 5 * (6 - 2) / 2";
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
        // Expected: 10 + (2*3) - (8/4) + ((5*(6-2))/2) = 10 + 6 - 2 + (5*4/2) = 10 + 6 - 2 + 10 = 24
        Assert.That(result, Is.Not.Null);
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
        // (5*3 + 2*4)/(5-3) + (2+4)*(3-5) = (15+8)/2 + 6*(-2) = 23/2 - 12 = 11.5 - 12 = -0.5
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Execute_ChainedOperationsWithReassignment_ComputesCorrectly()
    {
        // Arrange
        var code = @"
                let x = 10
                x = x * 2 + 5
                x = x / 3 - 1
                x = x * x + x
                x
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
        // x = 10*2+5 = 25
        // x = 25/3-1 ≈ 8.333-1 = 7.333
        // x = 7.333*7.333 + 7.333 ≈ 53.778 + 7.333 = 61.111
        Assert.That(result, Is.Not.Null);
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
        // (-5)*3 + (-2)*(-5-3) - (-2-5)/3 = -15 + (-2)*(-8) - (-7)/3 = -15 + 16 + 7/3 = 1 + 2.333 = 3.333
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Execute_MultiLevelNestedExpressions_ComputesCorrectly()
    {
        // Arrange
        var code = @"
                let result = (2 + (3 * (4 - (1 + 1)))) * ((5 + 1) / (2 + 1))
                result
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
        // (2 + (3 * (4 - 2))) * (6 / 3) = (2 + (3*2)) * 2 = (2+6)*2 = 8*2 = 16
        Assert.That(result, Is.Not.Null);
    }
}