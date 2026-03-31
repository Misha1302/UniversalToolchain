namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class NumbersModulePipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Numbers_IntegerLiteral_ExecutesToExpectedValue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("13", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(13));
    }

    [Test]
    public void Numbers_StandaloneNegativeLiteral_FailsWithoutUnaryMinusSupport()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFails("-13", Modules, "token");
    }

    [Test]
    public void Numbers_ParenthesizedLiteral_ExecutesToExpectedValue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("(13)", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(13));
    }

    [Test]
    public void Numbers_LeadingZeroLiteral_IsHandledDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("013", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(13));
    }

    [Test]
    public void Numbers_InvalidNumericLiteral_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFails("1.2.3", Modules, "token");
    }

    [TestCase("2+3", new[] { "Number", "Addition", "Number" })]
    [TestCase("10-23", new[] { "Number", "Substraction", "Number" })]
    public void Numbers_Lexing_BinaryOperations_KeepOperatorsSeparate(string code, string[] expectedTags)
    {
        var tokens = BuildNumbersAndArithmeticLexer().Lexemize(code);
        var actualTags = tokens.Select(x => x.LexemePattern?.LexemeType.GetName()).ToArray();

        Assert.That(actualTags, Is.EqualTo(expectedTags));
    }

    [Test]
    public void Numbers_Lexing_ScientificNotationWithMinus_StaysSingleNumberToken()
    {
        var tokens = BuildNumbersAndArithmeticLexer().Lexemize("1e-5");
        var actualTags = tokens.Select(x => x.LexemePattern?.LexemeType.GetName()).ToArray();

        Assert.That(actualTags, Is.EqualTo(new[] { "Number" }));
        Assert.That(tokens[0].Text, Is.EqualTo("1e-5"));
    }

    private static BasicCore.LexerWrapper.ILexer BuildNumbersAndArithmeticLexer()
    {
        BasicCore.LexerWrapper.ILexer lexer = new BasicLexer.Core.BasicLexerImpl();
        new NumbersModule.Module.NumbersModuleImpl().InitLexer(lexer);
        new ArithmeticModule.Module.ArithmeticModuleImpl().InitLexer(lexer);
        return lexer;
    }
}
