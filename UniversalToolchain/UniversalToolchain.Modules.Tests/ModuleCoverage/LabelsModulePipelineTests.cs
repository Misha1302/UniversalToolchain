using LabelsModule.Core;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class LabelsModulePipelineTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;

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
            _modules);
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
            _modules);
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
            _modules,
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
            _modules,
            "label");
    }

    [Test]
    public void Labels_LabelOnlyProgram_DoesNotCorruptExecutionState()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("@x: 2", _modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(2));
    }

    [Test]
    public void Labels_SameSourceName_ProducesStableIdentityAcrossIndependentScopes()
    {
        var first = new LabelsSharedData();
        var second = new LabelsSharedData();

        var firstLoop = first.GetIdByName("@loop");
        var secondLoop = second.GetIdByName("@loop");
        var end = first.GetIdByName("@end");

        Assert.Multiple(() =>
        {
            Assert.That(firstLoop, Is.EqualTo(secondLoop));
            Assert.That(end, Is.Not.EqualTo(firstLoop));
            Assert.That(first.GetIdByName("@loop"), Is.EqualTo(firstLoop));
        });
    }
}