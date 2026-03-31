namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class CSharpInteropModuleContractsTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void CSharpInterop_StaticMethodCall_ReturnsExpectedValue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("NumbersModule.Core.RealNumberImpl.Add(2, 5)", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(7));
    }

    [Test]
    public void CSharpInterop_InteropResult_CanParticipateInArithmeticExpression()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("NumbersModule.Core.RealNumberImpl.Add(2, 5) + 3", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(10));
    }

    [Test]
    public void CSharpInterop_MissingMethod_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFails("NumbersModule.Core.RealNumberImpl.Missing(2, 5)", Modules, "method", "member", "overload");
    }

    [Test]
    public void CSharpInterop_WrongArgumentCount_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFails("NumbersModule.Core.RealNumberImpl.Add(2)", Modules, "argument", "parameter", "count");
    }

    [Test]
    public void CSharpInterop_ModuleDisabled_SameProgramFailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFails("NumbersModule.Core.RealNumberImpl.Add(2, 5)", Modules.Where(x => x != "CSharpInterop"), "identifier", "variable", "not found", "unknown");
    }
}
