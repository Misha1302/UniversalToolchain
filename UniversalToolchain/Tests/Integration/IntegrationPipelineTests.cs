namespace Tests.Integration;

[TestFixture]
public class IntegrationPipelineTests : TestBase
{
    [Test]
    public void Execute_FullPipelineWithAllModules_CompletesSuccessfully()
    {
        var code = @"
                let counter = 0
                let total = 0
                
                @outer:
                if counter >= 3 goto @done
                    let inner = 0
                    
                    @inner:
                    if inner >= 3 goto @inner_done
                        let x = counter * 10 + inner
                        let y = Main.Pow(x, 2)
                        
                        if y > 100
                            (total = total + Main.Sqrt(y))
                        else
                            (total = total + y)
                        
                        inner = inner + 1
                        goto @inner
                    @inner_done:
                    
                    counter = counter + 1
                    goto @outer
                @done:
                
                let result = Main.Round(total * 100) / 100
                result
            ";


        var result = ExecuteCode(code);


        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(191.0).Within(1e-7));
    }
}