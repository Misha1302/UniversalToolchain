using ArithmeticModule;
using BasicCore;
using CSharpInteropModule;
using EqualityModule;
using IdentifierModule;
using NumbersModule;
using ScopesModule;
using VariablesModule;
using WhitespacesModule;

namespace Tests;

[TestFixture]
public class ModuleCombinationsTests : TestBase
{
    [Test]
    public void Execute_AllCoreModulesTogether_WorksCorrectly()
    {
        // Arrange
        var code = @"
                let x = 10
                let y = (x + 5) * 2
                y = y - 3
                y / 2
            ";
        var modules = new ICoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new CSharpInteropModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(13.5).Within(1e-9));
    }

    [Test]
    public void Execute_MixedOperationsWithDifferentPrecedence_RespectsOrder()
    {
        // Arrange
        var code = @"
                let a = 2 + 3 * 4
                let b = (2 + 3) * 4
                let c = a + b * 2
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
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(54).Within(1e-9));
    }
}