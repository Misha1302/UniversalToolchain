using NumbersModule.Core;

namespace Tests.Core;

[TestFixture]
public class UserFunctionsModuleTests : TestBase
{
    private static IEnumerable<TestCaseData> FunctionCompositionCases()
    {
        yield return new TestCaseData(2.0, 3.0, 11.0).SetName("Execute_FunctionComposition_WithPositiveValues_ReturnsExpected");
        yield return new TestCaseData(-4.0, 5.5, -7.5).SetName("Execute_FunctionComposition_WithMixedSigns_ReturnsExpected");
        yield return new TestCaseData(0.0, 7.25, 14.5).SetName("Execute_FunctionComposition_WithZeroArgument_ReturnsExpected");
        yield return new TestCaseData(1.5, -2.5, -2.0).SetName("Execute_FunctionComposition_WithFractions_ReturnsExpected");
    }

    private static IEnumerable<TestCaseData> AssociativityCases()
    {
        var values = new[] { -3.0, -0.5, 0.0, 1.25, 7.0 };

        foreach (var a in values)
        foreach (var b in values)
        foreach (var c in values)
            yield return new TestCaseData(a, b, c)
                .SetName($"Execute_NestedFunctionCalls_Associativity_a_{a}_b_{b}_c_{c}");
    }

    [Test]
    public void Execute_FunctionWithTwoParameters_ReturnsComputedValue()
    {
        const string code =
            """
            fn add(a, b) (
                return a + b
            )

            add(2, 3)
            """;

        var result = ExecuteCode(code);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(5).Within(1e-9));
    }

    [Test]
    public void Execute_FunctionCallWithWrongArgumentsCount_ThrowsException()
    {
        const string code =
            """
            fn add(a, b) (
                return a + b
            )

            add(2)
            """;

        Assert.That(() => ExecuteCode(code), Throws.Exception.With.Message.Contains("expects 2 args"));
    }

    [Test]
    public void Execute_ReturnOutsideFunction_ThrowsException()
    {
        const string code =
            """
            return 1
            """;

        Assert.That(() => ExecuteCode(code), Throws.Exception.With.Message.Contains("return"));
    }

    [Test]
    public void Execute_RecursiveFibonacciFunction_HandlesDeepRecursionAndBranching()
    {
        const string code =
            """
            fn fib(n) (
                if n <= 1 (
                    return n
                )

                return fib(n - 1) + fib(n - 2)
            )

            fib(10)
            """;

        var result = ExecuteCode(code);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(55).Within(1e-9));
    }

    [Test]
    public void Execute_FunctionParameter_ShadowsGlobalVariableWithoutMutatingOuterScope()
    {
        const string code =
            """
            let value = 10

            fn mutate(value) (
                value = value + 5
                return value
            )

            let inner = mutate(1)
            value + inner
            """;

        var result = ExecuteCode(code);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(16).Within(1e-9));
    }

    [TestCaseSource(nameof(FunctionCompositionCases))]
    public void Execute_FunctionComposition_WithGeneratedScenarios_ReturnsExpected(
        double left,
        double right,
        double expected)
    {
        var code = $"""
                    fn scaleAndShift(value, factor, shift) (
                        return value * factor + shift
                    )

                    fn compose(a, b) (
                        let first = scaleAndShift(a, 2.0, b)
                        let second = scaleAndShift(b, -1.0, a)
                        return first + second
                    )

                    compose({left.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {right.ToString(System.Globalization.CultureInfo.InvariantCulture)})
                    """;

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(expected).Within(1e-9));
    }

    [TestCaseSource(nameof(AssociativityCases))]
    public void Execute_NestedFunctionCalls_AssociativityHoldsAcrossGeneratedTriples(double a, double b, double c)
    {
        var toCode = (double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var code = $"""
                    fn add(x, y) (
                        return x + y
                    )

                    let left = add(add({toCode(a)}, {toCode(b)}), {toCode(c)})
                    let right = add({toCode(a)}, add({toCode(b)}, {toCode(c)}))
                    left - right
                    """;

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(0).Within(1e-9));
    }
}
