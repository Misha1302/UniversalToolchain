namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class InternalPreprocessorLexemesModulePipelineTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void InternalPreprocessorLexemes_UserFacingUnsupportedToken_IsRejectedDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining("#![set x = 2] 2 + 3", _modules, "internal-only");
    }

    [Test]
    public void InternalPreprocessorLexemes_EquivalentComposedProgram_PreservesSemanticResult()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("2+3", "2 + 3", _modules);
    }

    [Test]
    public void InternalPreprocessorLexemes_FailureDiagnostics_AreStableAndUseful()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining("#![oops", _modules, "internal-only");
    }
}