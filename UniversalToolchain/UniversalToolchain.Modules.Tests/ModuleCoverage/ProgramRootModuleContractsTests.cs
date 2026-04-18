namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ProgramRootModuleContractsTests
{
    private static readonly string[] ModulesWithoutScopes = ["Arithmetic", "Identifier", "Numbers", "Variables", "Whitespaces"];
    private static readonly string[] ModulesWithScopes = ["Arithmetic", "Identifier", "Numbers", "Scopes", "Variables", "Whitespaces"];

    [Test]
    public void ProgramRoot_WithoutScopesModule_StillTranslatesSimpleExpression()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("2 + 3", ModulesWithoutScopes);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void Parentheses_WithoutScopesModule_FailDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertCompilerAndInterpreterFailSameWay("(2 + 3) * 4", ModulesWithoutScopes);
    }

    [Test]
    public void Parentheses_WithScopesModule_StillOverridePrecedence()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("(2 + 3) * 4", ModulesWithScopes);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(20));
    }
}
