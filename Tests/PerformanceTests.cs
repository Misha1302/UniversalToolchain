using System.Diagnostics;
using ArithmeticModule;
using BasicCore;
using ConditionsModule;
using EqualityModule;
using IdentifierModule;
using NumbersModule;
using ScopesModule;
using VariablesModule;
using WhitespacesModule;

namespace Tests;

[TestFixture]
public class PerformanceTests : TestBase
{
    [Test]
    public void Execute_ManySimpleOperations_PerformsWithinReasonableTime()
    {
        // Arrange
        var code = @"
                let result = 0
                let i = 0
                while i < 100
                    result = result + i
                    i = i + 1
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

        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var result = ExecuteCode(code, modules);
        stopwatch.Stop();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(
            stopwatch.ElapsedMilliseconds / Executors.Count,
            Is.LessThan(1000)
        );
    }

    [Test]
    public void Execute_ComplexNestedExpressions_PerformsWell()
    {
        // Arrange
        var code = @"
                let a = 1 + 2 * 3 - 4 / 2 + (5 * (6 - 2)) / 4
                let b = a * 2 - a / 2 + (a + 1) * 3
                let c = b * a - b / a + (a + b) * 2
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

        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var result = ExecuteCode(code, modules);
        stopwatch.Stop();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(
            stopwatch.ElapsedMilliseconds / Executors.Count,
            Is.LessThan(500)
        );
    }
}