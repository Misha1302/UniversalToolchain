using System.Globalization;

namespace ParserConfigurationModule.Module;

/// <summary>
///     Dumps or reapplies lexer pattern priorities. Loading is strict and transactional:
///     malformed entries fail before the live lexer configuration is modified.
/// </summary>
/// <param name="actionType">Dump or read configuration.</param>
/// <param name="path">Configuration file path.</param>
public sealed class LexerConfigurationModuleImpl(ActionType actionType, string path = "LexerConfiguration.txt") : IFrontendCoreModule
{
    private const string FormatVersion = "2";
    private static readonly UTF8Encoding _strictUtf8 = new(false, true);

    public void InitLexer(ILexer lexer)
    {
        lexer = lexer.ArgNotNull();

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
            var configurationText = ConfigurationDumper.Dump(lexer.Configuration.CreateSnapshot());
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath)!;
            Directory.CreateDirectory(directory);

            var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllText(temporaryPath, configurationText, new UTF8Encoding(false));
                File.Move(temporaryPath, fullPath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }

            Debug.WriteLine($"Lexer configuration dumped to {fullPath}");
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to dump lexer configuration to '{path}'.", exception);
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
            var snapshot = ConfigurationLoader.Build(configText);
            lexer.Configuration.ReplaceWith(snapshot);
            Debug.WriteLine($"Lexer configuration loaded from {Path.GetFullPath(path)}");
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to load lexer configuration from '{path}'.", exception);
        }
    }

    private static class ConfigurationDumper
    {
        public static string Dump(IReadOnlyList<LexerPatternRegistration> patterns)
        {
            patterns = patterns.ArgNotNull();

            var lines = new List<string>
            {
                "# Lexer Configuration Dump",
                $"# Format-Version: {FormatVersion}",
                "# Format: <priority>|<base64_encoded_pattern>|<lexeme_type>|<ignore_flag>",
                "# Pattern is base64 encoded so regular expressions may contain pipe characters.",
                string.Empty
            };

            lines.AddRange(patterns.Select(FormatPatternLine));
            return string.Join(Environment.NewLine, lines);
        }

        private static string FormatPatternLine(LexerPatternRegistration item)
        {
            var encodedPattern = Convert.ToBase64String(Encoding.UTF8.GetBytes(item.Pattern.Pattern));
            return string.Join(
                '|',
                item.Priority.ToString("R", CultureInfo.InvariantCulture),
                encodedPattern,
                item.Pattern.LexemeType.GetName(),
                item.Ignore.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static class ConfigurationLoader
    {
        public static IReadOnlyList<LexerPatternRegistration> Build(string configText)
        {
            configText = configText.ArgNotNull();

            var parsedLines = ParseConfigurationLines(configText);
            ValidateDuplicates(parsedLines);

            return parsedLines
                .Select(static line => new LexerPatternRegistration(
                    line.Priority,
                    new LexemePattern(line.Pattern, ExtensibleEnum<LexemeTag>.CreateOrGet(line.LexemeType)),
                    line.Ignore))
                .ToArray();
        }

        private static IReadOnlyList<ConfigLine> ParseConfigurationLines(string configText)
        {
            var result = new List<ConfigLine>();
            var lineNumber = 0;

            foreach (var rawLine in configText.Split('\n'))
            {
                lineNumber++;
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                var parts = line.Split('|');
                if (parts.Length != 4)
                    throw new FormatException($"Line {lineNumber} must contain exactly four pipe-separated fields.");

                if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var priority) ||
                    !float.IsFinite(priority))
                {
                    throw new FormatException($"Line {lineNumber} contains invalid finite priority '{parts[0].Trim()}'.");
                }

                var pattern = DecodePattern(parts[1].Trim(), lineNumber);
                var lexemeType = parts[2].Trim();
                if (lexemeType.Length == 0)
                    throw new FormatException($"Line {lineNumber} contains an empty lexeme type.");

                if (!bool.TryParse(parts[3].Trim(), out var ignore))
                    throw new FormatException($"Line {lineNumber} contains invalid ignore flag '{parts[3].Trim()}'.");

                try
                {
                    _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
                }
                catch (ArgumentException exception)
                {
                    throw new FormatException($"Line {lineNumber} contains invalid regex pattern.", exception);
                }

                result.Add(new ConfigLine(lineNumber, priority, pattern, lexemeType, ignore));
            }

            return result;
        }

        private static string DecodePattern(string encodedPattern, int lineNumber)
        {
            try
            {
                return _strictUtf8.GetString(Convert.FromBase64String(encodedPattern));
            }
            catch (Exception exception) when (exception is FormatException or DecoderFallbackException)
            {
                throw new FormatException($"Line {lineNumber} contains an invalid Base64/UTF-8 pattern payload.", exception);
            }
        }

        private static void ValidateDuplicates(IReadOnlyList<ConfigLine> lines)
        {
            var pairs = new HashSet<(string Pattern, string LexemeType)>();
            var patternTexts = new HashSet<string>(StringComparer.Ordinal);
            var lexemeTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in lines)
            {
                if (!pairs.Add((line.Pattern, line.LexemeType)))
                    throw new FormatException($"Line {line.LineNumber} duplicates lexer pattern '{line.Pattern}' for lexeme type '{line.LexemeType}'.");
                if (!patternTexts.Add(line.Pattern))
                    throw new FormatException($"Line {line.LineNumber} assigns lexer regex '{line.Pattern}' to more than one lexeme type.");
                if (!lexemeTypes.Add(line.LexemeType))
                    throw new FormatException($"Line {line.LineNumber} assigns lexeme type '{line.LexemeType}' more than one regex.");
            }
        }

        private sealed record ConfigLine(
            int LineNumber,
            float Priority,
            string Pattern,
            string LexemeType,
            bool Ignore);
    }
}
