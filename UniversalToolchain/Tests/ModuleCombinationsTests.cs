namespace Tests;

[TestFixture]
public class ModuleCombinationsTests : TestBase
{
    [Test]
    public void Execute_AllCoreModulesTogether_WorksCorrectly()
    {
        var code = @"
                let x = 10
                let y = (x + 5) * 2
                y = y - 3
                y / 2
            ";


        var result = ExecuteCode(code);


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(13.5).Within(1e-9));
    }

    [Test]
    public void Execute_MixedOperationsWithDifferentPrecedence_RespectsOrder()
    {
        var code = @"
                let a = 2 + 3 * 4
                let b = (2 + 3) * 4
                let c = a + b * 2
                c
            ";


        var result = ExecuteCode(code);


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(54).Within(1e-9));
    }
}