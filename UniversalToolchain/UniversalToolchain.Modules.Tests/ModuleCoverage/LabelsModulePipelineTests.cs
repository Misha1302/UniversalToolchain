namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class LabelsModulePipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Labels_ForwardJump_ReachesTargetLabel()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let x = 1
            goto @end
            x = 10
            @end: x
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(1));
    }

    [Test]
    public void Labels_BackwardJump_IsHandledDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let i = 0
            @loop: i = i + 1
            if i < 3 goto @loop
            i
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(3));
    }

    [Test]
    public void Labels_MissingLabel_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining(
            """
            goto @missing
            1
            """,
            Modules,
            "label");
    }

    [Test]
    public void Labels_DuplicateLabel_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining(
            """
            @x: 1
            @x: 2
            """,
            Modules,
            "label");
    }

    [Test]
    public void Labels_LabelOnlyProgram_DoesNotCorruptExecutionState()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("@x: 2", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(2));
    }
}
