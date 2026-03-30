namespace UniversalToolchain.Dialects.Tests.ModuleCoverage;

[TestFixture]
public class LocalVariablesOptimizerModulePipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    private static void AssertSameWithWithoutOptimizer(ModulePipelineTestHelper h, string code)
    {
        var disabled = h.ExecuteBoth(code, Modules);
        var enabled = h.ExecuteBoth(code, Modules, ["LocalVariablesOptimization"]);
        ModulePipelineTestHelper.AssertParity(disabled.Compiler, disabled.Interpreter);
        ModulePipelineTestHelper.AssertParity(enabled.Compiler, enabled.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(disabled.Compiler), Is.EqualTo(ModulePipelineTestHelper.AsNumber(enabled.Compiler)).Within(1e-9));
    }

    [Test] public void LocalVariablesOptimizer_EnabledAndDisabled_ProduceSameSimpleProgramResult(){using var h=new ModulePipelineTestHelper();AssertSameWithWithoutOptimizer(h,"let x=1; x=x+2; x");}
    [Test] public void LocalVariablesOptimizer_EnabledAndDisabled_ProduceSameBranchingProgramResult(){using var h=new ModulePipelineTestHelper();AssertSameWithWithoutOptimizer(h,"let x=1; if x==1 (x=5) else (x=9); x");}
    [Test] public void LocalVariablesOptimizer_EnabledAndDisabled_ProduceSameLoopProgramResult(){using var h=new ModulePipelineTestHelper();AssertSameWithWithoutOptimizer(h,"let s=0; let i=0; i=i+1; i=i+1; i=i+1; s=s+i; s");}
    [Test] public void LocalVariablesOptimizer_CompilerParity_IsPreservedWhenOptimizerEnabled(){using var h=new ModulePipelineTestHelper();var r=h.ExecuteBoth("let x=1; let y=2; x=x+y; x",Modules,["LocalVariablesOptimization"]);ModulePipelineTestHelper.AssertParity(r.Compiler,r.Interpreter);}    
    [Test] public void LocalVariablesOptimizer_RepeatedReadsAndWrites_DoNotChangeSemantics(){using var h=new ModulePipelineTestHelper();AssertSameWithWithoutOptimizer(h,"let x=0; x=x+1; x=x+1; x=x+1; x");}
    [Test] public void LocalVariablesOptimizer_ScopeAndLabelsScenario_RemainsSemanticallyStable(){using var h=new ModulePipelineTestHelper();AssertSameWithWithoutOptimizer(h,"let x=0; (let x=10; x); x=x+3; x");}
}
