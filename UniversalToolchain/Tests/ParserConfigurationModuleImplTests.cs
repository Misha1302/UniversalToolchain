using ArithmeticModule;
using BasicCore;
using BasicCore.ParserWrapper;
using BasicParser;
using ConditionsModule;
using CSharpInteropModule;
using EqualityModule;
using LabelsModule;
using ParserConfigurationModule;
using ScopesModule;
using VariablesModule;

namespace Tests;

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
        // Arrange
        var parser = CreateParserWithModules();
        var module = new ParserConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);

        // Act
        module.InitParser(parser);

        // Assert
        Assert.That(File.Exists(_testConfigPath), Is.True);

        var content = File.ReadAllText(_testConfigPath);
        Assert.That(content, Does.Contain("# Parser Configuration Dump"));
        Assert.That(content, Does.Contain("# Format: <priority>|<type_full_name>|<instance_hash>|<ast_node_type>"));

        // Проверяем наличие хотя бы некоторых ожидаемых строк
        Assert.That(content, Does.Contain("ScopesModule.ScopesCreator"));
        Assert.That(content, Does.Contain("ArithmeticModule.AdditionOperationNodeCreator"));
    }

    [Test]
    public void LoadConfiguration_ChangesOrder_WhenFileExists()
    {
        // Arrange
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

        // Act
        loadModule.InitParser(parser2);

        // Assert
        var scopesCreatorPriority = GetCreatorPriority(parser2, "ScopesModule.ScopesCreator");
        var equalityCreatorPriority = GetCreatorPriority(parser2, "EqualityModule.ValuesSetNodeCreator");

        Assert.That(scopesCreatorPriority, Is.EqualTo(99999f));
        Assert.That(equalityCreatorPriority, Is.EqualTo(-50f));
    }

    [Test]
    public void LoadConfiguration_UsesDefault_WhenFileNotFound()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.txt");
        var parser = CreateParserWithModules();
        var module = new ParserConfigurationModuleImpl(ActionType.ReadConfiguration, nonExistentPath);

        // Act & Assert - не должно быть исключения
        Assert.DoesNotThrow(() => module.InitParser(parser));

        // Проверяем, что конфигурация осталась по умолчанию
        var scopesCreatorPriority = GetCreatorPriority(parser, "ScopesModule.ScopesCreator");
        Assert.That(scopesCreatorPriority, Is.EqualTo(-100000f)); // Оригинальный приоритет
    }

    [Test]
    public void DumpAndLoad_PreservesAllCreators()
    {
        // Arrange
        var parser1 = CreateParserWithModules();
        var originalCount = CountAllCreators(parser1);

        var dumpModule = new ParserConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);
        dumpModule.InitParser(parser1);

        var parser2 = CreateParserWithModules();
        var loadModule = new ParserConfigurationModuleImpl(ActionType.ReadConfiguration, _testConfigPath);

        // Act
        loadModule.InitParser(parser2);
        var loadedCount = CountAllCreators(parser2);

        // Assert
        Assert.That(loadedCount, Is.EqualTo(originalCount));
    }

    [Test]
    public void LoadConfiguration_HandlesDuplicateTypes_Correctly()
    {
        // Arrange
        var parser = CreateParserWithModules();

        // Создаем файл конфигурации с несколькими экземплярами одного типа
        var configContent =
            """
            # Test config
            -10.00|ConditionsModule.ComparisonNodeCreator|0|Equal
            -10.00|ConditionsModule.ComparisonNodeCreator|1|NotEqual
            -10.00|ConditionsModule.ComparisonNodeCreator|2|Greater
            20.00|ArithmeticModule.AdditionOperationNodeCreator|0|Addition
            20.00|ArithmeticModule.SubstractionOperationNodeCreator|0|Substraction
            """;

        File.WriteAllText(_testConfigPath, configContent);

        var loadModule = new ParserConfigurationModuleImpl(ActionType.ReadConfiguration, _testConfigPath);

        // Act & Assert - не должно быть исключения
        Assert.DoesNotThrow(() => loadModule.InitParser(parser));

        // Проверяем, что все креаторы добавлены
        var creators = GetAllCreators(parser);
        var comparisonCreators = creators.Where(c => c.GetType().FullName?.Contains("ComparisonNodeCreator") == true).ToList();
        Assert.That(comparisonCreators.Count, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void DumpConfiguration_IncludesAstNodeType_ForEachCreator()
    {
        // Arrange
        var parser = CreateParserWithModules();
        var module = new ParserConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);

        // Act
        module.InitParser(parser);
        var content = File.ReadAllText(_testConfigPath);

        // Assert
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
        {
            module.InitParser(parser);
        }

        return parser;
    }

    private float GetCreatorPriority(IParser parser, string typeFullName)
    {
        foreach (var level in parser.Configuration.NodeCreators)
        {
            foreach (var creator in level.Value)
            {
                if (creator.GetType().FullName == typeFullName)
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
        {
            result.AddRange(level.Value);
        }
        return result;
    }
}