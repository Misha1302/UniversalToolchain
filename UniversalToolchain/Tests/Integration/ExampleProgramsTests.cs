namespace Tests;

[TestFixture]
public class ExampleProgramsTests : TestBase
{
    [Test]
    public void Execute_CompleteExampleLikeInProgramCs_WorksCorrectly()
    {
        var code = @"
                let a = 10
                let b = 20
                let c = a * b - 5
                b = b + 1
                c = c - 15
                c
            ";


        var result = ExecuteCode(code);


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(180).Within(1e-9));
    }
}