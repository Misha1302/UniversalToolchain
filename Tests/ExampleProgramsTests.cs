using ArithmeticModule;
using BasicCore;
using ConditionsModule;
using CSharpInteropModule;
using EqualityModule;
using IdentifierModule;
using NumbersModule;
using ScopesModule;
using VariablesModule;
using WhitespacesModule;

namespace Tests;

[TestFixture]
public class ExampleProgramsTests : TestBase
{
    [Test]
    public void Execute_CompleteExampleLikeInProgramCs_WorksCorrectly()
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
            new EqualityModuleImpl(),
            new CSharpInteropModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Execute_ExtendedExampleWithMoreOperations_CompletesSuccessfully()
    {
        // Arrange
        var code = @"
                let baseValue = 100
                let multiplier = 2.5
                let iterations = 4
                
                let result = baseValue
                let i = 0

                @start:
                    if i < iterations goto @end

                    result = result * multiplier - 10
                    i = i + 1
                @end:
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
            new ComparisonOperations(),
            new EqualityModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        Assert.That(result, Is.Not.Null);
    }
}