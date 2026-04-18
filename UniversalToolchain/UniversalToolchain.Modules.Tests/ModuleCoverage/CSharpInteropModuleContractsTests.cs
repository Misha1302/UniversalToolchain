namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class CSharpInteropModuleContractsTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void CSharpInterop_StaticMethodCall_ReturnsExpectedValue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("NumbersModule.Core.RealNumberImpl.Add(2, 5)", _modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(7));
    }

    [Test]
    public void CSharpInterop_InteropResult_CanParticipateInArithmeticExpression()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("NumbersModule.Core.RealNumberImpl.Add(2, 5) + 3", _modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(10));
    }

    [Test]
    public void CSharpInterop_MissingMethod_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining("NumbersModule.Core.RealNumberImpl.Missing(2, 5)", _modules, "method");
    }

    [Test]
    public void CSharpInterop_WrongArgumentCount_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFails("NumbersModule.Core.RealNumberImpl.Add(2)", _modules);
    }

    [Test]
    public void CSharpInterop_ModuleDisabled_SameProgramFailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining("NumbersModule.Core.RealNumberImpl.Add(2, 5)", _modules.Where(x => x != "CSharpInterop"), string.Empty);
    }
}