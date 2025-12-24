namespace Tests;

[TestFixture]
public class IntegrationPipelineTests : TestBase
{
    [Test]
    public void Execute_FullPipelineWithAllModules_CompletesSuccessfully()
    {
        // Arrange
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
                            total = total + Main.Sqrt(y)
                        else
                            total = total + y
                        
                        inner = inner + 1
                        goto @inner
                    @inner_done:
                    
                    counter = counter + 1
                    goto @outer
                @done:
                
                let result = Main.Round(total * 100) / 100
                result
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new SemicolonAsNewLineModuleImpl(),
            new ArithmeticModuleImpl(),
            new CSharpInteropModuleImpl(),
            new LabelsModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new ConditionsModuleImpl(),
            new ComparisonOperations(),
            new BooleanOperations()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert - проверяем, что пайплайн работает без ошибок
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.GreaterThan(0));
    }
}