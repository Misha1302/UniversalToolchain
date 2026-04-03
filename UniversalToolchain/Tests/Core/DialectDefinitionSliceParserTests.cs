using BasicLexer.Core;
using BasicParser.Core;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Frontend;

namespace Tests.Core;

[TestFixture]
public class DialectDefinitionSliceParserTests
{
    [Test]
    public void Constructor_WithNullRegistry_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DialectDefinitionSliceParser(null!));
    }

    [Test]
    public void Parse_WithNullAst_ThrowsArgumentNullException()
    {
        var parser = new DialectDefinitionSliceParser(CreateRegistry());

        Assert.Throws<ArgumentNullException>(() => parser.Parse(null!));
    }

    [Test]
    public void Parse_WithValidAst_ReturnsExpectedNonNullSliceAndBasicInvariants()
    {
        var registry = CreateRegistry();
        var ast = ParseAst("dialect Tiny\nuse Arithmetic,Variables\nsecurity trusted\ncapability sandbox\n");
        var parser = new DialectDefinitionSliceParser(registry);

        var slice = parser.Parse(ast);

        Assert.Multiple(() =>
        {
            Assert.That(slice, Is.Not.Null);
            Assert.That(slice.Name, Is.EqualTo("Tiny"));
            Assert.That(slice.UseModules, Is.EqualTo(new[] { "Arithmetic", "Variables" }));
            Assert.That(slice.ExcludeModules, Is.Empty);
            Assert.That(slice.OrderDirectives, Is.Empty);
            Assert.That(slice.BackendDirectives, Is.Empty);
            Assert.That(slice.IntrinsicDirectives, Is.Empty);
            Assert.That(slice.OptimizerDirectives, Is.Empty);
            Assert.That(slice.SecurityProfile, Is.EqualTo(DialectSecurityProfile.Trusted));
            Assert.That(slice.CapabilityDirectives.Select(x => (x.Name, x.Value)), Is.EqualTo(new[] { ("sandbox", true) }));
        });
    }

    [Test]
    public void Execute_WithNullCompilation_ThrowsArgumentNullException()
    {
        var executor = new DialectDefinitionSliceExecutor();

        Assert.Throws<ArgumentNullException>(() => executor.Execute(null!, null!));
    }

    [Test]
    public void Execute_ReturnsTheSameSliceInstance()
    {
        var executor = new DialectDefinitionSliceExecutor();
        var slice = new DialectDefinitionSlice(
            "Tiny",
            [],
            [],
            [],
            [],
            [],
            [],
            null,
            []);

        var result = executor.Execute(slice, null!);

        Assert.That(ReferenceEquals(slice, result), Is.True);
    }

    private static DialectDslRegistry CreateRegistry()
    {
        var services = new ServiceCollection();
        services.AddDialectDslDefaultComposition();

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<DialectDslRegistry>();
    }

    private static AstNode ParseAst(string source)
    {
        var parser = new BasicParserImpl(new ParserConfiguration([]));
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));
        var frontend = new DialectDslFrontendModule(CreateRegistry());

        frontend.InitLexer(lexer);
        frontend.InitParser(parser);

        return parser.Parse(lexer.Lexemize(source));
    }
}