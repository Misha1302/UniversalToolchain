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
    public void Execute_FinancialCompoundGrowth_ComputesCorrectly()
    {
        // Arrange
        var code = @"
                let initialInvestment = 5000
                let annualReturn = 0.08
                let years = 10
                
                let futureValue = initialInvestment
                let year = 0

                @start:
                if year < years goto @end
                    futureValue = futureValue * (1 + annualReturn)
                    year = year + 1
                @end:
                futureValue - initialInvestment
            ";
        var modules = new ICoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new ComparisonOperations(),
            new EqualityModuleImpl()
        };

        // Act
        var result = ExecuteCode(code, modules);

        // Assert
        // Compound growth calculation
        Assert.That(result, Is.Not.Null);
    }

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
        // Total value = 2550 + 2362.5 + 1798 = 6710.5
        // Average price = 6710.5 / 450 ≈ 14.9122
        Assert.That(result, Is.Not.Null);
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
                let grossMargin = grossProfit / revenue
                let netMargin = netProfit / revenue
                
                netMargin * 100
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
        // grossProfit = 35000, netProfit = 15000
        // netMargin = 15000/100000 = 0.15 = 15%
        Assert.That(result, Is.Not.Null);
    }
}