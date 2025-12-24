using ArithmeticModule;
using BasicCore;
using ConditionsModule;
using EqualityModule;
using IdentifierModule;
using NumbersModule;
using ScopesModule;
using VariablesModule;
using WhitespacesModule;

namespace Tests;

[TestFixture]
public class BusinessCalculationsTests : TestBase
{
    [Test]
    public void Execute_InventoryValueCalculation_ComputesCorrectly()
    {
        // Arrange
        var code = @"
                let item1Price = 25.5
                let item1Quantity = 100
                let item2Price = 15.75
                let item2Quantity = 150
                let item3Price = 8.99
                let item3Quantity = 200
                
                let totalValue = item1Price * item1Quantity + 
                                item2Price * item2Quantity + 
                                item3Price * item3Quantity
                
                let averagePrice = totalValue / (item1Quantity + item2Quantity + item3Quantity)
                averagePrice
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
        // Total value = 2550 + 2362.5 + 1798 = 6710.5
        // Average price = 6710.5 / 450 ≈ 14.912222...
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(6710.5 / 450).Within(1e-9));
    }

    [Test]
    public void Execute_ProfitMarginCalculation_ComputesCorrectly()
    {
        // Arrange
        var code = @"
                let revenue = 100000
                let costOfGoods = 65000
                let operatingExpenses = 20000
                
                let grossProfit = revenue - costOfGoods
                let netProfit = grossProfit - operatingExpenses
                let netMargin = netProfit / revenue
                
                netMargin * 100
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
        // grossProfit = 35000, netProfit = 15000
        // netMargin = 15000/100000 = 0.15 = 15%
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(15).Within(1e-9));
    }
}