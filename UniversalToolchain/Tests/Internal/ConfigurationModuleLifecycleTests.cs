using AstNodeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.ParserWrapper.AstNodeTag>;
using BasicLexer.Core;
using ParserConfigurationModule.Core;
using ParserConfigurationModule.Module;
using System.Globalization;

namespace Tests.Internal;

[TestFixture]
public sealed class ConfigurationModuleLifecycleTests
{
    [Test]
    public void LexerConfigurationReader_AppliesToEveryFreshLexer()
    {
        using var file = TemporaryFile.Create(BuildLexerConfiguration("token", "TestToken", -12.5f));
        var module = new LexerConfigurationModuleImpl(ActionType.ReadConfiguration, file.Path);
        var first = new BasicLexerImpl();
        var second = new BasicLexerImpl();

        module.InitLexer(first);
        module.InitLexer(second);

        AssertLexerConfiguration(first.Configuration, "token", "TestToken", -12.5f);
        AssertLexerConfiguration(second.Configuration, "token", "TestToken", -12.5f);
    }

    [Test]
    public void LexerConfigurationReader_FailureDoesNotMutateLiveConfiguration_AndCanBeRetried()
    {
        using var file = TemporaryFile.Create("not|a|valid|configuration|line");
        var module = new LexerConfigurationModuleImpl(ActionType.ReadConfiguration, file.Path);
        var lexer = new BasicLexerImpl();
        lexer.Configuration.AddPattern(
            new LexemePattern("existing", ExtensibleEnum<LexemeTag>.CreateOrGet("ExistingToken")),
            priority: 7);
        var before = lexer.Configuration.CreateSnapshot();

        Assert.Throws<InvalidOperationException>(() => module.InitLexer(lexer));
        Assert.That(lexer.Configuration.CreateSnapshot(), Is.EqualTo(before));

        File.WriteAllText(file.Path, BuildLexerConfiguration("replacement", "ReplacementToken", 3));
        module.InitLexer(lexer);

        AssertLexerConfiguration(lexer.Configuration, "replacement", "ReplacementToken", 3);
    }

    [Test]
    public void LexerConfigurationReader_DuplicateRegexAcrossTypes_IsRejectedAtomically()
    {
        using var file = TemporaryFile.Create(string.Join(Environment.NewLine,
            BuildLexerConfiguration("same", "FirstType", 1),
            BuildLexerConfiguration("same", "SecondType", 2)));
        var module = new LexerConfigurationModuleImpl(ActionType.ReadConfiguration, file.Path);
        var lexer = new BasicLexerImpl();
        lexer.Configuration.AddPattern(new LexemePattern("existing", ExtensibleEnum<LexemeTag>.CreateOrGet("ExistingRegexType")));
        var before = lexer.Configuration.CreateSnapshot();

        Assert.Throws<InvalidOperationException>(() => module.InitLexer(lexer));
        Assert.That(lexer.Configuration.CreateSnapshot(), Is.EqualTo(before));
    }

    [Test]
    public void LexerConfigurationReader_DuplicateTypeAcrossRegexes_IsRejectedAtomically()
    {
        using var file = TemporaryFile.Create(string.Join(Environment.NewLine,
            BuildLexerConfiguration("first", "SameType", 1),
            BuildLexerConfiguration("second", "SameType", 2)));
        var module = new LexerConfigurationModuleImpl(ActionType.ReadConfiguration, file.Path);
        var lexer = new BasicLexerImpl();
        lexer.Configuration.AddPattern(new LexemePattern("existing", ExtensibleEnum<LexemeTag>.CreateOrGet("ExistingTypeName")));
        var before = lexer.Configuration.CreateSnapshot();

        Assert.Throws<InvalidOperationException>(() => module.InitLexer(lexer));
        Assert.That(lexer.Configuration.CreateSnapshot(), Is.EqualTo(before));
    }

    [Test]
    public void ParserConfigurationReader_AppliesToEveryFreshParser()
    {
        var creatorTypeId = typeof(TestNodeCreator).FullName!;
        var nodeType = TestNodeCreator.NodeType.ToString();
        using var file = TemporaryFile.Create(BuildParserConfiguration(creatorTypeId, nodeType, -24));
        var module = new ParserConfigurationModuleImpl(ActionType.ReadConfiguration, file.Path);
        var first = CreateParser(10);
        var second = CreateParser(20);

        module.InitParser(first);
        module.InitParser(second);

        AssertParserPriority(first, -24);
        AssertParserPriority(second, -24);
    }

    [Test]
    public void ParserConfigurationReader_CanRetryAfterFailure()
    {
        using var file = TemporaryFile.Create("invalid");
        var module = new ParserConfigurationModuleImpl(ActionType.ReadConfiguration, file.Path);
        var parser = CreateParser(10);

        Assert.Throws<InvalidOperationException>(() => module.InitParser(parser));
        AssertParserPriority(parser, 10);

        File.WriteAllText(
            file.Path,
            BuildParserConfiguration(typeof(TestNodeCreator).FullName!, TestNodeCreator.NodeType.ToString(), -5));
        module.InitParser(parser);

        AssertParserPriority(parser, -5);
    }

    private static BasicParserImpl CreateParser(float priority)
    {
        var parser = new BasicParserImpl();
        parser.Configuration.NodeCreators.Add(priority, new TestNodeCreator());
        return parser;
    }

    private static void AssertParserPriority(BasicParserImpl parser, float expectedPriority)
    {
        var entries = parser.Configuration.NodeCreators
            .SelectMany(level => level.Value.Select(creator => (level.Key, creator)))
            .ToArray();

        Assert.That(entries, Has.Length.EqualTo(1));
        Assert.That(entries[0].Key, Is.EqualTo(expectedPriority));
        Assert.That(entries[0].creator, Is.TypeOf<TestNodeCreator>());
    }

    private static void AssertLexerConfiguration(
        LexerConfiguration configuration,
        string expectedPattern,
        string expectedLexemeType,
        float expectedPriority)
    {
        var entries = configuration.CreateSnapshot();
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(entries[0].Pattern.Pattern, Is.EqualTo(expectedPattern));
            Assert.That(entries[0].Pattern.LexemeType.GetName(), Is.EqualTo(expectedLexemeType));
            Assert.That(entries[0].Priority, Is.EqualTo(expectedPriority));
            Assert.That(entries[0].Ignore, Is.False);
        });
    }

    private static string BuildLexerConfiguration(string pattern, string lexemeType, float priority)
    {
        var encodedPattern = Convert.ToBase64String(Encoding.UTF8.GetBytes(pattern));
        return string.Join(
            Environment.NewLine,
            "# Format-Version: 2",
            string.Join(
                '|',
                priority.ToString("R", CultureInfo.InvariantCulture),
                encodedPattern,
                lexemeType,
                bool.FalseString));
    }

    private static string BuildParserConfiguration(string creatorTypeId, string astNodeType, float priority) =>
        string.Join(
            Environment.NewLine,
            "# Format-Version: 2",
            string.Join(
                '|',
                priority.ToString("R", CultureInfo.InvariantCulture),
                creatorTypeId,
                "0",
                astNodeType));

    private sealed class TestNodeCreator : IAstNodeCreator
    {
        public static AstNodeType NodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("ConfigurationLifecycleTestNode");

        public AstNodeType AstNodeType => NodeType;

        public bool TryCreateNode(AstNode scope, int childIndex) => false;
    }

    private sealed class TemporaryFile : IDisposable
    {
        private TemporaryFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryFile Create(string contents)
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wist2-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = System.IO.Path.Combine(directory, "configuration.txt");
            File.WriteAllText(path, contents, new UTF8Encoding(false));
            return new TemporaryFile(path);
        }

        public void Dispose()
        {
            var directory = System.IO.Path.GetDirectoryName(Path);
            if (directory != null && Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }
}
