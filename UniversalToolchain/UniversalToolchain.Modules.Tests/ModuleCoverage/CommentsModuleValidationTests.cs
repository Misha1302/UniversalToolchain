namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class CommentsModuleValidationTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Comments_UnterminatedBlockComment_ThrowsDeterministicLexerError()
    {
        using var helper = new ModulePipelineTestHelper();
        const string code = "2 /* bad";
        helper.AssertFails(code, Modules, "comment");

        var compilerError = Assert.Catch(() => helper.ExecuteCompiler(code, Modules));
        var interpreterError = Assert.Catch(() => helper.ExecuteInterpreter(code, Modules));
        var compilerMessage = compilerError!.ToString().ToLowerInvariant();
        var interpreterMessage = interpreterError!.ToString().ToLowerInvariant();

        Assert.That(compilerMessage.Contains("comment") || compilerMessage.Contains("unterminated"), Is.True);
        Assert.That(interpreterMessage.Contains("comment") || interpreterMessage.Contains("unterminated"), Is.True);
    }

    [Test]
    public void Comments_SingleLineComment_StillIgnoredByExecution()
    {
        using var helper = new ModulePipelineTestHelper();
        helper.ExecuteEquivalent("// comment\n2 + 3", "2 + 3", Modules);
    }

    [Test]
    public void Comments_BlockCommentOnlyInput_DoesNotCreatePhantomStatement()
    {
        using var helper = new ModulePipelineTestHelper();
        helper.ExecuteEquivalent("let x = 2\n/* comment-only line */\nx + 3", "let x = 2\nx + 3", Modules);
    }

    [Test]
    public void Comments_BlockCommentAroundExpression_PreservesExecutionSemantics()
    {
        using var helper = new ModulePipelineTestHelper();
        helper.ExecuteBoth("2 /* comment */ + 3", Modules);
        helper.ExecuteEquivalent("2 /* comment */ + 3", "2 + 3", Modules);
    }
}
