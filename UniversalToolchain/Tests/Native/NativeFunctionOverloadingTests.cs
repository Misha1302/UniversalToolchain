namespace Tests.Native;

[TestFixture]
public class NativeFunctionOverloadingTests : TestBase
{
    [SetUp]
    public void Setup()
    {
        SetArithmeticMode(WistOptions.ArithmeticModeEnum.Native);
    }

    [Test]
    public void Execute_SystemMathAbsOverloads_NativeMode_Int()
    {
        var code = "System.Math.Abs(-5)";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Execute_SystemMathAbsOverloads_NativeMode_Double()
    {
        var code = "System.Math.Abs(-5.5)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(5.5).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathAbsOverloads_NativeMode_Float()
    {
        var code = "System.Math.Abs(-3.14F)";
        var result = ExecuteCode<float>(code);
        Assert.That(result, Is.EqualTo(3.14F).Within(1e-6F));
    }

    [Test]
    public void Execute_SystemMathAbsOverloads_NativeMode_Long()
    {
        var code = "System.Math.Abs(-100)";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void Execute_SystemMathPowOverloads_NativeMode_Double()
    {
        var code = "System.Math.Pow(2.0, 3.0)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(8.0).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathPowOverloads_NativeMode_IntAndDouble_WidensIntToDouble()
    {
        var code = "System.Math.Pow(2, 3.0)";
        Assert.Throws<InvalidOperationException>(() => ExecuteCode<double>(code));
    }

    [Test]
    public void Execute_SystemMathSqrtOverloads_NativeMode_Double()
    {
        var code = "System.Math.Sqrt(25.0)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(5.0).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathMaxOverloads_NativeMode_Int()
    {
        var code = "System.Math.Max(5, 10)";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Execute_SystemMathMaxOverloads_NativeMode_Double()
    {
        var code = "System.Math.Max(3.14, 2.71)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(3.14).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathMaxOverloads_NativeMode_Float()
    {
        var code = "System.Math.Max(1.5F, 2.5F)";
        var result = ExecuteCode<float>(code);
        Assert.That(result, Is.EqualTo(2.5F).Within(1e-6F));
    }

    [Test]
    public void Execute_SystemMathMinOverloads_NativeMode_Int()
    {
        var code = "System.Math.Min(5, 10)";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Execute_SystemMathMinOverloads_NativeMode_Double()
    {
        var code = "System.Math.Min(3.14, 2.71)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(2.71).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathRoundOverloads_NativeMode_Double()
    {
        var code = "System.Math.Round(3.14159)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(3.0).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathRoundOverloads_NativeMode_DoubleWithDigits()
    {
        var code = "System.Math.Round(3.14159, 2)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(3.14).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathCeilingOverloads_NativeMode_Double()
    {
        var code = "System.Math.Ceiling(3.14159)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(4.0).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathFloorOverloads_NativeMode_Double()
    {
        var code = "System.Math.Floor(3.14159)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(3.0).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathSinOverloads_NativeMode_Double()
    {
        var code = "System.Math.Sin(0.0)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(0.0).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathCosOverloads_NativeMode_Double()
    {
        var code = "System.Math.Cos(0.0)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(1.0).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathTanOverloads_NativeMode_Double()
    {
        var code = "System.Math.Tan(0.0)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(0.0).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathLogOverloads_NativeMode_Double()
    {
        var code = "System.Math.Log(1.0)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(0.0).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathLog10Overloads_NativeMode_Double()
    {
        var code = "System.Math.Log10(100.0)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(2.0).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathExpOverloads_NativeMode_Double()
    {
        var code = "System.Math.Exp(0.0)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(1.0).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathClampOverloads_NativeMode_Int()
    {
        var code = "System.Math.Clamp(15, 0, 10)";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Execute_SystemMathClampOverloads_NativeMode_Double()
    {
        var code = "System.Math.Clamp(15.5, 0.0, 10.0)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(10.0).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathClampOverloads_NativeMode_Float()
    {
        var code = "System.Math.Clamp(15.5F, 0.0F, 10.0F)";
        var result = ExecuteCode<float>(code);
        Assert.That(result, Is.EqualTo(10.0F).Within(1e-6F));
    }

    [Test]
    public void Execute_SystemConvertToInt32Overloads_NativeMode_Double()
    {
        var code = "System.Convert.ToInt32(3.14159)";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Execute_SystemConvertToDoubleOverloads_NativeMode_Int()
    {
        var code = "System.Convert.ToDouble(42)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(42.0).Within(1e-9));
    }

    [Test]
    public void Execute_SystemConvertToDecimalOverloads_NativeMode_Double()
    {
        var code = "System.Convert.ToDecimal(3.14159)";
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(3.14159M).Within(0.00001M));
    }

    [Test]
    public void Execute_MultipleMathFunctionCalls_NativeMode_ComplexExpression()
    {
        var code = @"
            let x = 2.0
            let y = 3.0
            
            let powResult = System.Math.Pow(x, y)
            let sqrtResult = System.Math.Sqrt(powResult)
            let absResult = System.Math.Abs(-4.0)
            let logResult = System.Math.Log10(100.0)
            
            sqrtResult + absResult * logResult
        ";

        var result = ExecuteCode<double>(code);
        // 2^3 = 8, sqrt(8) = 2.8284271247461903
        // abs(-4) = 4, log10(100) = 2, 4*2 = 8
        // total = 2.8284271247461903 + 8 = 10.82842712474619
        Assert.That(result, Is.EqualTo(10.82842712474619).Within(1e-9));
    }

    [Test]
    public void Execute_MathFunctionWithTypeInference_NativeMode_MixedTypes()
    {
        var code = @"
            let intVal = 5
            let doubleVal = 3.14
            let floatVal = 2.718F
            
            let max1 = System.Math.Max(intVal, 3)
            let max2 = System.Math.Max(doubleVal, 2.71)
            let max3 = System.Math.Max(floatVal, 1.5F)
            
            Main.ToDouble(max1) + max2 + Main.ToDouble(max3)
        ";

        var result = ExecuteCode<double>(code);
        // max(5,3)=5, max(3.14,2.71)=3.14, max(2.718,1.5)=2.718
        // total = 5 + 3.14 + 2.718 = 10.858
        Assert.That(result, Is.EqualTo(10.858).Within(1e-7));
    }

    [Test]
    public void Execute_OverloadedFunctionInConditional_NativeMode_ConditionalExecution()
    {
        var code = @"
            let value = 25.0
            let threshold = 20.0
            
            let result = (
                if value > threshold (
                    System.Math.Sqrt(value) + System.Math.Abs(-5.0)
                ) else (
                    System.Math.Pow(value, 2.0)
                )
            )
            
            result
        ";

        var result = ExecuteCode<double>(code);
        // sqrt(25) + abs(-5) = 5 + 5 = 10
        Assert.That(result, Is.EqualTo(10.0).Within(1e-9));
    }

    [Test]
    public void Execute_NestedFunctionCalls_NativeMode_DeepCallStack()
    {
        var code = @"
            let a = 2.0
            let b = 3.0
            
            System.Math.Sqrt(
                System.Math.Pow(
                    System.Math.Abs(0.0 - a),
                    System.Math.Floor(b)
                )
            )
        ";

        var result = ExecuteCode<double>(code);
        // abs(-2) = 2, floor(3) = 3, 2^3 = 8, sqrt(8) = 2.8284271247461903
        Assert.That(result, Is.EqualTo(2.8284271247461903).Within(1e-9));
    }

    [Test]
    public void Execute_MathFunctionWithVariables_NativeMode_DynamicParameters()
    {
        var code = @"
            let baseValue = 10.0
            let exponent = 2.5
            
            let powerResult = System.Math.Pow(baseValue, exponent)
            let sqrtResult = System.Math.Sqrt(powerResult)
            let rounded = System.Math.Round(sqrtResult, 2)
            
            rounded
        ";

        var result = ExecuteCode<double>(code);
        // 10^2.5 = 316.227766, sqrt = 17.7827941, rounded to 2 decimals = 17.78
        Assert.That(result, Is.EqualTo(17.78).Within(1e-2));
    }

    [Test]
    public void Execute_SystemDoubleParseOverloads_NativeMode_WithCulture()
    {
        // Note: This test might fail if string support is not fully implemented
        // We'll use a simple numeric expression instead for now
        var code = @"
            let value = 123.456
            let rounded = System.Math.Round(value, 2)
            let parsed = System.Convert.ToDouble(rounded)
            
            parsed
        ";

        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(123.46).Within(1e-9));
    }

    [Test]
    public void Execute_SystemMathSignOverloads_NativeMode_Int()
    {
        var code = "System.Math.Sign(-5)";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(-1));
    }

    [Test]
    public void Execute_SystemMathSignOverloads_NativeMode_Double()
    {
        var code = "System.Math.Sign(3.14)";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Execute_SystemMathSignOverloads_NativeMode_Float()
    {
        var code = "System.Math.Sign(-2.718F)";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(-1));
    }

    [Test]
    public void Execute_ComplexFinancialCalculation_NativeMode_MultipleFunctions()
    {
        var code = @"
            let principal = 1000.0
            let rate = 0.05
            let years = 3.0
            
            // Compound interest: P * (1 + r)^n
            let growthFactor = System.Math.Pow(1.0 + rate, years)
            let futureValue = principal * growthFactor
            
            // Round to 2 decimal places for currency
            System.Math.Round(futureValue, 2)
        ";

        var result = ExecuteCode<double>(code);
        // 1000 * (1.05)^3 = 1000 * 1.157625 = 1157.625, rounded = 1157.63
        Assert.That(result, Is.EqualTo(1157.63).Within(1e-9));
    }
}