using StringsModule.Core;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class StringsModulePipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Strings_LiteralAndEscapes_AreDecodedDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("\"line1\\nline2\\t\\\"x\\\"\\\\\"", Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsString(result.Compiler), Is.EqualTo("line1\nline2\t\"x\"\\"));
    }

    [Test]
    public void Strings_ConcatenationAndComparison_WorkWithVariables()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth(
            """
            let a = "foo"
            let b: string = "bar"
            let c = a + b
            if c == "foobar" (
                c
            ) else (
                "bad"
            )
            """,
            Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsString(result.Compiler), Is.EqualTo("foobar"));
    }

    [Test]
    public void Strings_Literals_DoNotConflictWithCommentPatterns()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("\"http://x\" == \"http://x\"", Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsBool(result.Compiler), Is.True);
    }

    [Test]
    public void Strings_UnsupportedArithmeticOperator_ThrowsReadableError()
    {
        using var h = new ModulePipelineTestHelper();

        var ex = Assert.Throws<NotSupportedException>(() => h.ExecuteCompiler("\"a\" - \"b\"", Modules));

        Assert.That(ex!.Message, Does.Contain("Operator '-'").And.Contain("WistStringImpl"));
    }
}
