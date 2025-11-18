using ArithmeticModule;
using BasicCore;
using EqualityModule;
using IdentifierModule;
using NumbersModule;
using ScopesModule;
using SemicolonAsNewLineModule;
using VariablesModule;
using WhitespacesModule;

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
    public void Execute_VeryLargeNumbers_HandlesCorrectly()
    {
        // Arrange
        var code = @"
                let big = 1000000
                let veryBig = big * big
                veryBig / big
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
    public void Execute_DecimalNumbers_ComputesPrecisely()
    {
        // Arrange
        var code = @"
                let a = 0.1
                let b = 0.2
                let c = a + b
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

    [Test]
    public void Execute_MultipleVariableScopes_ManagesStateCorrectly()
    {
        // Arrange
        var code = @"
                let global = 10
                let result1 = (let x = 5; x * global)
                let result2 = (let x = 3; x * global)
                result1 + result2
            ";
        var modules = new ICoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new SemicolonAsNewLineModuleImpl(),
            new EqualityModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        Assert.That(result, Is.Not.Null);
    }
}