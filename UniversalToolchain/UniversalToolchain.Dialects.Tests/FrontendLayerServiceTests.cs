using AbstractIrConverters;
using BasicCodeTranslator;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicLexer.Core;
using BasicParser.Core;
using CommonExceptions;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Tests;

public class FrontendLayerServiceTests
{
    [Test]
    public void FrontendModule_InitParser_CreatesSemanticDialectRootNode()
    {
        var module = new DialectDslFrontendModule();
        var parser = new BasicParserImpl(new ParserConfiguration([]));
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));

        module.InitLexer(lexer);
        module.InitParser(parser);

        var ast = parser.Parse(lexer.Lexemize("dialect Tiny\nuse Arithmetic\n"));

        Assert.That(ast.Children.Single(), Is.TypeOf<DialectRootAstNode>());
    }

    [Test]
    public void FrontendModule_AstTranslator_EmitsStructuredDialectAnnotations()
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
        var annotations = DialectDefinitionSliceAirReader.Read(ir);

        Assert.That(annotations.OfType<DialectNameAirAnnotation>().Single().Name, Is.EqualTo("Tiny"));
        Assert.That(annotations.OfType<UseModulesAirAnnotation>().Single().ModuleNames, Is.EqualTo(new[] { "Arithmetic" }));
        Assert.That(annotations.Any(x => x is not DialectNameAirAnnotation and not UseModulesAirAnnotation), Is.False);
    }

    [Test]
    public void Parser_UnknownDirective_ThrowsPredictableError()
    {
        var compiler = new DialectDslCompiler();

        var ex = Assert.Throws<ParserException>(() => compiler.Compile("dialect Tiny\nunknown x\n"));

        Assert.That(ex!.Message, Does.Contain("Unknown directive"));
    }
}
