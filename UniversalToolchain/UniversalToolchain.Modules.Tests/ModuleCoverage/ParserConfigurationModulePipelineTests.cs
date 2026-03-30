namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ParserConfigurationModulePipelineTests
{
    [Test]
    public void ParserConfiguration_ValidConfiguration_ComposesAndExecutesProgram()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("2+3", ModulePipelineTestHelper.FullUniversalModules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void ParserConfiguration_UnknownConfigurationEntry_FailsCompositionDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        var composition = h.Compose(ModulePipelineTestHelper.FullUniversalModules.Concat(["ParserConfiguration"]));
        Assert.That(composition.IsSuccess, Is.False);
        Assert.That(string.Join("\\n", composition.SemanticDiagnostics.Concat(composition.ResolutionDiagnostics).Select(static d => d.Message)), Does.Contain("ParserConfiguration"));
    }

    [Test]
    public void ParserConfiguration_ConflictingConfigurationEntries_AreHandledDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        var one = h.Compose(ModulePipelineTestHelper.FullUniversalModules.Concat(["ParserConfiguration", "ParserConfiguration"]));
        Assert.That(one.IsSuccess, Is.False);
        Assert.That(string.Join("\\n", one.SemanticDiagnostics.Concat(one.ResolutionDiagnostics).Select(static d => d.Message)), Does.Contain("duplicate").IgnoreCase);
    }

    [Test]
    public void ParserConfiguration_ConfigurationChangesAcceptedSurfaceSyntaxAsIntended()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFails("2 +", ModulePipelineTestHelper.FullUniversalModules, "token");
    }

    [Test]
    public void ParserConfiguration_BackendParity_IsPreservedUnderValidConfiguration()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("let x=2; x+3", ModulePipelineTestHelper.FullUniversalModules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
    }
}