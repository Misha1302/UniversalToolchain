namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class InternalPreprocessorLexemesModulePipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void InternalPreprocessorLexemes_UserFacingUnsupportedToken_IsRejectedDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining("#![set x = 2] 2 + 3", Modules, "internal-only");
    }

    [Test]
    public void InternalPreprocessorLexemes_EquivalentComposedProgram_PreservesSemanticResult()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("2+3", "2 + 3", Modules);
    }

    [Test]
    public void InternalPreprocessorLexemes_FailureDiagnostics_AreStableAndUseful()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining("#![oops", Modules, "internal-only");
    }
}