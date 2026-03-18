using AbstractIrConverters;
using BasicCodeTranslator;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using BasicLexer.Core;
using BasicParser.Core;
using CommonExceptions;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Tests;

public class FrontendLayerServiceTests
{
    [Test]
    public void TokenLineSplitter_SplitsLinesAndSkipsBlankLines_Deterministically()
    {
        var lines = DialectTokenLineSplitter.Split(Lex("\n\ndialect Tiny\n\nuse Arithmetic,Variables\n\n"));

        Assert.Multiple(() =>
        {
            Assert.That(lines.Count, Is.EqualTo(2));
            Assert.That(lines[0].Select(x => x.Text), Is.EqualTo(new[] { "dialect", "Tiny" }));
            Assert.That(lines[1].Select(x => x.Text), Is.EqualTo(new[] { "use", "Arithmetic", ",", "Variables" }));
        });
    }

    [Test]
    public void DirectiveLineParser_ParsesUseDirectiveIdentifierList()
    {
        var parser = new DialectDirectiveLineParser();
        var accumulation = new DialectDirectiveAccumulation();
        var line = DialectTokenLineSplitter.Split(Lex("use Arithmetic,Variables\n"))[0];

        var parsed = parser.TryParse(line, accumulation);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(accumulation.UseModules, Is.EqualTo(new[] { "Arithmetic", "Variables" }));
        });
    }

    [Test]
    public void FrontendModule_InitParser_CreatesExplicitDocumentTree()
    {
        var ast = ParseWithFrontendModule("\n dialect Tiny\n\nuse Arithmetic,Variables\nallow add_i32\n");
        var document = DialectDslAstValidator.Validate(ast);

        Assert.Multiple(() =>
        {
            Assert.That(document, Is.TypeOf<DialectDocumentAstNode>());
            Assert.That(document.Children.Select(x => x.GetType()).ToArray(), Is.EqualTo(new[]
            {
                typeof(DialectDeclarationAstNode),
                typeof(UseModulesDirectiveAstNode),
                typeof(AllowIntrinsicDirectiveAstNode)
            }));
            Assert.That(document.Declaration.NameNode.Identifier, Is.EqualTo("Tiny"));
            Assert.That(((UseModulesDirectiveAstNode)document.Directives[0]).Identifiers.Identifiers.Select(x => x.Identifier),
                Is.EqualTo(new[] { "Arithmetic", "Variables" }));
        });
    }

    [Test]
    public void FrontendModule_AstTranslator_EmitsTypedDialectAnnotations()
    {
        var module = new DialectDslFrontendModule();
        var parser = new BasicParserImpl(new ParserConfiguration([]));
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));
        var translator = new BasicAstToBytecodeTranslatorImpl(new BytecodeTranslatorConfiguration([]));

        module.InitLexer(lexer);
        module.InitParser(parser);
        module.InitAstTranslator(translator);

        var ast = parser.Parse(lexer.Lexemize("dialect Tiny\nuse Arithmetic,Variables\nsecurity trusted\ncapability sandbox\n"));
        var bytecode = translator.Translate(module.ProcessAst(ast));
        var ir = new BytecodeToAbstractIrConverterImpl().Translate(bytecode);
        var annotations = ir.Instructions.SelectMany(x => x.Metadata).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(annotations.OfType<DialectNameAirAnnotation>().Single().Name, Is.EqualTo("Tiny"));
            Assert.That(annotations.OfType<UseModulesAirAnnotation>().Single().Modules, Is.EqualTo(new[] { "Arithmetic", "Variables" }));
            Assert.That(annotations.OfType<SecurityAirAnnotation>().Single().Profile, Is.EqualTo(DialectSecurityProfile.Trusted));
            Assert.That(annotations.OfType<CapabilityAirAnnotation>().Single().Capabilities, Is.EqualTo(new[] { "sandbox" }));
        });
    }

    [Test]
    public void DefinitionSliceParser_InvalidHeader_ThrowsParserException()
    {
        var ex = Assert.Throws<ParserException>(() => ParseWithFrontendModule("use Arithmetic\n"));

        Assert.That(ex!.Message, Does.Contain("dialect <name>").IgnoreCase);
    }

    [Test]
    public void AirReader_RejectsUnknownAnnotationType_Explicitly()
    {
        var ir = new UniversalIntermediateRepresentation.AbstractIR();
        ir.AppendInstructions([new Instruction(UOpCode.Annotate, metadata: [new object()])]);

        var ex = Assert.Throws<InvalidOperationException>(() => DialectDefinitionSliceAirReader.Read(ir));

        Assert.That(ex!.Message, Does.Contain("unsupported annotation type"));
    }

    private static AstNode ParseWithFrontendModule(string source)
    {
        var module = new DialectDslFrontendModule();
        var parser = new BasicParserImpl(new ParserConfiguration([]));
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));
        module.InitLexer(lexer);
        module.InitParser(parser);
        return parser.Parse(lexer.Lexemize(source));
    }

    private static List<LexemeValue> Lex(string source)
    {
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));
        lexer.AddLexemes(DialectDslLexemeRegistry.Registrations);
        return lexer.Lexemize(source);
    }
}
