using DependencyInjection;

namespace Tests;

[TestFixture]
public class NativeEdgeCasesTests : TestBase
{
    [SetUp]
    public void Setup()
    {
        SetArithmeticMode(WistOptions.ArithmeticModeEnum.Native);
    }

    [Test]
    public void Execute_FloatOverflow_ReturnsInfinity()
    {
        var code = "1e38F * 1e38F";
        var result = ExecuteCode<float>(code);
        Assert.That(float.IsInfinity(result), Is.True);
    }

    [Test]
    public void Execute_DoubleUnderflow_ReturnsZero()
    {
        var code = "1e-308 / 1e308";
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(0).Within(1e-322));
    }

    [Test]
    public void Execute_DivisionByZero_Int_ThrowsException()
    {
        var code = "1 / 0";
        Assert.That(() => ExecuteCode<int>(code), Throws.Exception);
    }

    [Test]
    public void Execute_DivisionByZero_Float_ReturnsInfinity()
    {
        var code = "1.0F / 0.0F";
        var result = ExecuteCode<float>(code);
        Assert.That(float.IsInfinity(result), Is.True);
    }

    [Test]
    public void Execute_NaN_Propagation()
    {
        var code = "0.0 / 0.0 + 5.0";
        var result = ExecuteCode<double>(code);
        Assert.That(double.IsNaN(result), Is.True);
    }

    [Test]
    public void Execute_Infinity_Operations()
    {
        var code = """
                   let inf = 1.0 / 0.0
                   let negInf = -1.0 / 0.0
                   inf + negInf
                   """;
        var result = ExecuteCode<double>(code);
        Assert.That(double.IsNaN(result), Is.True);
    }

    [Test]
    public void Execute_SignedZero_Operations()
    {
        var code = """
                   let posZero = 0.0
                   let negZero = -0.0
                   1.0 / posZero == 1.0 / negZero
                   """;
        var result = ExecuteCode<bool>(code);
        Assert.That(result, Is.False); // +Inf != -Inf
    }

    [Test]
    public void Execute_DecimalPrecision_Limits()
    {
        var code = """
                   // Decimal имеет максимум 28-29 значащих цифр
                   let a = 79228162514264337593543950335M  // MaxValue
                   let b = 0.0000000000000000000000000001M // Минимальное значение
                   a + b == a
                   """;
        var result = ExecuteCode<bool>(code);
        Assert.That(result, Is.True); // b слишком мало, чтобы изменить a
    }

    [Test]
    public void Execute_TypeConversion_Overflow()
    {
        var code = "int(5000000000.0)"; // 5 миллиардов > int.MaxValue
        Assert.Throws<OverflowException>(() => ExecuteCode<int>(code));
    }

    [Test]
    public void Execute_StackOverflow_DeepRecursion()
    {
        var code = """
                   let depth = 10000
                   @recurse:
                   if depth <= 0 goto @base
                   depth = depth - 1
                   goto @recurse
                   @base:
                   depth
                   """;

        // Не должно вызывать StackOverflowException благодаря оптимизации хвостовой рекурсии
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Execute_MemoryExhaustion_LargeAllocation()
    {
        var code = """
                   let size = 1000000M
                   let sum = 0M
                   let i = 0M
                   @loop:
                       if i >= size goto @end
                       // Создаем промежуточные значения
                       let temp = i * i * i * i / 10M
                       sum = sum + temp
                       i = i + 1M
                       goto @loop
                   @end:
                   sum
                   """;


        
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(19999950000033333333333341834M));
    }
}