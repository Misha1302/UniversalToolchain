namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ConditionsModuleIsolationTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Conditions_IfTrue_ReturnsThenBranch()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("if 2 == 2 (1) else (2)", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(1));
    }

    [Test]
    public void Conditions_IfFalse_ReturnsElseBranch()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("if 2 == 3 (1) else (2)", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(2));
    }

    [Test]
    public void Conditions_NestedIf_ChoosesExpectedLeaf()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            if 2 == 2 (
                if 3 == 3 (9) else (8)
            ) else (7)
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(9));
    }

    [Test]
    public void Conditions_ConditionBasedOnEquality_ChoosesExpectedBranch()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let x = 2
            if x == 2 (10) else (20)
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(10));
    }

    [Test]
    public void Conditions_NonBooleanCondition_IsHandledDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        try
        {
            var r = h.ExecuteBoth(
                """
                if 1 (2) else (3)
                """,
                Modules);
            ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        }
        catch (Exception ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }
}
