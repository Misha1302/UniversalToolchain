namespace UniversalToolchain.Dialects.Tests.ModuleCoverage;

[TestFixture]
public class IdentifierModulePipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;
    [Test] public void Identifier_SimpleIdentifier_CanBeDeclaredAndRead(){using var h=new ModulePipelineTestHelper();var r=h.ExecuteBoth("let x = 2; x",Modules);ModulePipelineTestHelper.AssertParity(r.Compiler,r.Interpreter);Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler),Is.EqualTo(2));}
    [Test] public void Identifier_IdentifierWithDigits_CanBeDeclaredAndRead(){using var h=new ModulePipelineTestHelper();var r=h.ExecuteBoth("let x2 = 5; x2",Modules);ModulePipelineTestHelper.AssertParity(r.Compiler,r.Interpreter);Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler),Is.EqualTo(5));}
    [Test] public void Identifier_UnknownIdentifier_FailsDeterministically(){using var h=new ModulePipelineTestHelper();h.AssertFails("hi",Modules,"identifier");}
    [Test] public void Identifier_ReservedKeywordAsIdentifier_IsRejectedDeterministically(){using var h=new ModulePipelineTestHelper();h.AssertFails("let if = 2",Modules,"token");}
    [Test] public void Identifier_CaseSensitivity_IsHandledDeterministically(){using var h=new ModulePipelineTestHelper();h.AssertFails("let x = 2; X",Modules,"identifier");}
}
