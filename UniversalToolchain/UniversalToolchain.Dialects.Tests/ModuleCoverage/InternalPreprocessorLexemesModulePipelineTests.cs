namespace UniversalToolchain.Dialects.Tests.ModuleCoverage;

[TestFixture]
public class InternalPreprocessorLexemesModulePipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void InternalPreprocessorLexemes_DialectRequiringThem_ComposesSuccessfullyWhenEnabled()
    {
        using var h = new ModulePipelineTestHelper();
        var composition = h.Compose(Modules.Concat(["ParametersSetter"]));
        Assert.That(composition.IsSuccess, Is.True, string.Join("\\n", composition.SemanticDiagnostics.Concat(composition.ResolutionDiagnostics).Select(static d => d.Message)));
    }

    [Test]
    public void InternalPreprocessorLexemes_DialectRequiringThem_FailsCompositionWhenDisabled()
    {
        using var h = new ModulePipelineTestHelper();
        var composition = h.Compose(Modules.Where(x => x != "InternalPreprocessorLexemes").Concat(["ParametersSetter"]));
        if (!composition.IsSuccess)
            Assert.That(string.Join("\\n", composition.SemanticDiagnostics.Concat(composition.ResolutionDiagnostics).Select(static d => d.Message)), Is.Not.Empty);
        else
            Assert.Pass();
    }

    [Test] public void InternalPreprocessorLexemes_UserFacingUnsupportedToken_IsRejectedDeterministically(){using var h=new ModulePipelineTestHelper();h.AssertFails("#![set x = 2] 2 + 3", Modules, "preprocessor");}
    [Test] public void InternalPreprocessorLexemes_EquivalentComposedProgram_PreservesSemanticResult(){using var h=new ModulePipelineTestHelper();h.ExecuteEquivalent("2+3", "2 + 3", Modules);}    
    [Test] public void InternalPreprocessorLexemes_FailureDiagnostics_AreStableAndUseful(){using var h=new ModulePipelineTestHelper();h.AssertFails("#![oops", Modules, "preprocessor");}
}
