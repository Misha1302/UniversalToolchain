namespace Tests.Core;

[TestFixture]
public class FrontendModuleRegistrationExtensionsTests
{
    [Test]
    public void AddLexemes_WithMultipleSets_RegistersAllPatternsAndRespectsPriority()
    {
        var lexer = new BasicLexerImpl();

        lexer.AddLexemes(
            [new LexemeRegistration(@"\>\=", "GreaterOrEqual", Priority: -1f)],
            [new LexemeRegistration(@"\>", "Greater")]
        );

        var tokens = lexer.Lexemize(">=");

        Assert.That(tokens, Has.Count.EqualTo(1));
        Assert.That(tokens[0].LexemePattern?.LexemeType.GetName(), Is.EqualTo("GreaterOrEqual"));
    }

    [Test]
    public void AddNodeCreators_WithMultipleSets_AddsCreatorsToExpectedPriorityLevels()
    {
        var parser = new BasicParserImpl();

        var first = new TestNodeCreator("First");
        var second = new TestNodeCreator("Second");

        parser.AddNodeCreators(
            [new NodeCreatorRegistration(-10f, first)],
            [new NodeCreatorRegistration(5f, second)]
        );

        Assert.That(parser.Configuration.NodeCreators[-10f], Contains.Item(first));
        Assert.That(parser.Configuration.NodeCreators[5f], Contains.Item(second));
    }

    [Test]
    public void AddVisitors_AddsVisitorsInProvidedOrder()
    {
        var translator = new BasicAstToBytecodeTranslatorImpl();
        var first = new TestVisitor();
        var second = new TestVisitor();

        translator.AddVisitors(first, second);

        Assert.That(translator.Configuration.Visitors, Has.Count.EqualTo(2));
        Assert.That(translator.Configuration.Visitors[0], Is.SameAs(first));
        Assert.That(translator.Configuration.Visitors[1], Is.SameAs(second));
    }

    private sealed class TestNodeCreator(string nodeType) : IAstNodeCreator
    {
        public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet(nodeType);

        public bool TryCreateNode(AstNode scope, int childIndex) => false;
    }

    private sealed class TestVisitor : IAstVisitor
    {
        public void TryVisit(BytecodeVisitorData data)
        {
        }
    }
}