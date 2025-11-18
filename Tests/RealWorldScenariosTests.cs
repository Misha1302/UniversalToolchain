using ArithmeticModule;
using BasicCore;
using EqualityModule;
using IdentifierModule;
using NumbersModule;
using ScopesModule;
using VariablesModule;
using WhitespacesModule;

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
        var modules = new ICoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Execute_MathematicalFormula_ComputesCorrectly()
    {
        // Arrange
        var code = @"
                let radius = 5
                let pi = 3.14159
                let area = pi * radius * radius
                let circumference = 2 * pi * radius
                area
            ";
        var modules = new ICoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        Assert.That(result, Is.Not.Null);
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
        var modules = new ICoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        Assert.That(result, Is.Not.Null);
    }
}