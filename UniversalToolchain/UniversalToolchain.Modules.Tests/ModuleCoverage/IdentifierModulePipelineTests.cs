namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class IdentifierModulePipelineTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Identifier_SimpleIdentifier_CanBeDeclaredAndRead()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("let x = 2; x", _modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(2));
    }

    [Test]
    public void Identifier_IdentifierWithDigits_CanBeDeclaredAndRead()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("let x2 = 5; x2", _modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void Identifier_UnknownIdentifier_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining("hi", _modules, string.Empty);
    }

    [Test]
    public void Identifier_ReservedKeywordAsIdentifier_IsRejectedDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining("let if = 2", _modules, string.Empty);
    }

    [Test]
    public void Identifier_CaseSensitivity_IsHandledDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining("let x = 2; X", _modules, string.Empty);
    }
}