// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Diagnostics;
using ArithmeticModule;
using BasicCore;
using ConditionsModule;
using CSharpInteropModule;
using EqualityModule;
using IdentifierModule;
using LabelsModule;
using NumbersModule;
using ScopesModule;
using SemicolonAsNewLineModule;
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
            @start:
                result = result + i
                i = i + 1
                if i < 100
                    goto @start
            result
            ";
        var modules = new ICoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new SemicolonAsNewLineModuleImpl(),
            new ArithmeticModuleImpl(),
            new CSharpInteropModuleImpl(),
            new LabelsModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new ConditionsModuleImpl(),
            new ComparisonOperations(),
            new BooleanOperations(),
        };

        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var result = ExecuteCode(code, modules);
        stopwatch.Stop();

        // Assert
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(4950).Within(1e-9));
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
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(591.2).Within(1e-9));
        Assert.That(
            stopwatch.ElapsedMilliseconds / Executors.Count,
            Is.LessThan(500)
        );
    }
}