using ArithmeticModule;
using BasicCore;
using ConditionsModule;
using EqualityModule;
using IdentifierModule;
using LabelsModule;
using NumbersModule;
using ScopesModule;
using VariablesModule;
using WhitespacesModule;

namespace Tests;

[TestFixture]
public class LoopTests : TestBase
{
    [Test]
    public void Execute_SimpleLoopWithLabels_ComputesSumCorrectly()
    {
        // Arrange
        var code = @"
                let sum = 0
                let i = 1
                
                @start:
                if i > 10 goto @end
                    sum = sum + i
                    i = i + 1
                    goto @start
                @end:
                sum
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new LabelsModuleImpl(),
            new ConditionsModuleImpl(),
            new ComparisonOperations()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        // Sum of 1 to 10 = 55
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(55).Within(1e-9));
    }

    [Test]
    public void Execute_NestedLoops_ComputesMultiplicationTable()
    {
        // Arrange
        var code = @"
                let result = 0
                let i = 1
                
                @outer_loop:
                if i > 3 goto @outer_end
                    let j = 1
                    
                    @inner_loop:
                    if j > 3 goto @inner_end
                        result = result + (i * j)
                        j = j + 1
                        goto @inner_loop
                    @inner_end:
                    
                    i = i + 1
                    goto @outer_loop
                @outer_end:
                result
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new LabelsModuleImpl(),
            new ConditionsModuleImpl(),
            new ComparisonOperations()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        // Sum of multiplication table 3x3: 
        // 1*1 + 1*2 + 1*3 + 2*1 + 2*2 + 2*3 + 3*1 + 3*2 + 3*3 = 36
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(36).Within(1e-9));
    }

    [Test]
    public void Execute_ConditionalLoopBreak_StopsWhenConditionMet()
    {
        // Arrange
        var code = @"
                let counter = 0
                let total = 0
                
                @loop:
                if counter >= 10 goto @end
                    counter = counter + 1
                    if counter > 5 goto @skip
                        total = total + counter
                    @skip:
                    goto @loop
                @end:
                total
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new LabelsModuleImpl(),
            new ConditionsModuleImpl(),
            new ComparisonOperations()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        // Sum of numbers 1 to 5 = 15
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(15).Within(1e-9));
    }
}