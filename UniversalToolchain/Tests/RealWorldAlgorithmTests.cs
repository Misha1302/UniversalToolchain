namespace Tests;

[TestFixture]
public class RealWorldAlgorithmTests : TestBase
{
    [Test]
    public void Execute_FibonacciSequence_ComputesCorrectValue()
    {
        // Arrange
        var code = @"
                let n = 10
                let a = 0
                let b = 1
                let temp = 0
                let i = 2
                
                if n == 0
                    a = 0
                else (
                    if n == 1
                        a = 1
                    else
                        @loop:
                        if i > n goto @end
                            temp = a + b
                            a = b
                            b = temp
                            i = i + 1
                            goto @loop
                        @end:
                )
                b
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
        var result = ExecuteCode(code);

        // Assert
        // Fibonacci(10) = 55
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(55).Within(1e-9));
    }

    [Test]
    public void Execute_FactorialCalculation_ComputesLargeFactorial()
    {
        // Arrange
        var code = @"
                let n = 6
                let result = 1
                let i = 1
                
                @loop:
                if i > n goto @end
                    result = result * i
                    i = i + 1
                    goto @loop
                @end:
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
        var result = ExecuteCode(code);

        // Assert
        // 6! = 720
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(720).Within(1e-9));
    }

    [Test]
    public void Execute_BubbleSortSimulation_SortsNumbers()
    {
        // Arrange
        var code = @"
                let a = 5
                let b = 3  
                let c = 8
                let d = 1
                let e = 2
                let temp = 0
                let swapped = 1
                let i = 0
                
                @outer_loop:
                if swapped == 1 goto @end
                    swapped = 0
                    
                    if a > b
                        temp = a
                        a = b
                        b = temp
                        swapped = 1
                    
                    if b > c
                        temp = b
                        b = c
                        c = temp
                        swapped = 1
                    
                    if c > d
                        temp = c
                        c = d
                        d = temp
                        swapped = 1
                    
                    if d > e
                        temp = d
                        d = e
                        e = temp
                        swapped = 1
                    
                    goto @outer_loop
                @end:
                
                a + b + c + d + e
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
        var result = ExecuteCode(code);

        // Assert
        // Sum of sorted array [1, 2, 3, 5, 8] = 19
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(19).Within(1e-9));
    }
}