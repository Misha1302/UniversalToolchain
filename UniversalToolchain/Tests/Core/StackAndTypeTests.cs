namespace Tests;

[TestFixture]
public class StackAndTypeTests : TestBase
{
    [Test]
    public void Execute_ComplexStackOperationsWithMixedTypes_PreservesStackIntegrity()
    {
        var code = @"
                let a = 10
                let b = 20.5
                let c = a + b
                let d = c * 2
                let e = Main.Floor(d) 
                let f = Main.Ceiling(b) 
                let g = e - f
                
                let h = (a + b) * (c - d / e) + f
                h
            ";


        var result = ExecuteCode(code);


        // a=10, b=20.5, c=30.5, d=61.0, e=61.0, f=21.0, g=40.0
        // h = (10 + 20.5) * (30.5 - 61.0 / 61.0) + 21.0
        //   = 30.5 * (30.5 - 1.0) + 21.0
        //   = 30.5 * 29.5 + 21.0
        //   = 899.75 + 21.0 = 920.75
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(920.75).Within(1e-9));
    }

    [Test]
    public void Execute_NestedMethodCalls_MaintainsStackCorrectly()
    {
        var code = @"
                let x = 2.0
                let y = 3.0
                let z = 4.0
                let e = 2.718281828459045
                
                let result = Main.Pow(
                    Main.Sqrt(x) + Main.Log(y, e),
                    Main.Abs(z - y)
                )
                result
            ";


        var result = ExecuteCode(code);


        // sqrt(2) ≈ 1.41421356
        // ln(3) ≈ 1.09861229
        // sum ≈ 2.51282585
        // abs(4-3) = 1
        // pow(2.51282585, 1) = 2.51282585
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(2.51282585).Within(1e-7));
    }

    [Test]
    public void Execute_StackHeavyOperationsWithConditionals_HandlesComplexFlow()
    {
        var code = @"
                let stackTest = 0
                let i = 0
                let pi = 3.141592653589793
                
                @stack_loop:
                if i >= 5 goto @stack_end
                    let temp1 = Main.Sin(i * pi / 10)
                    let temp2 = Main.Cos(i * pi / 10)
                    let temp3 = Main.Pow(temp1, 2) + Main.Pow(temp2, 2)
                    
                    
                    if Main.Abs(temp3 - 1.0) < 0.000001
                        stackTest = stackTest + 1
                    
                    i = i + 1
                    goto @stack_loop
                @stack_end:
                
                stackTest
            ";


        var result = ExecuteCode(code);


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(5).Within(1e-9));
    }
}