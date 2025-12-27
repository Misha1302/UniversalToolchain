namespace Tests;

[TestFixture]
public class StaticMethodTests : TestBase
{
    [Test]
    public void Execute_StaticMethodWithGenericParameters_HandlesGenericsCorrectly()
    {
        var code = @"
                let number = 100
                let logResult = Main.Log(number, 10)
                let sqrtResult = Main.Sqrt(number)
                logResult + sqrtResult
            ";


        var result = ExecuteCode(code);


        // log₁₀(100) = 2, sqrt(100) = 10, sum = 12
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(12).Within(1e-9));
    }

    [Test]
    public void Execute_StaticMethodWithVoidReturn_HandlesVoidCorrectly()
    {
        var code = @"
                Main.Print(2)
                Main.Get42()
            ";


        var result = ExecuteCode(code);


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(42).Within(1e-9));
    }
}