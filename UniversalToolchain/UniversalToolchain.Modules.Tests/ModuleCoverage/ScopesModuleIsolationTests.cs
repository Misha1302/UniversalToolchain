namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ScopesModuleIsolationTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Scopes_InnerScope_CanReadOuterVariable()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let x = 2
            (x + 3)
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void Scopes_InnerScopeVariableOutsideAccess_IsHandledDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            (let x = 2)
            x
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
    }

    [Test]
    public void Scopes_Shadowing_UsesInnerVariableInsideScope()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let x = 2
            (let x = 7; x)
            """,
            Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(7));
    }

    [Test]
    public void Scopes_EmptyScope_DoesNotChangeOuterState()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent(
            """
            let x = 2
            ()
            x + 1
            """,
            """
            let x = 2
            x + 1
            """,
            Modules);
    }
}
