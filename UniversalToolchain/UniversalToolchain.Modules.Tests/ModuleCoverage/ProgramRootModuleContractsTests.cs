namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ProgramRootModuleContractsTests
{
    private static readonly string[] _modulesWithoutExplicitScopes = ["Arithmetic", "Identifier", "Numbers", "Whitespaces"];
    private static readonly string[] _modulesWithScopes = ["Arithmetic", "Identifier", "Numbers", "Scopes", "Variables", "Whitespaces"];

    [Test]
    public void ProgramRoot_WithoutExplicitScopesModule_StillTranslatesSimpleExpression()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("2 + 3", _modulesWithoutExplicitScopes);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void Parentheses_WithoutExplicitScopesModule_SucceedThroughArithmeticDependencyClosure()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("(2 + 3) * 4", _modulesWithoutExplicitScopes);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(20));
    }

    [Test]
    public void Parentheses_WithExplicitScopesModule_StillOverridePrecedence()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("(2 + 3) * 4", _modulesWithScopes);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(20));
    }
}
