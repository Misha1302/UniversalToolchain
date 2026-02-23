using UserFunctionsModule;

namespace Tests;

[TestFixture]
public class UserFunctionsModuleTests : TestBase
{
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
}
