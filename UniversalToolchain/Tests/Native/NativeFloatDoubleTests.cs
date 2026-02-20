namespace Tests;

[TestFixture]
public class NativeFloatDoubleTests : TestBase
{
    [SetUp]
    public void Setup()
    {
        SetArithmeticMode(WistOptions.ArithmeticModeEnum.Native);
    }

    [Test]
    public void Execute_FloatAddition_ReturnsCorrectResult()
    {
        var code = "1.5F + 2.5F";
        var result = ExecuteCode<float>(code);
        Assert.That(result, Is.EqualTo(4.0F).Within(1e-6F));
    }

    [Test]
    public void Execute_DoubleAddition_ReturnsCorrectResult()
    {
        var code = "1.5 + 2.5";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(4.0).Within(1e-9));
    }

    [Test]
    public void Execute_FloatWithSuffix_ReturnsFloat()
    {
        var code = "3.14F * 2F";
        var result = ExecuteCode<float>(code);
        Assert.That(result, Is.EqualTo(6.28F).Within(1e-6F));
    }

    [Test]
    public void Execute_DoubleWithSuffix_ReturnsDouble()
    {
        var code = "3.14D * 2D";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(6.28).Within(1e-9));
    }

    [Test]
    public void Execute_FloatPrecision_HandlesSmallValues()
    {
        var code = "0.1F + 0.2F";
        var result = ExecuteCode<float>(code);
        Assert.That(result, Is.EqualTo(0.3F).Within(1e-6F));
    }

    [Test]
    public void Execute_DoublePrecision_HandlesScientificNotation()
    {
        var code = "1.23e4 + 2.34e3";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(14640.0).Within(1e-9));
    }

    [Test]
    public void Execute_FloatDivision_ReturnsFractionalResult()
    {
        var code = "5.0F / 2.0F";
        var result = ExecuteCode<float>(code);
        Assert.That(result, Is.EqualTo(2.5F).Within(1e-6F));
    }

    [Test]
    public void Execute_DoubleDivision_ReturnsFractionalResult()
    {
        var code = "5.0 / 2.0";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(2.5).Within(1e-9));
    }

    [Test]
    public void Execute_FloatWithUnderscores_HandlesCorrectly()
    {
        var code = "1_000.5F + 2_000.5F";
        var result = ExecuteCode<float>(code);
        Assert.That(result, Is.EqualTo(3001.0F).Within(1e-6F));
    }

    [Test]
    public void Execute_DoubleComplexExpression_ComputesCorrectly()
    {
        var code = "(2.5 + 3.5) * (4.2 - 1.2) / 2.0";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(9.0).Within(1e-9)); // 6.0 * 3.0 / 2.0 = 9.0
    }

    [Test]
    public void Execute_FloatWithVariablesAndCondition()
    {
        var code = """
                   let temp = 98.6F
                   let threshold = 100.0F
                   if temp < threshold
                      temp + 1.5F
                   else
                      temp
                   """;
        var result = ExecuteCode<float>(code);
        Assert.That(result, Is.EqualTo(100.1F).Within(1e-6F));
    }

    [Test]
    public void Execute_DoubleInLoop_AccumulatesPrecision()
    {
        var code = """
                   let sum = 0.0
                   let i = 1.0
                   @loop:
                       if i > 3.0 goto @end
                       sum = sum + (1.0 / i)
                       i = i + 1.0
                       goto @loop   
                   @end:
                   sum
                   """;
        var result = ExecuteCode<double>(code);
        // 1 + 1/2 + 1/3 ≈ 1.8333333333333333
        Assert.That(result, Is.EqualTo(1 + 1.0 / 2 + 1.0 / 3).Within(1e-9));
    }

    [Test]
    public void Execute_FloatTypeConversion_FromInt()
    {
        var code = "Main.ToFloat(5)";
        var result = ExecuteCode<float>(code);
        Assert.That(result, Is.EqualTo(5.0F).Within(1e-6F));
    }

    [Test]
    public void Execute_DoubleTypeConversion_FromFloat()
    {
        var code = "Main.ToDouble(3.14F)";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(3.14).Within(1e-6));
    }

    [Test]
    public void Execute_FloatSpecialValues_HandlesInfinity()
    {
        var code = "1.0F / 0.0F";
        var result = ExecuteCode<float>(code);
        Assert.That(float.IsInfinity(result), Is.True);
    }

    [Test]
    public void Execute_DoubleSpecialValues_HandlesNaN()
    {
        var code = "0.0 / 0.0";
        var result = ExecuteCode<double>(code);
        Assert.That(double.IsNaN(result), Is.True);
    }
}
