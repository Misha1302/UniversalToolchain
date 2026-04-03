namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class LoopsModulePipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Loops_SimpleLoop_AccumulatesExpectedResult()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let s = 0
            for (let i = 1) (i <= 4) (i = i + 1) (s = s + i)
            s
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(10));
    }

    [Test]
    public void Loops_ZeroIterationLoop_LeavesStateUnchanged()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let s = 5
            for (let i = 10) (i < 0) (i = i + 1) (s = s + 1)
            s
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void Loops_LoopWithCounter_StopsAtExpectedBoundary()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let i = 3
            i
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(3));
    }

    [Test]
    public void Loops_NestedLoops_ProduceExpectedAggregateResult()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let s = 0
            for (let i = 1) (i <= 2) (i = i + 1) (
                for (let j = 1) (j <= 2) (j = j + 1) (s = s + (i * j))
            )
            s
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(9));
    }

    [Test]
    public void Loops_MalformedLoop_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining(
            """
            for (let i = 0) (i < 3) (i = i + 1) i
            """,
            Modules,
            string.Empty);
    }
}