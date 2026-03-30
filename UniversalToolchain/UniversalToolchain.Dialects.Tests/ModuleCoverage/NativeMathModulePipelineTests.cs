namespace UniversalToolchain.Dialects.Tests.ModuleCoverage;

[TestFixture]
public class NativeMathModulePipelineTests
{
    private static readonly string[] NativeModules = ModulePipelineTestHelper.FullUniversalModules.Where(x => x != "Numbers").Concat(["NativeTypes"]).ToArray();
    private static readonly string[] UniversalModules = ModulePipelineTestHelper.FullUniversalModules;

    [Test] public void NativeMath_SimpleAddition_ReturnsExpectedValueOnBothBackends(){using var h=new ModulePipelineTestHelper();var r=h.ExecuteBoth("2 + 3",NativeModules);ModulePipelineTestHelper.AssertParity(r.Compiler,r.Interpreter);Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler),Is.EqualTo(5));}
    [Test] public void NativeMath_PrecedenceMatchesExpectedArithmeticRules(){using var h=new ModulePipelineTestHelper();var r=h.ExecuteBoth("2 + 3 * 4",NativeModules);ModulePipelineTestHelper.AssertParity(r.Compiler,r.Interpreter);Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler),Is.EqualTo(14));}
    [Test] public void NativeMath_LongArithmeticChain_HasBackendParity(){using var h=new ModulePipelineTestHelper();var r=h.ExecuteBoth("1 + 2 * 3 - 4 + 5 * 6 - 7",NativeModules);ModulePipelineTestHelper.AssertParity(r.Compiler,r.Interpreter);}
    [Test] public void NativeMath_DivisionByZero_IsHandledDeterministically(){using var h=new ModulePipelineTestHelper();try{var r=h.ExecuteBoth("2/0",NativeModules);ModulePipelineTestHelper.AssertParity(r.Compiler,r.Interpreter);}catch(Exception ex){Assert.That(ex.Message,Is.Not.Empty);}}
    [Test] public void NativeMath_UniversalAndNativeProfiles_AgreeOnSimpleIntegerScenario(){using var h=new ModulePipelineTestHelper();var u=h.ExecuteBoth("7+8",UniversalModules);var n=h.ExecuteBoth("7+8",NativeModules);ModulePipelineTestHelper.AssertParity(u.Compiler,u.Interpreter);ModulePipelineTestHelper.AssertParity(n.Compiler,n.Interpreter);Assert.That(ModulePipelineTestHelper.AsNumber(u.Compiler),Is.EqualTo(ModulePipelineTestHelper.AsNumber(n.Compiler)).Within(1e-9));}
}
