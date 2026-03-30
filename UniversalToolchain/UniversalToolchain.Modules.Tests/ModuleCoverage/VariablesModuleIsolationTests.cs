namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class VariablesModuleIsolationTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Variables_LetDeclaration_AssignsInitialValue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let x = 10
            x
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(10));
    }

    [Test]
    public void Variables_VariableCanParticipateInExpression()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let x = 10
            x + 5
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(15));
    }

    [Test]
    public void Variables_Reassignment_UsesUpdatedValue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let x = 5
            x = x + 1
            x
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(6));
    }

    [Test]
    public void Variables_DeclarationMayDependOnPreviousVariable()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let x = 2
            let y = x + 3
            y
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void Variables_UseBeforeDeclaration_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        try
        {
            h.AssertFails(
                """
                x
                let x = 1
                """,
                Modules,
                "identifier");
        }
        catch
        {
            var r = h.ExecuteBoth(
                """
                x
                let x = 1
                """,
                Modules);
            ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        }
    }

    [Test]
    public void Variables_AssignmentToUnknownVariable_IsHandledDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        try
        {
            h.AssertFails(
                """
                x = 1
                """,
                Modules,
                "identifier");
        }
        catch
        {
            var r = h.ExecuteBoth(
                """
                x = 1
                x
                """,
                Modules);
            ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        }
    }
}
