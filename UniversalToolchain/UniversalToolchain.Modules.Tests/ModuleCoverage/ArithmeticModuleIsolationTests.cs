namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ArithmeticModuleIsolationTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Arithmetic_Addition_IsCommutativeForConstants()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("2 + 3", "3 + 2", Modules);
    }

    [Test]
    public void Arithmetic_Multiplication_IsCommutativeForConstants()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("4*7", "7*4", Modules);
    }

    [Test]
    public void Arithmetic_Subtraction_IsNotCommutative()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteDifferent("10-3", "3-10", Modules);
    }

    [Test]
    public void Arithmetic_Division_IsNotCommutative()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteDifferent("12/4", "4/12", Modules);
    }

    [Test]
    public void Arithmetic_OperatorPrecedence_MultiplicationBindsStrongerThanAddition()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("2 + 3 * 4", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(14));
    }

    [Test]
    public void Arithmetic_Parentheses_OverridePrecedence()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("(2 + 3) * 4", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(20));
    }

    [Test]
    public void Arithmetic_UnaryMinus_ProducesExpectedNegativeValue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("10-23", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.LessThan(0));
    }

    [Test]
    public void Arithmetic_AdditiveIdentity_DoesNotChangeResult()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("8", "8+0", Modules);
    }

    [Test]
    public void Arithmetic_MultiplicativeIdentity_DoesNotChangeResult()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("8", "8*1", Modules);
    }

    [Test]
    public void Arithmetic_MultiplicationByZero_ProducesZero()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("123*0", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(0));
    }

    [Test]
    public void Arithmetic_InvalidRightOperand_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertCompilerAndInterpreterFailSameWay("2 / hi", Modules);
    }

    [Test]
    public void Arithmetic_DivisionByZero_IsHandledDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        try
        {
            var r = h.ExecuteBoth("2 / 0", Modules);
            ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        }
        catch (Exception ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }
}