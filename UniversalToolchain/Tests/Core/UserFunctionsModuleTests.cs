using NumbersModule.Core;

namespace Tests.Core;

[TestFixture]
public class UserFunctionsModuleTests : TestBase
{
    private static IEnumerable<TestCaseData> FunctionCompositionCases()
    {
        yield return new TestCaseData(2.0, 3.0, 7.0).SetName("Execute_FunctionComposition_WithPositiveValues_ReturnsExpected");
        yield return new TestCaseData(-4.0, 5.5, -2.5).SetName("Execute_FunctionComposition_WithMixedSigns_ReturnsExpected");
        yield return new TestCaseData(0.0, 7.25, 7.25).SetName("Execute_FunctionComposition_WithZeroArgument_ReturnsExpected");
        yield return new TestCaseData(1.5, -2.5, 0.5).SetName("Execute_FunctionComposition_WithFractions_ReturnsExpected");
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

        Assert.That(() => ExecuteCode(code), Throws.Exception.With.Message.Contains("Неверное число аргументов"));
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
    public void Execute_FunctionWithConditionalExpression_ReturnsExpected()
    {
        const string code =
            """
            fn square(n) (
                return n * n
            )

            square(-10)
            """;

        var result = ExecuteCode(code);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(100).Within(1e-9));
    }

    [Test]
    public void Execute_FunctionParameter_ShadowsGlobalVariableWithoutMutatingOuterScope()
    {
        const string code =
            """
            let value = 10

            fn mutate(value) (
                return value + 5
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
                    fn compose(a, b) (
                        return a * 2.0 + b
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


    private static IEnumerable<TestCaseData> DeepCompositionCases()
    {
        yield return new TestCaseData(5.0, 2.0, 59.0)
            .SetName("Execute_DeepFunctionComposition_WithIntegerArguments_ReturnsExpected");
        yield return new TestCaseData(-1.5, 4.0, 3.25)
            .SetName("Execute_DeepFunctionComposition_WithMixedSigns_ReturnsExpected");
        yield return new TestCaseData(0.0, 3.25, 10.5625)
            .SetName("Execute_DeepFunctionComposition_WithFractionalArguments_ReturnsExpected");
    }

    [TestCaseSource(nameof(DeepCompositionCases))]
    public void Execute_DeepFunctionComposition_WithMultipleFunctionLayers_ReturnsExpected(
        double x,
        double y,
        double expected)
    {
        var toCode = (double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var code = $"""
                    fn add(a, b) (
                        return a + b
                    )

                    fn scale(v, factor) (
                        return v * factor
                    )

                    fn square(n) (
                        return n * n
                    )

                    fn pipeline(p, q) (
                        return add(square(add(p, q)), scale(p, 2.0))
                    )

                    pipeline({toCode(x)}, {toCode(y)})
                    """;

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(expected).Within(1e-9));
    }

    [Test]
    public void Execute_FunctionCallArguments_AsExpressionsIncludingFunctionCalls_ReturnsExpected()
    {
        const string code =
            """
            fn add(a, b) (
                return a + b
            )

            fn mul(a, b) (
                return a * b
            )

            fn adjust(base, delta) (
                return base - delta
            )

            adjust(add(1, mul(2, 3)) + add(4, 5), mul(add(1, 1), 2))
            """;

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(12).Within(1e-9));
    }

    [Test]
    public void Execute_FunctionParameterShadowing_AcrossNestedCalls_KeepsOuterStateStable()
    {
        const string code =
            """
            let value = 100

            fn addToValue(value, increment) (
                return value + increment
            )

            fn calc(value) (
                return addToValue(value, 5) + addToValue(1, value)
            )

            let computed = calc(10)
            value + computed
            """;

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(126).Within(1e-9));
    }

    [Test]
    public void Execute_FunctionsAddition_ReturnExpected()
    {
        var code =
            """
            fn add(x, y) (
                return x + y
            )

            add(1, 2) + add(3, 4)
            """;

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(10).Within(1e-9));
    }


    [Test]
    public void Execute_RecursiveFactorial_ReturnsExpected()
    {
        const string code =
            """
            fn factorial(n) (
                if (n <= 1) (
                    return 1
                )

                return n * factorial(n - 1)
            )

            factorial(5)
            """;

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(120).Within(1e-9));
    }

    [Test]
    public void Execute_RecursiveFibonacci_ReturnsExpected()
    {
        const string code =
            """
            fn fib(n) (
                if (n <= 1) (
                    return n
                )

                return fib(n - 1) + fib(n - 2)
            )

            fib(8)
            """;

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(21).Within(1e-9));
    }

    [Test]
    public void Execute_FunctionBodyWithMultipleStatementsAndLocalVariable_ReturnsExpected()
    {
        const string code =
            """
            fn calc(value) (
                let doubled = value * 2
                let shifted = doubled + 3
                return shifted
            )

            calc(7)
            """;

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(17).Within(1e-9));
    }

    [Test]
    public void Execute_MutualRecursion_IsEvenAndIsOdd_ReturnsExpected()
    {
        const string code =
            """
            fn isEven(n) (
                if (n == 0) (
                    return 1
                )
                return isOdd(n - 1)
            )

            fn isOdd(n) (
                if (n == 0) (
                    return 0
                )
                return isEven(n - 1)
            )

            isEven(10)
            """;

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(1).Within(1e-9));
    }

}