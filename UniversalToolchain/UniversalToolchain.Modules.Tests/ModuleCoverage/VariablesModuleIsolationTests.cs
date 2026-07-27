namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class VariablesModuleIsolationTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Variables_LetDeclaration_AssignsInitialValue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let x = 10
            x
            """,
            _modules);
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
            _modules);
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
            _modules);
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
            _modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void Variables_UseBeforeDeclaration_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertCompilerAndInterpreterFailSameWay(
            """
            x
            let x = 1
            """,
            _modules);
    }

    [Test]
    public void Variables_AssignmentToUnknownVariable_FailsClosedDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining(
            """
            x = 1
            x
            """,
            _modules,
            "Unknown identifier 'x'");
    }
}