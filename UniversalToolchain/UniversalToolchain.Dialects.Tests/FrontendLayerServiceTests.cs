using AbstractIrConverters;
using BasicCodeTranslator;
using BasicCore.LexerWrapper;
using BasicCore.Registration;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicParser.Core;
using BasicLexer.Core;
using CommonExceptions;
using UniversalToolchain.Dialects.Frontend;
using AstNodeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.ParserWrapper.AstNodeTag>;

namespace UniversalToolchain.Dialects.Tests;

public class FrontendLayerServiceTests
{
    [Test]
    public void TokenLineSplitter_SplitsByNewLineDeterministically()
    {
        var tokens = Lex("dialect Tiny\nuse Arithmetic\n");

        var lines = DialectTokenLineSplitter.Split(tokens);

        Assert.Multiple(() =>
        {
            Assert.That(lines.Count, Is.EqualTo(2));
            Assert.That(lines[0].Select(x => x.Text), Is.EqualTo(new[] { "dialect", "Tiny" }));
            Assert.That(lines[1].Select(x => x.Text), Is.EqualTo(new[] { "use", "Arithmetic" }));
        });
    }

    [Test]
    public void DirectiveLineParser_ParsesUseDirectiveIntoAccumulation()
    {
        var parser = new DialectDirectiveLineParser();
        var accumulation = new DialectDirectiveAccumulation();
        var line = DialectTokenLineSplitter.Split(Lex("use Arithmetic\n"))[0];

        var parsed = parser.TryParse(line, accumulation);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(accumulation.UseModules, Is.EqualTo(new[] { "Arithmetic" }));
        });
    }


    [Test]
    public void FrontendModule_InitParser_CreatesDialectLineNodes()
    {
        var module = new DialectDslFrontendModule();
        var parser = new BasicParserImpl(new ParserConfiguration([]));
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));

        module.InitLexer(lexer);
        module.InitParser(parser);

        var ast = parser.Parse(lexer.Lexemize("dialect Tiny\nuse Arithmetic\n"));

        Assert.Multiple(() =>
        {
            Assert.That(ast.Children.Any(x => x.NodeType == AstNodeType.CreateOrGet("DialectLine")), Is.True);
            Assert.That(ast.Children.Any(x => x.Text == "\n"), Is.False);
        });
    }


    [Test]
    public void FrontendModule_AstTranslator_EmitsDialectSliceAnnotationBytecode()
    {
        var module = new DialectDslFrontendModule();
        var parser = new BasicParserImpl(new ParserConfiguration([]));
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));
        var translator = new BasicAstToBytecodeTranslatorImpl(new BytecodeTranslatorConfiguration([]));

        module.InitLexer(lexer);
        module.InitParser(parser);
        module.InitAstTranslator(translator);

        var ast = parser.Parse(lexer.Lexemize("dialect Tiny\nuse Arithmetic\n"));
        var bytecode = translator.Translate(module.ProcessAst(ast));
        var ir = new BytecodeToAbstractIrConverterImpl().Translate(bytecode);
        var slice = DialectDefinitionSliceAirReader.Read(ir);

        Assert.That(slice.Name, Is.EqualTo("Tiny"));
        Assert.That(slice.UseModules, Is.EqualTo(new[] { "Arithmetic" }));
    }

    [Test]
    public void DefinitionSliceParser_InvalidHeader_ThrowsParserException()
    {
        var parser = new DialectDefinitionSliceParser();

        var parserCore = new BasicParserImpl(new ParserConfiguration([]));
        var module = new DialectDslFrontendModule();
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));
        module.InitLexer(lexer);
        module.InitParser(parserCore);
        var ast = parserCore.Parse(lexer.Lexemize("use Arithmetic\n"));

        var ex = Assert.Throws<ParserException>(() => parser.Parse(ast));

        Assert.That(ex!.Message, Does.Contain("dialect <Name>"));
    }

    private static List<LexemeValue> Lex(string source)
    {
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));
        lexer.AddLexemes(DialectDslLexemeRegistry.Registrations);
        return lexer.Lexemize(source);
    }
}
