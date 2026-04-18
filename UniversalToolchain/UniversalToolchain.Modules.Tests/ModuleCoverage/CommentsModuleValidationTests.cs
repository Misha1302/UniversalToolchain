namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class CommentsModuleValidationTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Comments_UnterminatedBlockComment_ThrowsDeterministicLexerError()
    {
        using var helper = new ModulePipelineTestHelper();
        const string code = "2 /* bad";
        helper.AssertFailsContaining(code, _modules, "comment");

        var compilerError = Assert.Catch(() => helper.ExecuteCompiler(code, _modules));
        var interpreterError = Assert.Catch(() => helper.ExecuteInterpreter(code, _modules));
        var compilerMessage = compilerError!.ToString().ToLowerInvariant();
        var interpreterMessage = interpreterError!.ToString().ToLowerInvariant();

        Assert.That(compilerMessage.Contains("comment") || compilerMessage.Contains("unterminated"), Is.True);
        Assert.That(interpreterMessage.Contains("comment") || interpreterMessage.Contains("unterminated"), Is.True);
    }

    [Test]
    public void Comments_SingleLineComment_StillIgnoredByExecution()
    {
        using var helper = new ModulePipelineTestHelper();
        helper.ExecuteEquivalent("// comment\n2 + 3", "2 + 3", _modules);
    }

    [Test]
    public void Comments_SingleLineCommentContainingBlockOpen_DoesNotThrow()
    {
        using var helper = new ModulePipelineTestHelper();
        helper.ExecuteEquivalent("// /*\n2 + 3", "2 + 3", _modules);
    }

    [Test]
    public void Comments_BlockCommentOpenedInsideSingleLineComment_DoesNotAffectNextLine()
    {
        using var helper = new ModulePipelineTestHelper();
        helper.ExecuteEquivalent("// before /*\n2 + 3", "2 + 3", _modules);
    }

    [Test]
    public void Comments_BlockCommentOnlyInput_DoesNotCreatePhantomStatement()
    {
        using var helper = new ModulePipelineTestHelper();
        helper.ExecuteEquivalent("let x = 2\n/* comment-only line */\nx + 3", "let x = 2\nx + 3", _modules);
    }

    [Test]
    public void Comments_BlockCommentAroundExpression_PreservesExecutionSemantics()
    {
        using var helper = new ModulePipelineTestHelper();
        helper.ExecuteBoth("2 /* comment */ + 3", _modules);
        helper.ExecuteEquivalent("2 /* comment */ + 3", "2 + 3", _modules);
    }
}