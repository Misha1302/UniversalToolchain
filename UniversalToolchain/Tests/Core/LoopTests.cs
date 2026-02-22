namespace Tests;

[TestFixture]
public class LoopTests : TestBase
{
    [Test]
    public void Execute_SimpleLoopWithLabels_ComputesSumCorrectly()
    {
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


        var result = ExecuteCode(code);


        // Sum of 1 to 10 = 55
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(55).Within(1e-9));
    }

    [Test]
    public void Execute_NestedLoops_ComputesMultiplicationTable()
    {
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


        var result = ExecuteCode(code);


        // Sum of multiplication table 3x3: 
        // 1*1 + 1*2 + 1*3 + 2*1 + 2*2 + 2*3 + 3*1 + 3*2 + 3*3 = 36
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(36).Within(1e-9));
    }

    [Test]
    public void Execute_ConditionalLoopBreak_StopsWhenConditionMet()
    {
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


        var result = ExecuteCode(code);


        // Sum of numbers 1 to 5 = 15
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(15).Within(1e-9));
    }

    [Test]
    public void Execute_WhileLoop_ComputesSumCorrectly()
    {
        var code = @"
                let sum = 0
                let i = 1

                while (i <= 10) (
                    sum = sum + i
                    i = i + 1
                )

                sum
            ";

        var result = ExecuteCode(code);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(55).Within(1e-9));
    }

    [Test]
    public void Execute_ForLoop_ComputesSumCorrectly()
    {
        var code = @"
                let sum = 0

                for (let i = 1) (i <= 10) (i = i + 1) (
                    sum = sum + i
                )

                sum
            ";

        var result = ExecuteCode(code);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(55).Within(1e-9));
    }

    [Test]
    public void Execute_ForLoop_ConditionFalseInitially_DoesNotRunBodyOrStep()
    {
        var code = @"
                let accumulator = 100

                for (let i = 10) (i < 0) (i = i + 1) (
                    accumulator = accumulator + 1
                )

                accumulator
            ";

        var result = ExecuteCode(code);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(100).Within(1e-9));
    }

    [Test]
    public void Execute_ForLoop_InitRunsExactlyOnce()
    {
        var code = @"
                let initCounter = 0
                let iterations = 0
                let result = -1

                for (initCounter = initCounter + 1) (iterations < 3) (iterations = iterations + 1) (
                    result = 0
                )

                result = initCounter
                result
            ";

        var result = ExecuteCode(code);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(1).Within(1e-9));
    }

    [Test]
    public void Execute_ForLoop_StepRunsOnlyAfterBody()
    {
        var code = @"
                let bodyRuns = 0
                let stepBeforeBody = 0
                let i = 0

                for (i = 0) (i < 3) (i = i + 1) (
                    if i > bodyRuns (
                        stepBeforeBody = 1
                    )

                    bodyRuns = bodyRuns + 1
                )

                stepBeforeBody
            ";

        var result = ExecuteCode(code);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(0).Within(1e-9));
    }
    
    
    [Test]
    public void Execute_ForLoop_NegativeStep_ComputesDescendingSum()
    {
        var sumCode = @"
                let sum = 0

                for (let i = 5) (i >= 1) (i = i - 1) (
                    sum = sum + i
                )

                sum
            ";

        var counterCode = @"
                let i = 0

                for (i = 5) (i >= 1) (i = i - 1) (
                )

                i
            ";

        var sumResult = ExecuteCode(sumCode);
        var counterResult = ExecuteCode(counterCode);

        var sumNumber = (RealNumberImpl)sumResult;
        Assert.That(sumNumber.GetValue(), Is.EqualTo(15).Within(1e-9));

        var counterNumber = (RealNumberImpl)counterResult;
        Assert.That(counterNumber.GetValue(), Is.EqualTo(0).Within(1e-9));
    }

    [Test]
    public void Execute_ForLoop_FractionalStep_ComputesExpectedCount()
    {
        var code = @"
                let count = 0

                for (let i = 0) (i <= 1) (i = i + 0.25) (
                    count = count + 1
                )

                count
            ";

        var result = ExecuteCode(code);

        // Inclusive range [0, 1] with step 0.25 gives ((1 - 0) / 0.25) + 1 = 5 iterations.
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(5).Within(1e-9));
    }

    [Test]
    public void Execute_ForLoop_LargeStep_SkipsValuesCorrectly()
    {
        var sumCode = @"
                let sum = 0

                for (let i = 0) (i <= 9) (i = i + 3) (
                    sum = sum + i
                )

                sum
            ";

        var counterCode = @"
                let i = 0

                for (i = 0) (i <= 9) (i = i + 3) (
                )

                i
            ";

        var sumResult = ExecuteCode(sumCode);
        var counterResult = ExecuteCode(counterCode);

        // Iterations should include only 0, 3, 6, 9.
        var sumNumber = (RealNumberImpl)sumResult;
        Assert.That(sumNumber.GetValue(), Is.EqualTo(18).Within(1e-9));

        var counterNumber = (RealNumberImpl)counterResult;
        Assert.That(counterNumber.GetValue(), Is.EqualTo(12).Within(1e-9));
    }

}
