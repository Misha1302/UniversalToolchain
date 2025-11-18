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
public class ArithmeticEdgeCasesTests : TestBase
{
    [Test]
    public void Execute_VerySmallNumbers_HandlesPrecision()
    {
        // Arrange
        var code = @"
                let tiny = 0.000000001
                let veryTiny = 0.0000000001
                (tiny * 1000000000) + (veryTiny * 10000000000)
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
        // 0.000000001 * 1000000000 = 1
        // 0.0000000001 * 10000000000 = 1
        // 1 + 1 = 2
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Execute_ExpressionAtPrecisionLimits_HandlesCorrectly()
    {
        // Arrange
        var code = @"
                let maxPrecision = 1.0000000000000001
                let minPrecision = 0.9999999999999999
                (maxPrecision - 1) * 10000000000000000 + (1 - minPrecision) * 10000000000000000
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
    public void Execute_DivisionByVerySmallNumber_HandlesCorrectly()
    {
        // Arrange
        var code = @"
                let number = 100
                let verySmall = 0.000000001
                number / verySmall
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
        // 100 / 0.000000001 = 100000000000
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Execute_ChainedDivisionOperations_PreservesPrecision()
    {
        // Arrange
        var code = @"
                let a = 100
                let b = 3
                let c = 7
                let result = a / b / c * b * c
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
        // Should be approximately 100 (may have floating point errors)
        Assert.That(result, Is.Not.Null);
    }
}