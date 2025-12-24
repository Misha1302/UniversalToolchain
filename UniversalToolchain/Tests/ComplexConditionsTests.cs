using ArithmeticModule;
using BasicCore;
using ConditionsModule;
using EqualityModule;
using IdentifierModule;
using LabelsModule;
using NumbersModule;
using ScopesModule;
using SemicolonAsNewLineModule;
using VariablesModule;
using WhitespacesModule;

namespace Tests;

[TestFixture]
public class ComplexConditionsTests : TestBase
{
    [Test]
    public void Execute_NestedIfElseConditions_ReturnsCorrectBranch()
    {
        // Arrange
        var code = @"
                let x = 15
                let result = 0
                
                if x > 10 (
                    if x < 20
                        result = 1
                    else
                        result = 2
                )
                else
                    result = 3
                
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
        // x=15 is between 10 and 20, so result should be 1
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(1).Within(1e-9));
    }

    [Test]
    public void Execute_ComplexBooleanLogic_CombinesMultipleConditions()
    {
        // Arrange
        var code = @"
                let a = 5
                let b = 10
                let c = 15
                let result = 0
                
                if (a < b) and (b < c)
                    result = 1
                else
                    result = 0
                
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
        // 5 < 10 and 10 < 15 is true, so result should be 1
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(1).Within(1e-9));
    }

    [Test]
    public void Execute_ElifChain_SelectsCorrectCondition()
    {
        // Arrange
        var code = @"
                let score = 85
                let grade = 0
                
                if score >= 90
                    grade = 5
                elif score >= 80
                    grade = 4
                elif score >= 70
                    grade = 3
                elif score >= 60
                    grade = 2
                else
                    grade = 1
                
                grade
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new SemicolonAsNewLineModuleImpl(),
            new ArithmeticModuleImpl(),
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
        // score=85 should give grade=4
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(4).Within(1e-9));
    }
}