namespace Tests;

[TestFixture]
public class PerformanceTests : TestBase
{
    [Test]
    public void Execute_ManySimpleOperations_PerformsWithinReasonableTime()
    {
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

        var stopwatch = new Stopwatch();


        stopwatch.Start();
        var result = ExecuteCode(code);
        stopwatch.Stop();


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(4950).Within(1e-9));
        Assert.That(
            stopwatch.ElapsedMilliseconds / CoresCount,
            Is.LessThan(1000)
        );
    }

    [Test]
    public void Execute_ComplexNestedExpressions_PerformsWell()
    {
        var code = @"
                let a = 1 + 2 * 3 - 4 / 2 + (5 * (6 - 2)) / 4
                let b = a * 2 - a / 2 + (a + 1) * 3
                let c = b * a - b / a + (a + b) * 2
                c
            ";

        var stopwatch = new Stopwatch();


        stopwatch.Start();
        var result = ExecuteCode(code);
        stopwatch.Stop();


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(591.2).Within(1e-9));
        Assert.That(
            stopwatch.ElapsedMilliseconds / CoresCount,
            Is.LessThan(500)
        );
    }
}