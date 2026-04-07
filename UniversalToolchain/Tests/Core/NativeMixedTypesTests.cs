namespace Tests.Core;

[TestFixture]
public class NativeMixedTypesTests : LegacyTestBase
{
    [SetUp]
    public void Setup()
    {
        SetArithmeticMode(ArithmeticMode.Native);
    }

    [Test]
    public void Execute_TypeInference_ForDifferentLiterals()
    {
        // Тест проверяет, что система правильно определяет типы литералов
        var codeInt = "42";
        var resultInt = ExecuteCode(codeInt);
        Assert.That(resultInt, Is.TypeOf<int>());

        var codeFloat = "3.14F";
        var resultFloat = ExecuteCode(codeFloat);
        Assert.That(resultFloat, Is.TypeOf<float>());

        var codeDouble = "3.14";
        var resultDouble = ExecuteCode(codeDouble);
        Assert.That(resultDouble, Is.TypeOf<double>());

        var codeDecimal = "3.14M";
        var resultDecimal = ExecuteCode(codeDecimal);
        Assert.That(resultDecimal, Is.TypeOf<decimal>());
    }

    [Test]
    public void Execute_NativeMathFunctions_WithDifferentTypes()
    {
        var code = """
                   let intResult = DoubleMath.Abs(Main.ToDouble(-5))
                   let floatResult = DoubleMath.Sqrt(Main.ToDouble(16.0F))
                   let doubleResult = DoubleMath.Pow(2.0, 3.0)
                   intResult + floatResult + doubleResult
                   """;
        var result = ExecuteCode<double>(code);
        // 5 + 4 + 8 + 3.14 = 20.14
        Assert.That(result, Is.EqualTo(5 + 4 + 8).Within(1e-9));
    }

    [Test]
    public void Execute_ComparisonOperators_WithDifferentNumericTypes()
    {
        var code = """
                   let a = 5
                   let b = 5.0
                   let c = 5.0F
                   let d = 5M
                   (Main.ToDouble(a) == b) and (Main.ToFloat(b) == c) and (Main.ToDecimal(c) == d)
                   """;
        var result = ExecuteCode<bool>(code);
        Assert.That(result, Is.True);
    }

    [Test]
    public void Execute_ConditionalExpressions_WithTypePromotion()
    {
        var code = """
                   let x = 10
                   if x > 5
                      3.14
                   else
                      42.0
                   """;
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(3.14).Within(1e-9));
    }

    [Test]
    public void Execute_VariableReassignment_WithTypeChange()
    {
        var code = """
                   let x = 10.0
                   x = 20.5
                   x
                   """;
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(20.5).Within(1e-9));
    }

    [Test]
    public void Execute_NativePerformance_ComplexCalculation()
    {
        var code =
            """
            let iterations = 1000
            let pi = 3.141592653589793
            let e = 2.718281828459045
            let sum = 0.0
            let i = 0
            @loop:
                if i >= iterations goto @end
                let angle = Main.ToDouble(i) * pi / Main.ToDouble(iterations)
                sum = sum + DoubleMath.Sin(angle) * DoubleMath.Cos(angle) * DoubleMath.Exp((0.0 - angle) / e)
                i = i + 1
                goto @loop
            @end:
            sum
            """;

        var stopwatch = Stopwatch.StartNew();
        var result = ExecuteCode<double>(code);
        stopwatch.Stop();

        Assert.That(result, Is.EqualTo(52.73964918794139).Within(1e-9));
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000));
    }

    [Test]
    public void Execute_MemoryUsage_WithLargeNativeArrays()
    {
        var code =
            """
            let size = 100
            let sum = 0.0
            let i = 0
            @loop:
            if i >= size goto @end
                let x = Main.ToDouble(i) * 1.0
                let y = DoubleMath.Sin(x) * DoubleMath.Cos(x)
                sum = sum + y
                i = i + 1
                goto @loop
            @end:
            sum
            """;

        var result = ExecuteCode<double>(code);

        Assert.That(result, Is.EqualTo(0.3006425761130279).Within(1e-9));
    }
}