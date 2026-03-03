using NumbersModule.Core;

namespace Tests.Arithmetic;

[TestFixture]
public class NegativeNumbersGeneratedTests : TestBase
{
    private const double Tolerance = 1e-9;

    public static IEnumerable<TestCaseData> NegativeNumberExpressionCases()
    {
        yield return new TestCaseData("-5 + 2", -3d).SetName("Negative_Addition_WithPositive");
        yield return new TestCaseData("-5 - 2", -7d).SetName("Negative_Subtraction_WithPositive");
        yield return new TestCaseData("-5 * -2", 10d).SetName("Negative_Multiplication_TwoNegatives");
        yield return new TestCaseData("-20 / -4", 5d).SetName("Negative_Division_TwoNegatives");
        yield return new TestCaseData("(-5 + 2) * (3 - 7)", 12d).SetName("Negative_WithParentheses");
        yield return new TestCaseData("-10 + 2 * (-3 + 8) - 4", -4d).SetName("Negative_MixedOperators_RespectsPrecedence");
    }

    public static IEnumerable<TestCaseData> ComplexNegativeScenarioCases()
    {
        yield return new TestCaseData(
            @"
                let a = -8
                let b = 3
                let c = -2
                ((a + b) * (c - b) + (a / c))
            ",
            29d).SetName("ComplexNegative_Variables_WithNestedParentheses");

        yield return new TestCaseData(
            @"
                let x = -15
                let y = -4
                let z = 6
                (x / y) + (z * (x + y)) - (x - z)
            ",
            -89.25d).SetName("ComplexNegative_MixedSigns_DivMulAndSub");

        yield return new TestCaseData(
            @"
                let p = -3
                let q = 5
                let r = -7
                ((p * q) - (r / p) + (q - r) * (p + 2))
            ",
            -29.333333333333332d).SetName("ComplexNegative_FractionalResult_WithNestedCombinations");
    }

    public static IEnumerable<TestCaseData> GeneratedNegativeCombinationCases()
    {
        var values = new[] { -10d, -3d, -1d, 1d, 2d, 5d };
        var operations = new[] { "+", "-", "*", "/" };

        foreach (var left in values)
        {
            foreach (var right in values)
            {
                foreach (var op in operations)
                {
                    if (op == "/" && Math.Abs(right) < Tolerance)
                    {
                        continue;
                    }

                    var expression = $"({left}) {op} ({right})";
                    var expected = op switch
                    {
                        "+" => left + right,
                        "-" => left - right,
                        "*" => left * right,
                        "/" => left / right,
                        _ => throw new InvalidOperationException("Unsupported operation")
                    };

                    yield return new TestCaseData(expression, expected)
                        .SetName($"Generated_{left}_{op}_{right}".Replace('-', 'N').Replace('.', '_'));
                }
            }
        }
    }

    [TestCaseSource(nameof(NegativeNumberExpressionCases))]
    public void Execute_NegativeNumberExpressionScenarios_ReturnsExpectedResult(string code, double expected)
    {
        var result = ExecuteCode(code);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(expected).Within(Tolerance));
    }

    [TestCaseSource(nameof(ComplexNegativeScenarioCases))]
    public void Execute_ComplexNegativeScenarios_ReturnsExpectedResult(string code, double expected)
    {
        var result = ExecuteCode(code);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(expected).Within(Tolerance));
    }

    [TestCaseSource(nameof(GeneratedNegativeCombinationCases))]
    public void Execute_GeneratedNegativeCombinationScenarios_ReturnsExpectedResult(string code, double expected)
    {
        var result = ExecuteCode(code);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(expected).Within(Tolerance));
    }
}
