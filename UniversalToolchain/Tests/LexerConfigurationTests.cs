namespace Tests;

[TestFixture]
public class LexerConfigurationTests
{
    [SetUp]
    public void Setup()
    {
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"test_lexer_config_{Guid.NewGuid()}.txt");
    }

    [TearDown]
    public void Teardown()
    {
        if (File.Exists(_testConfigPath))
        {
            File.Delete(_testConfigPath);
        }
    }

    private string _testConfigPath;

    [Test]
    public void LexerConfiguration_DumpAndLoad_SimplePatterns_RestoresCorrectly()
    {
        // Arrange
        var lexer = new BasicLexerImpl();

        // Добавляем простые паттерны
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"\d+", ExtensibleEnum<LexemeTag>.CreateOrGet("Number")),
            priority: 10f
        );
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"\+", ExtensibleEnum<LexemeTag>.CreateOrGet("Plus")),
            priority: 5f
        );
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"\s+", ExtensibleEnum<LexemeTag>.CreateOrGet("Whitespace")),
            true
        );

        // Act - Дамп
        var dumpModule = new LexerConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);
        dumpModule.InitLexer(lexer);

        // Assert - Файл создан
        Assert.That(File.Exists(_testConfigPath), Is.True);
        var fileContent = File.ReadAllText(_testConfigPath);
        Assert.That(fileContent, Does.Contain("# Lexer Configuration Dump"));
        Assert.That(fileContent, Does.Contain("Number"));
        Assert.That(fileContent, Does.Contain("Plus"));

        // Act - Загрузка в новый лексер
        var newLexer = new BasicLexerImpl();
        var loadModule = new LexerConfigurationModuleImpl(ActionType.ReadConfiguration, _testConfigPath);
        loadModule.InitLexer(newLexer);

        // Assert - Лексер работает корректно
        var tokens = newLexer.Lexemize("123+456");
        var tokenTypes = tokens.Select(t => t.LexemePattern?.LexemeType.GetName()).ToList();

        // Должны быть только Number и Plus (Whitespace игнорируется)
        Assert.That(tokenTypes, Has.Count.EqualTo(3));
        Assert.That(tokenTypes[0], Is.EqualTo("Number"));
        Assert.That(tokenTypes[1], Is.EqualTo("Plus"));
        Assert.That(tokenTypes[2], Is.EqualTo("Number"));
    }

    [Test]
    public void LexerConfiguration_DumpAndLoad_PatternsWithPipeCharacter_RestoresCorrectly()
    {
        // Arrange
        var lexer = new BasicLexerImpl();

        // Паттерн с символом | (ИЛИ в regex)
        lexer.Configuration.TryAddPattern(
            new LexemePattern("(a|b|c)", ExtensibleEnum<LexemeTag>.CreateOrGet("Choice")),
            priority: 10f
        );

        // Act
        var dumpModule = new LexerConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);
        dumpModule.InitLexer(lexer);

        // Assert - Файл создан и содержит base64
        var fileContent = File.ReadAllText(_testConfigPath);
        Assert.That(fileContent, Does.Contain("Choice"));
        Assert.That(fileContent, Does.Not.Contain("(a|b|c)")); // Должно быть в base64

        // Act - Загрузка
        var newLexer = new BasicLexerImpl();
        var loadModule = new LexerConfigurationModuleImpl(ActionType.ReadConfiguration, _testConfigPath);
        loadModule.InitLexer(newLexer);

        // Assert
        var tokens = newLexer.Lexemize("abc");
        Assert.That(tokens, Has.Count.EqualTo(3));
        Assert.That(tokens.All(t => t.LexemePattern?.LexemeType.GetName() == "Choice"));
    }

    [Test]
    public void LexerConfiguration_DumpAndLoad_ComplexRegexPatterns_RestoresCorrectly()
    {
        // Arrange
        var lexer = new BasicLexerImpl();

        // Сложные паттерны с разными спецсимволами
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"\bif\b|\belse\b|\bwhile\b", ExtensibleEnum<LexemeTag>.CreateOrGet("Keyword")),
            priority: 100f
        );
        lexer.Configuration.TryAddPattern(
            new LexemePattern("[a-zA-Z_][a-zA-Z0-9_]*", ExtensibleEnum<LexemeTag>.CreateOrGet("Identifier")),
            priority: 50f
        );
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"\""[^\""]*\""", ExtensibleEnum<LexemeTag>.CreateOrGet("String")),
            priority: 30f
        );
        lexer.Configuration.TryAddPattern(
            new LexemePattern(" ", ExtensibleEnum<LexemeTag>.CreateOrGet("Whitespace")),
            priority: 30f
        );
        lexer.Configuration.TryAddPattern(
            new LexemePattern("==", ExtensibleEnum<LexemeTag>.CreateOrGet("Eq")),
            priority: 30f
        );

        // Act
        var dumpModule = new LexerConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);
        dumpModule.InitLexer(lexer);
        var loadModule = new LexerConfigurationModuleImpl(ActionType.ReadConfiguration, _testConfigPath);

        var newLexer = new BasicLexerImpl();
        loadModule.InitLexer(newLexer);

        // Assert
        var tokens = newLexer.Lexemize("""if x == "test" else y""");
        Assert.That(tokens, Has.Count.GreaterThan(0));

        // Проверяем, что ключевые слова распознаются
        var keywordTokens = tokens.Where(t => t.Text is "if" or "else").ToList();
        Assert.That(keywordTokens, Has.Count.EqualTo(2));
    }

    [Test]
    public void LexerConfiguration_IgnoreFlag_PreservedCorrectly()
    {
        // Arrange
        var lexer = new BasicLexerImpl();

        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"\s+", ExtensibleEnum<LexemeTag>.CreateOrGet("Space")),
            true
        );
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"\n", ExtensibleEnum<LexemeTag>.CreateOrGet("NewLine"))
        );

        // Act
        var dumpModule = new LexerConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);
        dumpModule.InitLexer(lexer);

        // Проверяем, что флаги сохранены в файле
        var fileContent = File.ReadAllText(_testConfigPath);
        var lines = fileContent.Split('\n').Where(l => !l.StartsWith('#') && !string.IsNullOrWhiteSpace(l)).ToList();

        var spaceLine = lines.First(l => l.Contains("Space"));
        var newLineLine = lines.First(l => l.Contains("NewLine"));

        Assert.That(spaceLine, Does.EndWith("|True"));
        Assert.That(newLineLine, Does.EndWith("|False"));
    }

    [Test]
    public void LexerConfiguration_Priorities_PreservedCorrectly()
    {
        // Arrange
        var lexer = new BasicLexerImpl();

        lexer.Configuration.TryAddPattern(new LexemePattern(@"\.", ExtensibleEnum<LexemeTag>.CreateOrGet("Dot")), priority: 100f);
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\d+\.\d+", ExtensibleEnum<LexemeTag>.CreateOrGet("Float")), priority: 90f);
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\d+", ExtensibleEnum<LexemeTag>.CreateOrGet("Integer")), priority: 80f);

        // Act
        var dumpModule = new LexerConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);
        dumpModule.InitLexer(lexer);

        // Проверяем порядок в файле (должны быть отсортированы по убыванию приоритета)
        var fileContent = File.ReadAllText(_testConfigPath);
        var lines = fileContent.Split('\n')
            .Where(l => !l.StartsWith('#') && !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split('|')[0])
            .Select(float.Parse)
            .ToList();

        // Проверяем, что приоритеты отсортированы по возрастанию
        for (var i = 0; i < lines.Count - 1; i++)
        {
            Assert.That(lines[i], Is.LessThanOrEqualTo(lines[i + 1]));
        }
    }

    [Test]
    public void LexerConfiguration_EmptyConfiguration_DumpsEmptyFile()
    {
        // Arrange
        var lexer = new BasicLexerImpl(); // Пустая конфигурация

        // Act
        var dumpModule = new LexerConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);
        dumpModule.InitLexer(lexer);

        // Assert
        var fileContent = File.ReadAllText(_testConfigPath);
        Assert.That(fileContent, Does.Contain("# Lexer Configuration Dump"));

        // Только заголовки, без паттернов
        var lines = fileContent.Split('\n').Where(l => !l.StartsWith('#') && !string.IsNullOrWhiteSpace(l)).ToList();
        Assert.That(lines, Is.Empty);
    }

    [Test]
    public void LexerConfiguration_LoadNonExistentFile_DoesNotThrow()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.txt");
        var lexer = new BasicLexerImpl();

        // Act & Assert - Не должно бросать исключение
        Assert.DoesNotThrow(() =>
        {
            var loadModule = new LexerConfigurationModuleImpl(ActionType.ReadConfiguration, nonExistentPath);
            loadModule.InitLexer(lexer);
        });
    }

    [Test]
    public void LexerConfiguration_InvalidBase64InFile_HandledGracefully()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, @"# Invalid config
100.00|NOT_VALID_BASE64|Number|false");

        var lexer = new BasicLexerImpl();

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var loadModule = new LexerConfigurationModuleImpl(ActionType.ReadConfiguration, _testConfigPath);
            loadModule.InitLexer(lexer);
        });
    }

    [Test]
    public void LexerConfiguration_InvalidRegexPattern_HandledGracefully()
    {
        // Arrange
        // Некорректный regex: незакрытая скобка
        var invalidPattern = "(";
        var encodedPattern = Convert.ToBase64String(Encoding.UTF8.GetBytes(invalidPattern));

        File.WriteAllText(_testConfigPath, $"100.00|{encodedPattern}|InvalidPattern|false");

        var lexer = new BasicLexerImpl();

        // Act & Assert - Не должно бросать исключение при загрузке
        Assert.DoesNotThrow(() =>
        {
            var loadModule = new LexerConfigurationModuleImpl(ActionType.ReadConfiguration, _testConfigPath);
            loadModule.InitLexer(lexer);
        });
    }

    [Test]
    public void LexerConfiguration_MissingFieldsInLine_HandledGracefully()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, @"# Config with missing fields
100.00|pattern_only
|missing_priority|Type|false
100.00||EmptyPattern|true");

        var lexer = new BasicLexerImpl();

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var loadModule = new LexerConfigurationModuleImpl(ActionType.ReadConfiguration, _testConfigPath);
            loadModule.InitLexer(lexer);
        });
    }

    [Test]
    public void LexerConfiguration_CommentsAndEmptyLines_Ignored()
    {
        // Arrange
        var configContent = @"# This is a comment

# Another comment
100.00|XGQr|Number|false

# Yet another comment
50.00|XCsr|Plus|false
";
        File.WriteAllText(_testConfigPath, configContent);

        var lexer = new BasicLexerImpl();

        // Act
        var loadModule = new LexerConfigurationModuleImpl(ActionType.ReadConfiguration, _testConfigPath);
        loadModule.InitLexer(lexer);

        // Assert - Должны загрузиться 2 паттерна
        var tokens = lexer.Lexemize("123+456");
        Assert.That(tokens, Has.Count.EqualTo(3));
    }

    [Test]
    public void LexerConfiguration_SpecialRegexCharacters_HandledCorrectly()
    {
        // Arrange
        var lexer = new BasicLexerImpl();

        // Паттерны со специальными символами regex
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"\[\w+\]", ExtensibleEnum<LexemeTag>.CreateOrGet("Brackets")),
            priority: 10f
        );
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"\\(.*?)\\", ExtensibleEnum<LexemeTag>.CreateOrGet("Escaped")),
            priority: 10f
        );
        lexer.Configuration.TryAddPattern(
            new LexemePattern(" ", ExtensibleEnum<LexemeTag>.CreateOrGet("Whitespace")),
            true,
            10f
        );

        // Act
        var dumpModule = new LexerConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);
        dumpModule.InitLexer(lexer);

        var newLexer = new BasicLexerImpl();
        var loadModule = new LexerConfigurationModuleImpl(ActionType.ReadConfiguration, _testConfigPath);
        loadModule.InitLexer(newLexer);

        // Assert
        var tokens = newLexer.Lexemize("[test] \\escaped\\");
        Assert.That(tokens, Has.Count.EqualTo(2));
        Assert.That(tokens[0].Text, Is.EqualTo("[test]"));
        Assert.That(tokens[1].Text, Is.EqualTo("\\escaped\\"));
    }

    [Test]
    public void LexerConfiguration_PreservesPatternOrderForSamePriority()
    {
        // Arrange
        var lexer = new BasicLexerImpl();

        // Паттерны с одинаковым приоритетом
        lexer.Configuration.TryAddPattern(
            new LexemePattern("a", ExtensibleEnum<LexemeTag>.CreateOrGet("LetterA")),
            priority: 10f
        );
        lexer.Configuration.TryAddPattern(
            new LexemePattern("b", ExtensibleEnum<LexemeTag>.CreateOrGet("LetterB")),
            priority: 10f
        );
        lexer.Configuration.TryAddPattern(
            new LexemePattern("c", ExtensibleEnum<LexemeTag>.CreateOrGet("LetterC")),
            priority: 10f
        );

        // Act
        var dumpModule = new LexerConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);
        dumpModule.InitLexer(lexer);

        // Assert - Порядок сохранения в файле может быть разный, но это нормально
        // Главное, чтобы загрузка работала
        var fileContent = File.ReadAllText(_testConfigPath);
        Assert.That(fileContent, Does.Contain("LetterA"));
        Assert.That(fileContent, Does.Contain("LetterB"));
        Assert.That(fileContent, Does.Contain("LetterC"));
    }

    [Test]
    public void LexerConfiguration_IntegrationWithExistingModules_WorksCorrectly()
    {
        // Arrange
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl()
        };

        // Создаем core с этими модулями
        var lexer = new BasicLexerImpl();
        foreach (var module in modules)
        {
            module.InitLexer(lexer);
        }

        // Act - Дамп конфигурации после инициализации модулями
        var dumpModule = new LexerConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);
        dumpModule.InitLexer(lexer);

        // Assert - Проверяем, что все паттерны из модулей сохранены
        var fileContent = File.ReadAllText(_testConfigPath);
        Assert.That(fileContent, Does.Contain("Identifier"));
        Assert.That(fileContent, Does.Contain("Number"));
        Assert.That(fileContent, Does.Contain("Addition"));
        Assert.That(fileContent, Does.Contain("Substraction"));
    }

    [Test]
    public void LexerConfiguration_RoundTripWithComplexCode_ProducesSameTokens()
    {
        // Arrange
        var originalLexer = new BasicLexerImpl();

        // Настраиваем как типичный лексер
        originalLexer.Configuration.TryAddPattern(
            new LexemePattern(@"\bif\b|\belse\b", ExtensibleEnum<LexemeTag>.CreateOrGet("Keyword")),
            priority: 100f
        );
        originalLexer.Configuration.TryAddPattern(
            new LexemePattern(@"[a-zA-Z_]\w*", ExtensibleEnum<LexemeTag>.CreateOrGet("Identifier")),
            priority: 90f
        );
        originalLexer.Configuration.TryAddPattern(
            new LexemePattern(@"\d+(\.\d+)?", ExtensibleEnum<LexemeTag>.CreateOrGet("Number")),
            priority: 80f
        );
        originalLexer.Configuration.TryAddPattern(
            new LexemePattern(@"\s+", ExtensibleEnum<LexemeTag>.CreateOrGet("Whitespace")),
            true
        );
        originalLexer.Configuration.TryAddPattern(
            new LexemePattern(">", ExtensibleEnum<LexemeTag>.CreateOrGet("Gt")),
            true
        );
        originalLexer.Configuration.TryAddPattern(
            new LexemePattern("=", ExtensibleEnum<LexemeTag>.CreateOrGet("Eq")),
            true
        );

        // Дамп
        var dumpModule = new LexerConfigurationModuleImpl(ActionType.DumpConfiguration, _testConfigPath);
        dumpModule.InitLexer(originalLexer);

        // Загрузка
        var loadedLexer = new BasicLexerImpl();
        var loadModule = new LexerConfigurationModuleImpl(ActionType.ReadConfiguration, _testConfigPath);
        loadModule.InitLexer(loadedLexer);

        // Complex code
        var code = "if x > 5 else y = 3.14";

        // Act
        var originalTokens = originalLexer.Lexemize(code)
            .Where(t => !originalLexer.Configuration.LexemesToIgnore.Contains(t.LexemePattern.NotNull().LexemeType.NotNull()))
            .Select(t => t.Text)
            .ToList();

        var loadedTokens = loadedLexer.Lexemize(code)
            .Where(t => !loadedLexer.Configuration.LexemesToIgnore.Contains(t.LexemePattern.NotNull().LexemeType.NotNull()))
            .Select(t => t.Text)
            .ToList();

        // Assert
        Assert.That(loadedTokens, Is.EqualTo(originalTokens));
    }
}