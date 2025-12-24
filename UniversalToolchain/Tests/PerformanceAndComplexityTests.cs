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
public class PerformanceAndComplexityTests : TestBase
{
    [Test]
    public void Execute_ComplexNestedLoops_HandlesDeepRecursion()
    {
        // Arrange
        var code = @"
                let total = 0
                let i = 0
                let j = 0
                let k = 0
                
                @loop_i:
                if i >= 5 goto @end_i
                    j = 0
                    
                    @loop_j:
                    if j >= 4 goto @end_j
                        k = 0
                        
                        @loop_k:
                        if k >= 3 goto @end_k
                            total = total + (i * 100 + j * 10 + k)
                            k = k + 1
                            goto @loop_k
                        @end_k:
                        
                        j = j + 1
                        goto @loop_j
                    @end_j:
                    
                    i = i + 1
                    goto @loop_i
                @end_i:
                
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
        // Sum of all combinations: i=0-4, j=0-3, k=0-2
        // This is a complex calculation that tests loop performance
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(12960).Within(1e-9));
    }

    [Test]
    public void Execute_MemoryIntensiveCalculation_HandlesLargeIterations()
    {
        // Arrange
        var code = @"
                let sum = 0
                let i = 1
                
                @loop:
                if i > 100 goto @end
                    let j = 1
                    
                    @inner:
                    if j > 100 goto @inner_end
                        sum = sum + (i * j)
                        j = j + 1
                        goto @inner
                    @inner_end:
                    
                    i = i + 1
                    goto @loop
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
        // Sum of i*j for i=1..1000, j=1..100
        // = (sum i=1..100) * (sum j=1..100) = (1000*1001/2) * (100*101/2)
        // = 5050 * 5050 = 25502500
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(25502500).Within(1e-9));
    }
}