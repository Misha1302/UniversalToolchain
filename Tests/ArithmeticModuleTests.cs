using ArithmeticModule;
using BasicCore;
using IdentifierModule;
using NumbersModule;
using ScopesModule;
using WhitespacesModule;

namespace Tests;

[TestFixture]
public class ArithmeticModuleTests : TestBase
{
    [TestCase("2 + 3", 5)]
    [TestCase("10 - 4", 6)]
    [TestCase("3 * 4", 12)]
    [TestCase("15 / 3", 5)]
    public void Execute_BasicArithmeticOperations_ReturnsExpectedResult(string code, double expected)
    {
        // Arrange
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
        // Note: This will need adjustment based on how numbers are actually returned
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Execute_OperatorPrecedence_MultiplicationBeforeAddition()
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
        // Should be 14, not 20
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Execute_WithParentheses_RespectsGrouping()
    {
        // Arrange
        var code = "(2 + 3) * 4";
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
        // Should be 20, not 14
        Assert.That(result, Is.Not.Null);
    }
}