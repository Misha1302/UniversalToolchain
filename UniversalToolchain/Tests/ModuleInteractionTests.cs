namespace Tests;

[TestFixture]
public class ModuleInteractionTests : TestBase
{
    [Test]
    public void Execute_MultipleModuleIntegration_WorksSeamlessly()
    {
        // Arrange
        var code = @"                
                let baseValue = 10
                let pi = 3.141592653589793 
                
                if baseValue > 5 (
                    @calculation:
                    let calculated = baseValue * 2
                    
                    let rounded = Main.Round(calculated * pi)
                    
                    rounded
                )
                else 0
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
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

        // Assert
        // baseValue = 10, calculated = 20, rounded = round(20 * π) ≈ round(62.83185) = 63
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(63).Within(1e-9));
    }

    [Test]
    public void Execute_ComplexExpressionWithAllOperators_OrderOfOperationsCorrect()
    {
        // Arrange
        var code = @"
                let a = 2.0
                let b = 3.0
                let c = 4.0
                
                let result = Main.Pow(a, b) + 
                           (Main.Sqrt(c) * a) - 
                           Main.Log(b, a) / 
                           Main.Abs(a - b)
                
                result
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new CSharpInteropModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        // 2^3 = 8
        // sqrt(4) * 2 = 2 * 2 = 4
        // logₐ(b) = log₂(3) ≈ 1.5849625
        // |2-3| = 1
        // 8 + 4 - 1.5849625 / 1 = 12 - 1.5849625 = 10.4150375
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(10.4150375).Within(1e-7));
    }
}