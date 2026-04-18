namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class EqualityModulePipelineTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Equality_EqualConstants_ReturnTrue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("2 == 2", _modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsBool(r.Compiler), Is.True);
    }

    [Test]
    public void Equality_DifferentConstants_ReturnFalse()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("2 == 3", _modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsBool(r.Compiler), Is.False);
    }

    [Test]
    public void Equality_EqualExpressions_ReturnTrue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("(2 + 3) == 5", _modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsBool(r.Compiler), Is.True);
    }

    [Test]
    public void Equality_SymmetricOperands_ProduceSameResult()
    {
        using var h = new ModulePipelineTestHelper();
        var a = h.ExecuteBoth("2==3", _modules);
        var b = h.ExecuteBoth("3==2", _modules);
        ModulePipelineTestHelper.AssertParity(a.Compiler, a.Interpreter);
        ModulePipelineTestHelper.AssertParity(b.Compiler, b.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsBool(a.Compiler), Is.EqualTo(ModulePipelineTestHelper.AsBool(b.Compiler)));
    }

    [Test]
    public void Equality_UnknownIdentifierOperand_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining("2 == hi", _modules, string.Empty);
    }
}