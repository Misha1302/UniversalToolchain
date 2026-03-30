namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class NumbersModulePipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;
    [Test] public void Numbers_IntegerLiteral_ExecutesToExpectedValue(){using var h=new ModulePipelineTestHelper();var r=h.ExecuteBoth("13",Modules);ModulePipelineTestHelper.AssertParity(r.Compiler,r.Interpreter);Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler),Is.EqualTo(13));}
    [Test] public void Numbers_NegativeLiteral_ExecutesToExpectedValue(){using var h=new ModulePipelineTestHelper();var r=h.ExecuteBoth("-13",Modules);ModulePipelineTestHelper.AssertParity(r.Compiler,r.Interpreter);Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler),Is.EqualTo(-13));}
    [Test] public void Numbers_ParenthesizedLiteral_ExecutesToExpectedValue(){using var h=new ModulePipelineTestHelper();var r=h.ExecuteBoth("(13)",Modules);ModulePipelineTestHelper.AssertParity(r.Compiler,r.Interpreter);Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler),Is.EqualTo(13));}
    [Test] public void Numbers_LeadingZeroLiteral_IsHandledDeterministically(){using var h=new ModulePipelineTestHelper();var r=h.ExecuteBoth("013",Modules);ModulePipelineTestHelper.AssertParity(r.Compiler,r.Interpreter);Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler),Is.EqualTo(13));}
    [Test] public void Numbers_InvalidNumericLiteral_FailsDeterministically(){using var h=new ModulePipelineTestHelper();h.AssertFails("1.2.3",Modules,"token");}
}
