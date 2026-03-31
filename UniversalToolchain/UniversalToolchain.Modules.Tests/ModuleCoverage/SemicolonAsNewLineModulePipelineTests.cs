namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class SemicolonAsNewLineModulePipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void SemicolonAsNewLine_NewlineSeparatedStatements_AreEquivalentToSemicolonSeparatedStatements()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("let x = 2\nx + 3", "let x = 2; x + 3", Modules);
    }

    [Test]
    public void SemicolonAsNewLine_MixedSemicolonsAndNewlines_PreserveExecutionOrder()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("let x = 1;\n x = x + 1\n x + 3", "let x = 1; x = x + 1; x + 3", Modules);
    }

    [Test]
    public void SemicolonAsNewLine_TrailingSemicolon_DoesNotChangeResult()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("let x = 2; x + 1;", "let x = 2; x + 1", Modules);
    }

    [Test]
    public void SemicolonAsNewLine_EmptyStatementBetweenSeparators_IsHandledDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("let x = 2;; x + 1", "let x = 2; x + 1", Modules);
    }

    [Test]
    public void SemicolonAsNewLine_ModuleDisabled_NewlineSeparatedProgramFailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFails("let x = 2\nx + 3", Modules.Where(x => x != "SemicolonAsNewLine"), "token", "parser", "invalid");
    }
}