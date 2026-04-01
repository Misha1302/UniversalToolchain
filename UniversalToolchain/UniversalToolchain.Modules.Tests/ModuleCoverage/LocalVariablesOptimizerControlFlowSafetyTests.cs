namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class LocalVariablesOptimizerControlFlowSafetyTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    private static (object? Compiler, object? Interpreter) ExecuteWithOptimizer(ModulePipelineTestHelper helper, string code)
        => helper.ExecuteBoth(code, Modules, ["LocalVariablesOptimization"]);

    private static (object? Compiler, object? Interpreter) ExecuteWithoutOptimizer(ModulePipelineTestHelper helper, string code)
        => helper.ExecuteBoth(code, Modules);

    private static void AssertEnabledMatchesDisabled(ModulePipelineTestHelper helper, string code)
    {
        var disabled = ExecuteWithoutOptimizer(helper, code);
        var enabled = ExecuteWithOptimizer(helper, code);

        ModulePipelineTestHelper.AssertParity(disabled.Compiler, disabled.Interpreter);
        ModulePipelineTestHelper.AssertParity(enabled.Compiler, enabled.Interpreter);
        ModulePipelineTestHelper.AssertParity(disabled.Compiler, enabled.Compiler);
        ModulePipelineTestHelper.AssertParity(disabled.Interpreter, enabled.Interpreter);
    }

    [Test]
    public void LocalVariablesOptimizer_RemovesRedundantRoundtrip_InStraightLineCode()
    {
        using var helper = new ModulePipelineTestHelper();
        const string code = "let x=1; let y=2; x=x+y; x=x+3; x";

        AssertEnabledMatchesDisabled(helper, code);
    }

    [Test]
    public void LocalVariablesOptimizer_DoesNotRewrite_WhenTargetStartsAtBranchLabel()
    {
        using var helper = new ModulePipelineTestHelper();
        const string code = "let x=1; goto @entry; x=99; @entry: x=x+3; x";

        AssertEnabledMatchesDisabled(helper, code);
    }

    [Test]
    public void LocalVariablesOptimizer_DoesNotRewrite_AcrossControlFlowJoinPoint()
    {
        using var helper = new ModulePipelineTestHelper();
        const string code = "let x=0; if 1==1 (x=10) else (x=20); x=x+1; x";

        AssertEnabledMatchesDisabled(helper, code);
    }

    [Test]
    public void LocalVariablesOptimizer_DoesNotRewrite_WhenBackwardBranchExists()
    {
        using var helper = new ModulePipelineTestHelper();
        const string code = "let i=0; let s=0; @loop: i=i+1; s=s+i; if i<3 goto @loop; s=s+1; s";

        AssertEnabledMatchesDisabled(helper, code);
    }

    [Test]
    public void LocalVariablesOptimizer_DoesNotRewrite_AcrossScopeSensitiveInstruction()
    {
        using var helper = new ModulePipelineTestHelper();
        const string code = "let x=2; if 1==1 (let x=10; x=11) else (x=x); x=x+3; x";

        AssertEnabledMatchesDisabled(helper, code);
    }

    [Test]
    public void LocalVariablesOptimizer_DoesNotRewrite_AcrossIntrinsicBranchInstruction()
    {
        using var helper = new ModulePipelineTestHelper();
        const string code = "let x=1; if x==1 (x=7) else (x=9); x=x+2; x";

        AssertEnabledMatchesDisabled(helper, code);
    }

    [Test]
    public void LocalVariablesOptimizer_NoOp_WhenBackendDoesNotSupportRequiredIntrinsics()
    {
        using var helper = new ModulePipelineTestHelper();
        const string code = "1+2+3";

        AssertEnabledMatchesDisabled(helper, code);
    }

    [Test]
    public void LocalVariablesOptimizer_PreservesSemantics_OnIfElseProgram()
    {
        using var helper = new ModulePipelineTestHelper();
        const string code = "let x=1; if x==1 (x=5) else (x=9); x";

        AssertEnabledMatchesDisabled(helper, code);
    }

    [Test]
    public void LocalVariablesOptimizer_PreservesSemantics_OnLoopProgram()
    {
        using var helper = new ModulePipelineTestHelper();
        const string code = "let i=0; let s=0; @loop: i=i+1; s=s+i; if i<4 goto @loop; s=s+2; s";

        AssertEnabledMatchesDisabled(helper, code);
    }

    [Test]
    public void LocalVariablesOptimizer_PreservesSemantics_OnNestedControlFlowProgram()
    {
        using var helper = new ModulePipelineTestHelper();
        const string code = "let x=0; let i=0; @loop: i=i+1; if i<3 (if x==0 (x=5) else (x=x+2)) else (x=x+1); if i<4 goto @loop; x=x+3; x";

        AssertEnabledMatchesDisabled(helper, code);
    }
}
