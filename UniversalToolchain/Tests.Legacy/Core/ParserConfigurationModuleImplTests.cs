namespace Tests.Core;

[TestFixture]
public class ParserConfigurationModuleImplTests
{
    [SetUp]
    public void Setup()
    {
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"test_parser_config_{Guid.NewGuid():N}.txt");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testConfigPath))
            File.Delete(_testConfigPath);
    }

    private string _testConfigPath;

    [Test]
    public void DumpConfiguration_CreatesFile_WithCorrectFormat()
    {
        var parser = CreateParserWithModules();
        var module = new ParserConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);


        module.InitParser(parser);


        Assert.That(File.Exists(_testConfigPath), Is.True);

        var content = File.ReadAllText(_testConfigPath);
        Assert.That(content, Does.Contain("# Parser Configuration Dump"));
        Assert.That(content, Does.Contain("# Format: <priority>|<type_full_name>|<instance_hash>|<ast_node_type>"));

        // Проверяем наличие хотя бы некоторых ожидаемых строк
        Assert.That(content, Does.Contain("ScopesCreator"));
        Assert.That(content, Does.Contain("AdditionOperationNodeCreator"));
    }

    [Test]
    public void LoadConfiguration_ChangesOrder_WhenFileExists()
    {
        var parser1 = CreateParserWithModules();
        var dumpModule = new ParserConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);
        dumpModule.InitParser(parser1);

        // Модифицируем файл конфигурации: меняем порядок приоритетов
        var originalContent = File.ReadAllText(_testConfigPath);
        var modifiedContent = originalContent
            .Replace("-100000.00|", "99999.00|") // Меняем приоритет ScopesCreator
            .Replace("10.00|EqualityModule", "-50.00|EqualityModule"); // Меняем приоритет ValuesSetNodeCreator

        File.WriteAllText(_testConfigPath, modifiedContent);

        var parser2 = CreateParserWithModules();
        var loadModule = new ParserConfigurationModuleImpl(ActionType.ReadConfiguration, _testConfigPath);


        loadModule.InitParser(parser2);


        var scopesCreatorPriority = GetCreatorPriority(parser2, "ScopesCreator");
        var equalityCreatorPriority = GetCreatorPriority(parser2, "ValuesSetNodeCreator");

        Assert.That(scopesCreatorPriority, Is.EqualTo(99999f));
        Assert.That(equalityCreatorPriority, Is.EqualTo(-50f));
    }

    [Test]
    public void LoadConfiguration_UsesDefault_WhenFileNotFound()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.txt");
        var parser = CreateParserWithModules();
        var module = new ParserConfigurationModuleImpl(ActionType.ReadConfiguration, nonExistentPath);


        Assert.DoesNotThrow(() => module.InitParser(parser));

        // Проверяем, что конфигурация осталась по умолчанию
        var scopesCreatorPriority = GetCreatorPriority(parser, "ScopesCreator");
        Assert.That(scopesCreatorPriority, Is.EqualTo(-100000f)); // Оригинальный приоритет
    }

    [Test]
    public void DumpAndLoad_PreservesAllCreators()
    {
        var parser1 = CreateParserWithModules();
        var originalCount = CountAllCreators(parser1);

        var dumpModule = new ParserConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);
        dumpModule.InitParser(parser1);

        var parser2 = CreateParserWithModules();
        var loadModule = new ParserConfigurationModuleImpl(ActionType.ReadConfiguration, _testConfigPath);


        loadModule.InitParser(parser2);
        var loadedCount = CountAllCreators(parser2);


        Assert.That(loadedCount, Is.EqualTo(originalCount));
    }

    [Test]
    public void LoadConfiguration_HandlesDuplicateTypes_Correctly()
    {
        var parser = CreateParserWithModules();

        // Создаем файл конфигурации с несколькими экземплярами одного типа
        var configContent =
            """
            # Test config
            -10.00|ConditionsModule.ComparisonNodeCreator|0|Equal
            -10.00|ConditionsModule.ComparisonNodeCreator|1|NotEqual
            -10.00|ConditionsModule.ComparisonNodeCreator|2|Greater
            20.00|ArithmeticModule.Creators.AdditionOperationNodeCreator|0|Addition
            20.00|ArithmeticModule.SubtractionOperationNodeCreator|0|Subtraction
            """;

        File.WriteAllText(_testConfigPath, configContent);

        var loadModule = new ParserConfigurationModuleImpl(ActionType.ReadConfiguration, _testConfigPath);


        Assert.DoesNotThrow(() => loadModule.InitParser(parser));

        // Проверяем, что все креаторы добавлены
        var creators = GetAllCreators(parser);
        var comparisonCreators = creators.Where(c => c.GetType().FullName?.Contains("ComparisonNodeCreator") == true).ToList();
        Assert.That(comparisonCreators.Count, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void DumpConfiguration_IncludesAstNodeType_ForEachCreator()
    {
        var parser = CreateParserWithModules();
        var module = new ParserConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);


        module.InitParser(parser);
        var content = File.ReadAllText(_testConfigPath);


        // Проверяем, что для каждого креатора указан AstNodeType
        var lines = content.Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('#'))
            .ToList();

        foreach (var line in lines)
        {
            var parts = line.Split('|');
            Assert.That(parts.Length, Is.GreaterThanOrEqualTo(4), $"Invalid line format: {line}");
            var astNodeType = parts[3].Trim();
            Assert.That(astNodeType, Is.Not.EqualTo("null"), $"AstNodeType is null for line: {line}");
        }
    }

    private BasicParserImpl CreateParserWithModules()
    {
        var parser = new BasicParserImpl();

        // Инициализируем парсер разными модулями (как в реальном использовании)
        var modules = new IFrontendCoreModule[]
        {
            new ScopesModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new LabelsModuleImpl(),
            new EqualityModuleImpl(),
            new CSharpInteropModuleImpl(),
            new ConditionsModuleImpl(),
            new ComparisonOperations(),
            new BooleanOperations()
        };

        foreach (var module in modules)
            module.InitParser(parser);

        return parser;
    }

    private float GetCreatorPriority(IParser parser, string typeName)
    {
        foreach (var level in parser.Configuration.NodeCreators)
        {
            foreach (var creator in level.Value)
            {
                var fullName = creator.GetType().FullName;
                if (fullName == typeName || fullName?.EndsWith($".{typeName}") == true || creator.GetType().Name == typeName)
                    return level.Key;
            }
        }

        return 0;
    }

    private int CountAllCreators(IParser parser)
    {
        return parser.Configuration.NodeCreators.Sum(level => level.Value.Count);
    }

    private List<IAstNodeCreator> GetAllCreators(IParser parser)
    {
        var result = new List<IAstNodeCreator>();
        foreach (var level in parser.Configuration.NodeCreators)
            result.AddRange(level.Value);
        return result;
    }
}