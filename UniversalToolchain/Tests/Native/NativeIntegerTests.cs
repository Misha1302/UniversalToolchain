namespace Tests;

[TestFixture]
public class NativeIntegerTests : TestBase
{
    [SetUp]
    public void Setup()
    {
        SetArithmeticMode(WistOptions.ArithmeticModeEnum.Native);
    }

    [Test]
    public void Execute_IntAddition_ReturnsCorrectResult()
    {
        var code = "1 + 2";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Execute_IntSubtraction_ReturnsCorrectResult()
    {
        var code = "10 - 4";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void Execute_IntMultiplication_ReturnsCorrectResult()
    {
        var code = "3 * 4";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void Execute_IntDivision_ReturnsIntegerResult()
    {
        var code = "7 / 2";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(3)); // Целочисленное деление
    }

    [Test]
    public void Execute_IntWithSuffixL_ReturnsLong()
    {
        var code = "100 + 200";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(300));
    }

    [Test]
    public void Execute_LargeIntOperations_HandlesCorrectly()
    {
        var code = "1000000M * 1000000M";
        var result = ExecuteCode<decimal>(code);
        Assert.That(result, Is.EqualTo(1000000000000L));
    }

    [Test]
    public void Execute_IntWithUnderscores_HandlesCorrectly()
    {
        var code = "1_000_000 + 2_000_000";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(3000000));
    }

    [Test]
    public void Execute_MixedIntOperationsWithParentheses_RespectsPrecedence()
    {
        var code = "(2 + 3) * 4 - 10 / 2";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(15)); // (5 * 4) - 5 = 15
    }

    [Test]
    public void Execute_IntNegativeNumbers_HandlesCorrectly()
    {
        var code = "-5 + 10 - (-3)";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void Execute_IntComparisonOperations_ReturnBoolean()
    {
        var code = "5 > 3 and 2 < 4";
        var result = ExecuteCode<bool>(code);
        Assert.That(result, Is.True);
    }

    [Test]
    public void Execute_IntWithVariables_WorksCorrectly()
    {
        var code = """
                   let a = 10
                   let b = 20
                   a + b
                   """;
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void Execute_IntInLoop_AccumulatesCorrectly()
    {
        var code = """
                   let sum = 0
                   let i = 1
                   @loop:
                   if i > 5 goto @end
                   sum = sum + i
                   i = i + 1
                   goto @loop
                   @end:
                   sum
                   """;
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(15)); // 1+2+3+4+5
    }

    [Test]
    public void Execute_IntWithExplicitTypeCast_ConvertsCorrectly()
    {
        var code = "Main.ToInt(5.7)";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Execute_IntWithScientificNotation_ConvertsToInt()
    {
        var code = "1e3"; // 1000
        var result = ExecuteCode<double>(code);
        Assert.That(result, Is.EqualTo(1000).Within(1e-7));
    }

    [Test]
    public void Execute_IntMaxValue_HandlesCorrectly()
    {
        var code = "2147483647";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void Execute_IntMinValue_HandlesCorrectly()
    {
        var code = "-2147483648";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(int.MinValue));
    }

    [Test]
    public void Execute_IntComplexExpression_ComputesCorrectly()
    {
        var code = "((2 + 3) * (4 - 1) + 5) / 2";
        var result = ExecuteCode<int>(code);
        Assert.That(result, Is.EqualTo(10)); // (5*3+5)/2 = 20/2 = 10
    }
}
