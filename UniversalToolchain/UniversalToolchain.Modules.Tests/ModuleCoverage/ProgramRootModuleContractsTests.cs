namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ProgramRootModuleContractsTests
{
    private static readonly string[] _modulesWithoutScopes = ["Arithmetic", "Identifier", "Numbers", "Whitespaces"];
    private static readonly string[] _modulesWithScopes = ["Arithmetic", "Identifier", "Numbers", "Scopes", "Variables", "Whitespaces"];

    [Test]
    public void ProgramRoot_WithoutScopesModule_StillTranslatesSimpleExpression()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("2 + 3", _modulesWithoutScopes);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void Parentheses_WithoutScopesModule_FailDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertCompilerAndInterpreterFailSameWay("(2 + 3) * 4", _modulesWithoutScopes);
    }

    [Test]
    public void Parentheses_WithScopesModule_StillOverridePrecedence()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("(2 + 3) * 4", _modulesWithScopes);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(20));
    }
}
