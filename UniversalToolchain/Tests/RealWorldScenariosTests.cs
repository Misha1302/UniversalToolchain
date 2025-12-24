namespace Tests;

[TestFixture]
public class RealWorldScenariosTests : TestBase
{
    [Test]
    public void Execute_SimpleCalculatorOperations_WorksCorrectly()
    {
        // Arrange
        var code = @"
                let price = 100
                let quantity = 3
                let discount = 0.1
                let total = price * quantity
                let finalPrice = total - (total * discount)
                finalPrice
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new ConditionsModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        // total = 100 * 3 = 300
        // discount = 300 * 0.1 = 30
        // finalPrice = 300 - 30 = 270
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(270).Within(1e-9));
    }

    [Test]
    public void Execute_MathematicalFormula_ComputesCorrectly()
    {
        // Arrange
        var code = @"
                let radius = 7
                let pi = 3.141592653589793
                let area = pi * radius * radius
                area
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new ConditionsModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        // area = π * r² = 3.141592653589793 * 49 ≈ 153.93804
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(Math.PI * 49).Within(1e-9));
    }

    [Test]
    public void Execute_ComplexBusinessLogic_WorksCorrectly()
    {
        // Arrange
        var code = @"
                let baseSalary = 1000
                let overtimeHours = 10
                let overtimeRate = 1.5
                let taxRate = 0.2
                
                let overtimePay = overtimeHours * (baseSalary / 160) * overtimeRate
                let grossSalary = baseSalary + overtimePay
                let taxAmount = grossSalary * taxRate
                let netSalary = grossSalary - taxAmount
                
                netSalary
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new ConditionsModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        // overtimePay = 10 * (1000/160) * 1.5 = 10 * 6.25 * 1.5 = 93.75
        // grossSalary = 1000 + 93.75 = 1093.75
        // taxAmount = 1093.75 * 0.2 = 218.75
        // netSalary = 1093.75 - 218.75 = 875
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(875).Within(1e-9));
    }
}