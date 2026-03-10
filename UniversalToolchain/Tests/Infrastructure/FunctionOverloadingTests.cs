namespace Tests.Infrastructure;

[TestFixture]
public class FunctionOverloadingTests : TestBase
{
    [SetUp]
    public void Setup()
    {
        SetArithmeticMode(WistOptions.ArithmeticModeEnum.Universal);
    }

    [Test]
    public void Execute_MathPowOverloads_UniversalMode_CallsCorrectOverload()
    {
        var code = @"
            let result1 = Main.Pow(2.0, 3.0)
            let result2 = Main.Pow(4.0, 0.5)
            result1 + result2
        ";

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        // 2^3 = 8, 4^0.5 = 2, total = 10
        Assert.That(numberResult.GetValue(), Is.EqualTo(10.0).Within(1e-9));
    }

    [Test]
    public void Execute_MathSqrtOverloads_UniversalMode_CallsCorrectOverload()
    {
        var code = @"
            let a = 25.0
            let b = 100.0
            Main.Sqrt(a) + Main.Sqrt(b)
        ";

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        // sqrt(25) = 5, sqrt(100) = 10, total = 15
        Assert.That(numberResult.GetValue(), Is.EqualTo(15.0).Within(1e-9));
    }

    [Test]
    public void Execute_MathAbsOverloads_UniversalMode_HandlesNegativeNumbers()
    {
        var code = @"
            let a = -10.5
            let b = -3.14
            Main.Abs(a) + Main.Abs(b)
        ";

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        // abs(-10.5) = 10.5, abs(-3.14) = 3.14, total = 13.64
        Assert.That(numberResult.GetValue(), Is.EqualTo(13.64).Within(1e-9));
    }

    [Test]
    public void Execute_MathLogOverloads_UniversalMode_Base10AndNaturalLog()
    {
        var code = @"
            let e = 2.718281828459045
            let ten = 10.0
            let hundred = 100.0
            
            // log base 10 of 100 = 2
            let log10 = Main.Log(hundred, ten)
            
            // natural log of e = 1
            let lnE = Main.Log(e, e)
            
            log10 + lnE
        ";

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        // log10(100) = 2, ln(e) = 1, total = 3
        Assert.That(numberResult.GetValue(), Is.EqualTo(3.0).Within(1e-9));
    }

    [Test]
    public void Execute_MathMaxMinOverloads_UniversalMode_ComparesNumbers()
    {
        var code = @"
            let a = 5.5
            let b = 3.2
            let c = 7.8
            
            let max1 = Main.Max(a, b)
            let max2 = Main.Max(max1, c)
            
            let min1 = Main.Min(a, b)
            let min2 = Main.Min(min1, c)
            
            max2 - min2
        ";

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        // max(5.5, 3.2, 7.8) = 7.8, min(5.5, 3.2, 7.8) = 3.2, difference = 4.6
        Assert.That(numberResult.GetValue(), Is.EqualTo(4.6).Within(1e-9));
    }

    [Test]
    public void Execute_MathFloorCeilingOverloads_UniversalMode_RoundsNumbers()
    {
        var code = @"
            let a = 3.7
            let b = -2.3
            let c = 4.0
            
            Main.Floor(a) + Main.Ceiling(b) + Main.Round(c)
        ";

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        // floor(3.7) = 3, ceiling(-2.3) = -2, round(4.0) = 4, total = 5
        Assert.That(numberResult.GetValue(), Is.EqualTo(5.0).Within(1e-9));
    }

    [Test]
    public void Execute_TrigonometricFunctionsOverloads_UniversalMode_SinCos()
    {
        var code = @"
            let pi = 3.141592653589793
            let angle = pi / 6.0  // 30 degrees
            
            // sin(30°) = 0.5, cos(30°) = √3/2 ≈ 0.86602540378
            Main.Sin(angle) + Main.Cos(angle)
        ";

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        // sin(π/6) + cos(π/6) = 0.5 + 0.86602540378 ≈ 1.36602540378
        Assert.That(numberResult.GetValue(), Is.EqualTo(1.36602540378).Within(1e-9));
    }

    [Test]
    public void Execute_ChainedFunctionCalls_UniversalMode_ComplexExpression()
    {
        var code = @"
            let x = 2.0
            let y = 3.0
            
            // sqrt(pow(x, y) + abs(-4.0) * log(100, 10))
            let powResult = Main.Pow(x, y)
            let absResult = Main.Abs(-4.0)
            let logResult = Main.Log(100.0, 10.0)
            let product = absResult * logResult
            let sum = powResult + product
            
            Main.Sqrt(sum)
        ";

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        // 2^3 = 8, abs(-4) = 4, log10(100) = 2, 4*2 = 8, 8+8=16, sqrt(16)=4
        Assert.That(numberResult.GetValue(), Is.EqualTo(4.0).Within(1e-9));
    }

    [Test]
    public void Execute_FunctionCallsWithVariables_UniversalMode_DynamicParameters()
    {
        var code = @"
            let baseValue = 10.0
            let exponent = 2.5
            
            let powerResult = Main.Pow(baseValue, exponent)
            let sqrtResult = Main.Sqrt(powerResult)
            
            let normalized = sqrtResult / baseValue
            
            Main.Round(normalized * 100) / 100
        ";

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        // 10^2.5 = 316.227766, sqrt = 17.7827941, /10 = 1.77827941, rounded to 2 decimals = 1.78
        Assert.That(numberResult.GetValue(), Is.EqualTo(1.78).Within(1e-2));
    }

    [Test]
    public void Execute_MixedFunctionCallsInConditional_UniversalMode_ConditionalExecution()
    {
        var code = @"
            let value = 25.0
            let threshold = 20.0
            
            let result = (
                if value > threshold (
                    Main.Sqrt(value) + Main.Abs(-5.0)
                ) else (
                    Main.Pow(value, 2.0)
                )
            )
            
            result
        ";

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        // value > threshold, so sqrt(25) + abs(-5) = 5 + 5 = 10
        Assert.That(numberResult.GetValue(), Is.EqualTo(10.0).Within(1e-9));
    }
}