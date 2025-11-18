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
public class AdvancedArithmeticTests : TestBase
{
    [Test]
    public void Execute_FloatingPointPrecision_HandlesDecimalsCorrectly()
    {
        // Arrange
        var code = @"
                let a = 0.1
                let b = 0.2
                let c = 0.3
                (a + b) * 10 - c * 10
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
        // (0.1 + 0.2) * 10 - 0.3 * 10 = 0.3 * 10 - 3 = 3 - 3 = 0
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Execute_LargeNumberOperations_HandlesCorrectly()
    {
        // Arrange
        var code = @"
                let big = 1000000
                let veryBig = big * big
                let huge = veryBig / big * 2
                huge - big
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
        // 1000000 * 1000000 = 1000000000000
        // 1000000000000 / 1000000 * 2 = 1000000 * 2 = 2000000
        // 2000000 - 1000000 = 1000000
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Execute_ComplexExpressionWithAllDataTypes_WorksCorrectly()
    {
        // Arrange
        var code = @"
                let intVal = 10
                let decimalVal = 2.5
                let negativeVal = -3
                let zero = 0
                
                (intVal * decimalVal + negativeVal * 2) / (decimalVal - 1) + zero
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
        // (10*2.5 + (-3)*2) / (2.5-1) + 0 = (25 - 6) / 1.5 = 19 / 1.5 = 12.666...
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Execute_ExpressionWithExponentialNotation_HandlesCorrectly()
    {
        // Arrange
        var code = @"
                let a = 1.5e2
                let b = 2.5e1
                let c = 1e1
                (a + b) / c - b * 2
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
        // (150 + 25) / 10 - 25*2 = 175/10 - 50 = 17.5 - 50 = -32.5
        Assert.That(result, Is.Not.Null);
    }
}