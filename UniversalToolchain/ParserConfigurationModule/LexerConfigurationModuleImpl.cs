using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicTypesExtensions;
using ExceptionsManager;

namespace ParserConfigurationModule;

/// <summary>
///     <para> You can fix lexer modules execution order like this: </para>
///     1. Dump lexer configuration to file <br />
///     2. Rearrange patterns by hand <br />
///     3. Read configuration from file <br />
///     <para> For correct work this module have to execute InitLexer after all others plugins </para>
/// </summary>
/// <param name="actionType">dump or read configuration</param>
/// <param name="path">path to file</param>
public class LexerConfigurationModuleImpl(ActionType actionType, string path = "LexerConfiguration.txt") : ICoreModule
{
    private bool _isInitialized;

    public void InitLexer(ILexer lexer)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        switch (actionType)
        {
            case ActionType.DumpConfiguration:
                DumpConfiguration(lexer);
                break;
            case ActionType.ReadConfiguration:
                LoadConfiguration(lexer);
                break;
            default:
                Thrower.InvalidOpEx($"Unknown action type: {actionType}");
                break;
        }
    }

    private void DumpConfiguration(ILexer lexer)
    {
        try
        {
            var configurationText = new ConfigurationDumper(lexer).Dump();
            File.WriteAllText(path, configurationText);
            Debug.WriteLine($"Lexer configuration dumped to {Path.GetFullPath(path)}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to dump lexer configuration: {ex.Message}");
            Thrower.InvalidOpEx($"Failed to dump lexer configuration: {ex.Message}");
        }
    }

    private void LoadConfiguration(ILexer lexer)
    {
        try
        {
            if (!File.Exists(path))
            {
                Debug.WriteLine($"Configuration file not found: {path}. Using default configuration.");
                return;
            }

            var configText = File.ReadAllText(path);
            new ConfigurationLoader(lexer).Load(configText);
            Debug.WriteLine($"Lexer configuration loaded from {Path.GetFullPath(path)}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load lexer configuration: {ex.Message}");
            Thrower.InvalidOpEx($"Failed to load lexer configuration: {ex.Message}");
        }
    }

    // Вспомогательные классы для инкапсуляции логики
    private class ConfigurationDumper(ILexer lexer)
    {
        public string Dump()
        {
            var patterns = CollectAllPatterns();
            return GenerateDumpContent(patterns);
        }

        private List<(float Priority, LexemePattern Pattern, bool Ignore)> CollectAllPatterns()
        {
            var result = new List<(float, LexemePattern, bool)>();

            var patternsCollection = lexer.Configuration.LevelCollectionPatterns;
            if (patternsCollection is LevelCollection<float, LexemePattern> levelCollection)
            {
                foreach (var level in levelCollection)
                {
                    foreach (var pattern in level.Value)
                    {
                        var ignore = lexer.Configuration.LexemesToIgnore.Contains(pattern.LexemeType);
                        result.Add((level.Key, pattern, ignore));
                    }
                }
            }

            return result;
        }

        private string GenerateDumpContent(List<(float Priority, LexemePattern Pattern, bool Ignore)> patterns)
        {
            var lines = new List<string>
            {
                "# Lexer Configuration Dump",
                $"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "# Format: <priority>|<base64_encoded_pattern>|<lexeme_type>|<ignore_flag>",
                "# Pattern is base64 encoded to avoid issues with | character in regex patterns",
                ""
            };

            lines.AddRange(patterns
                .OrderByDescending(x => x.Priority)
                .Select(FormatPatternLine));

            return string.Join(Environment.NewLine, lines);
        }

        private string FormatPatternLine((float Priority, LexemePattern Pattern, bool Ignore) item)
        {
            // Кодируем паттерн в base64, чтобы избежать проблем с символом |
            var encodedPattern = Convert.ToBase64String(Encoding.UTF8.GetBytes(item.Pattern.Pattern));
            var lexemeType = item.Pattern.LexemeType.GetName();

            return $"{item.Priority:F2}|{encodedPattern}|{lexemeType}|{item.Ignore}";
        }
    }

    private class ConfigurationLoader(ILexer lexer)
    {
        public void Load(string configText)
        {
            var lines = ParseConfigurationLines(configText);
            ClearCurrentConfiguration();
            ApplyNewConfiguration(lines);
        }

        private List<ConfigLine> ParseConfigurationLines(string configText)
        {
            var lines = new List<ConfigLine>();
            var lineNumber = 0;

            foreach (var rawLine in configText.Split('\n'))
            {
                lineNumber++;
                var line = rawLine.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                    continue;

                var parts = line.Split('|', 4);
                if (parts.Length < 3)
                {
                    Debug.WriteLine($"Warning: Invalid format in line {lineNumber}: {line}");
                    continue;
                }

                if (!float.TryParse(parts[0].Trim(), out var priority))
                {
                    Debug.WriteLine($"Warning: Invalid priority in line {lineNumber}: {line}");
                    continue;
                }

                var encodedPattern = parts[1].Trim();
                var lexemeType = parts[2].Trim();
                var ignore = parts.Length > 3 && bool.TryParse(parts[3].Trim(), out var ignoreFlag) && ignoreFlag;

                try
                {
                    var pattern = DecodePattern(encodedPattern);
                    lines.Add(new ConfigLine(priority, pattern, lexemeType, ignore));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Warning: Failed to decode pattern in line {lineNumber}: {ex.Message}");
                }
            }

            return lines;
        }

        private string DecodePattern(string encodedPattern)
        {
            // Декодируем из base64
            var bytes = Convert.FromBase64String(encodedPattern);
            return Encoding.UTF8.GetString(bytes);
        }

        private void ClearCurrentConfiguration()
        {
            try
            {
                var patternsCollection = lexer.Configuration.LevelCollectionPatterns;
                if (patternsCollection is LevelCollection<float, LexemePattern> levelCollection)
                {
                    levelCollection.Clear();
                }

                // Очищаем список игнорируемых лексем
                lexer.Configuration.LexemesToIgnore.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Warning: Could not clear lexer configuration: {ex.Message}");
                // Продолжаем работу
            }
        }

        private void ApplyNewConfiguration(List<ConfigLine> lines)
        {
            foreach (var line in lines.OrderByDescending(x => x.Priority))
            {
                try
                {
                    // Создаем тип лексемы
                    var lexemeType = ExtensibleEnum<LexemeTag>.CreateOrGet(line.LexemeType);

                    // Проверяем паттерн на валидность
                    try
                    {
                        // Создаем Regex для проверки синтаксиса
                        _ = new Regex(line.Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
                    }
                    catch (RegexParseException ex)
                    {
                        Debug.WriteLine($"Warning: Invalid regex pattern '{line.Pattern}': {ex.Message}");
                        continue;
                    }

                    // Создаем паттерн
                    var pattern = new LexemePattern(line.Pattern, lexemeType);

                    // Добавляем в конфигурацию
                    lexer.Configuration.TryUncheckedRewritePattern(pattern, line.Ignore, line.Priority);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Warning: Failed to add pattern {line.Pattern}: {ex.Message}");
                }
            }
        }

        private record ConfigLine(float Priority, string Pattern, string LexemeType, bool Ignore);
    }
}