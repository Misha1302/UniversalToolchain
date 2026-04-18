namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class LocalVariablesOptimizerSemanticPreservationTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;

    private static void AssertSameWithWithoutOptimizer(ModulePipelineTestHelper h, string code)
    {
        var disabled = h.ExecuteBoth(code, _modules);
        var enabled = h.ExecuteBoth(code, _modules, ["LocalVariablesOptimization"]);
        ModulePipelineTestHelper.AssertParity(disabled.Compiler, disabled.Interpreter);
        ModulePipelineTestHelper.AssertParity(enabled.Compiler, enabled.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(disabled.Compiler), Is.EqualTo(ModulePipelineTestHelper.AsNumber(enabled.Compiler)).Within(1e-9));
    }

    [Test]
    public void LocalVariablesOptimizer_EnabledAndDisabled_ProduceSameSimpleProgramResult()
    {
        using var h = new ModulePipelineTestHelper();
        AssertSameWithWithoutOptimizer(h, "let x=1; x=x+2; x");
    }

    [Test]
    public void LocalVariablesOptimizer_EnabledAndDisabled_ProduceSameBranchingProgramResult()
    {
        using var h = new ModulePipelineTestHelper();
        AssertSameWithWithoutOptimizer(h, "let x=1; if x==1 (x=5) else (x=9); x");
    }

    [Test]
    public void LocalVariablesOptimizer_EnabledAndDisabled_ProduceSameLoopProgramResult()
    {
        using var h = new ModulePipelineTestHelper();
        AssertSameWithWithoutOptimizer(h, "let s=0; let i=0; i=i+1; i=i+1; i=i+1; s=s+i; s");
    }

    [Test]
    public void LocalVariablesOptimizer_CompilerParity_IsPreservedWhenOptimizerEnabled()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("let x=1; let y=2; x=x+y; x", _modules, ["LocalVariablesOptimization"]);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
    }

    [Test]
    public void LocalVariablesOptimizer_RepeatedReadsAndWrites_DoNotChangeSemantics()
    {
        using var h = new ModulePipelineTestHelper();
        AssertSameWithWithoutOptimizer(h, "let x=0; x=x+1; x=x+1; x=x+1; x");
    }

    [Test]
    public void LocalVariablesOptimizer_ScopeAndLabelsScenario_RemainsSemanticallyStable()
    {
        using var h = new ModulePipelineTestHelper();
        AssertSameWithWithoutOptimizer(h, "let x=0; if 1==1 (let x=10; x=10) else (x=x); x=x+3; x");
    }

    [Test]
    public void LocalVariablesOptimizer_ShadowingWithLabel_RemainsSemanticallyStable()
    {
        using var h = new ModulePipelineTestHelper();
        AssertSameWithWithoutOptimizer(h, "let x=0; @next: if x==0 (let x=7; x=8) else (x=x+1); if x<2 goto @next; x");
    }

    [Test]
    public void LocalVariablesOptimizer_ForwardJumpWithLocalWriteRead_RemainsSemanticallyStable()
    {
        using var h = new ModulePipelineTestHelper();
        AssertSameWithWithoutOptimizer(h, "let x=1; goto @skip; x=9; @skip: x=x+2; x");
    }

    [Test]
    public void LocalVariablesOptimizer_NestedScopeWithPostScopeAssignment_RemainsSemanticallyStable()
    {
        using var h = new ModulePipelineTestHelper();
        AssertSameWithWithoutOptimizer(h, "let x=1; if 1==1 (let y=x+2; x=x+y) else (x=x); x=x+5; x");
    }
}