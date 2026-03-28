namespace Tests.Core;

[TestFixture]
public class EdgeCasesTests : TestBase
{
    [Test]
    public void Execute_ZeroValues_HandlesCorrectly()
    {
        var code = @"
                let zero = 0
                let result = zero * 100 + zero / 1
                result
            ";


        var result = ExecuteCode(code);


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(0).Within(1e-9));
    }

    [Test]
    public void Execute_VeryLargeNumbers_HandlesCorrectly()
    {
        var code = @"
                let big = 1000000
                let veryBig = big * big
                veryBig / big
            ";


        var result = ExecuteCode(code);


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(1000000).Within(1e-9));
    }

    [Test]
    public void Execute_DecimalNumbers_ComputesPrecisely()
    {
        var code = @"
                let a = 0.1
                let b = 0.2
                let c = a + b
                c
            ";


        var result = ExecuteCode(code);


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(0.3).Within(1e-9));
    }
}