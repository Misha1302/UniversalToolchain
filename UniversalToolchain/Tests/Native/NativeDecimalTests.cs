namespace Tests;

[TestFixture]
public class NativeDecimalTests : TestBase
{
    [SetUp]
    public void Setup()
    {
        SetArithmeticMode(WistOptions.ArithmeticModeEnum.Native);
    }

    [Test]
    public void Execute_DecimalAddition_ReturnsCorrectResult()
    {
        var code = "1.5M + 2.5M";
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(4.0M).Within(0.0000001M));
    }

    [Test]
    public void Execute_DecimalSubtraction_ReturnsCorrectResult()
    {
        var code = "10.0M - 4.5M";
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(5.5M).Within(0.0000001M));
    }

    [Test]
    public void Execute_DecimalMultiplication_ReturnsCorrectResult()
    {
        var code = "2.5M * 3.0M";
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(7.5M).Within(0.0000001M));
    }

    [Test]
    public void Execute_DecimalDivision_ReturnsExactResult()
    {
        var code = "10.0M / 4.0M";
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(2.5M).Within(0.0000001M));
    }

    [Test]
    public void Execute_DecimalWithSuffixM_ReturnsDecimal()
    {
        var code = "123.456M * 2M";
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(246.912M).Within(0.0000001M));
    }

    [Test]
    public void Execute_DecimalHighPrecision_HandlesManyDecimals()
    {
        var code = "1.23456789M + 2.34567890M";
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(3.58024679M).Within(0.00000001M));
    }

    [Test]
    public void Execute_DecimalFinancialCalculation_ExactResults()
    {
        var code = """
                   let principal = 1000.00M
                   let rate = 0.05M
                   let years = 3M
                   let interest = principal * rate * years
                   principal + interest
                   """;
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(1150.00M).Within(0.00001M));
    }

    [Test]
    public void Execute_DecimalWithUnderscores_HandlesCorrectly()
    {
        var code = "1_000_000.75M + 2_000_000.25M";
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(3000001.00M).Within(0.00001M));
    }

    [Test]
    public void Execute_DecimalComplexExpression_MaintainsPrecision()
    {
        var code = "((100.0M - 50.5M) * 2.0M + 1.5M) / 3.0M";
        var result = ExecuteCode<decimal>(code);
        // (49.5 * 2 + 1.5) / 3 = (99 + 1.5) / 3 = 100.5 / 3 = 33.5
        Assert.That(result, Is.EqualTo(33.5M).Within(0.0000001M));
    }

    [Test]
    public void Execute_DecimalTypeConversion_FromInt()
    {
        var code = "Main.ToDecimal(42)";
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(42M).Within(0.00001M));
    }

    [Test]
    public void Execute_DecimalTypeConversion_FromDouble()
    {
        var code = "Main.ToDecimal(3.14159)";
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(3.14159M).Within(0.00001M));
    }

    [Test]
    public void Execute_DecimalInTaxCalculation_FinancialPrecision()
    {
        var code = """
                   let amount = 1234.56M
                   let taxRate = 0.20M
                   let tax = amount * taxRate
                   let total = amount + tax
                   total
                   """;
        var result = ExecuteCode<decimal>(code);
        // 1234.56 * 1.2 = 1481.472
        Assert.That(result, Is.EqualTo(1481.472M).Within(0.00001M));
    }

    [Test]
    public void Execute_DecimalDivisionByZero_ThrowsException()
    {
        var code = "1.0M / 0.0M";
        Assert.That(() => ExecuteCode<decimal>(code), Throws.Exception);
    }

    [Test]
    public void Execute_DecimalWithScientificNotation_ConvertsCorrectly()
    {
        var code = "1.23e4M"; // 12300
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(12300M).Within(0.00001M));
    }

    [Test]
    public void Execute_DecimalMaxValue_HandlesCorrectly()
    {
        var code = "79228162514264337593543950335M"; // Decimal.MaxValue
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(decimal.MaxValue));
    }
}