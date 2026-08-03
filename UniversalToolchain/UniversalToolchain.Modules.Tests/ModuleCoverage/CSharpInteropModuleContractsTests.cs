namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class CSharpInteropModuleContractsTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;
    private const string ContractHost =
        "UniversalToolchain.Modules.Tests.ModuleCoverage.CSharpInteropContractHost";

    [Test]
    public void CSharpInterop_StaticMethodCall_ReturnsExpectedValue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth($"{ContractHost}.Add(2, 5)", _modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(7));
    }

    [Test]
    public void CSharpInterop_InteropResult_CanParticipateInArithmeticExpression()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth($"{ContractHost}.Add(2, 5) + 3", _modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(10));
    }

    [Test]
    public void CSharpInterop_MissingMethod_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining($"{ContractHost}.Missing(2, 5)", _modules, "method");
    }

    [Test]
    public void CSharpInterop_WrongArgumentCount_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFails($"{ContractHost}.Add(2)", _modules);
    }

    [Test]
    public void CSharpInterop_ModuleDisabled_SameProgramFailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining($"{ContractHost}.Add(2, 5)", _modules.Where(x => x != "CSharpInterop"), string.Empty);
    }
}

public static class CSharpInteropContractHost
{
    public static double Add(double left, double right) => left + right;
}