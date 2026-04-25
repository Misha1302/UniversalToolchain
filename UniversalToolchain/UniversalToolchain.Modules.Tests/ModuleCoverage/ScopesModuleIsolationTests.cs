namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ScopesModuleIsolationTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Scopes_InnerScope_CanReadOuterVariable()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(
            """
            let x = 2
            (x + 3)
            """,
            _modules);
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
            _modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
    }

    [Test]
    public void Scopes_DuplicateLocalNameAcrossNestedScope_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertCompilerAndInterpreterFailSameWay(
            """
            let x = 2
            (let x = 7; x)
            """,
            _modules);
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
            _modules);
    }
}
