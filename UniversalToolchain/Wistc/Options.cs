namespace Wistc;

// ReSharper disable once ClassNeverInstantiated.Global
public class Options
{
    // Основные опции
    [Option('s', "source", Required = true, HelpText = "Path to the source code.")]
    public string SourcePath { get; set; } = null!;

    // Логирование
    [Option('l', "logs", Required = false, HelpText = "Path to the file for logging.")]
    public string? LogsPath { get; set; }

    [Option("no-logging", Required = false, Default = false, HelpText = "Disable logging.")]
    public bool NoLogging { get; set; }

    // Конфигурация парсера
    [Option("parser-config", Required = false, HelpText = "Path to parser configuration file.")]
    public string? ParserConfigPath { get; set; }

    [Option("parser-config-read", Required = false, Default = false, HelpText = "Read parser configuration from file.")]
    public bool ParserConfigRead { get; set; }

    [Option("parser-config-dump", Required = false, Default = false, HelpText = "Dump parser configuration to file.")]
    public bool ParserConfigDump { get; set; }

    // Конфигурация лексера
    [Option("lexer-config", Required = false, HelpText = "Path to lexer configuration file.")]
    public string? LexerConfigPath { get; set; }

    [Option("lexer-config-read", Required = false, Default = false, HelpText = "Read lexer configuration from file.")]
    public bool LexerConfigRead { get; set; }

    [Option("lexer-config-dump", Required = false, Default = false, HelpText = "Dump lexer configuration to file.")]
    public bool LexerConfigDump { get; set; }

    // Управление модулями
    [Option("disable-modules", Required = false, Separator = ',', HelpText = "Disable specific modules (comma-separated).")]
    public IEnumerable<string>? DisableModules { get; set; }

    [Option("custom-modules", Required = false, Separator = ',', HelpText = "Paths to custom module DLLs (comma-separated).")]
    public IEnumerable<string>? CustomModuleDlls { get; set; }

    // Дополнительные опции
    [Option('h', "help", Required = false, Default = false, HelpText = "Show help message.")]
    public bool Help { get; set; }

    [Option('v', "verbose", Required = false, Default = false, HelpText = "Enable verbose output.")]
    public bool Verbose { get; set; }

    [Option("version", Required = false, Default = false, HelpText = "Show version information.")]
    public bool Version { get; set; }

    // Валидация
    public bool Validate(out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(SourcePath))
        {
            errorMessage = "Source path is required.";
            return false;
        }

        if (!File.Exists(SourcePath))
        {
            errorMessage = $"Source file not found: {SourcePath}";
            return false;
        }

        // Проверка конфликтующих опций парсера
        if (ParserConfigRead && ParserConfigDump)
        {
            errorMessage = "Cannot both read and dump parser configuration.";
            return false;
        }

        if ((ParserConfigRead || ParserConfigDump) && string.IsNullOrWhiteSpace(ParserConfigPath))
        {
            errorMessage = "Parser configuration path is required when using parser config options.";
            return false;
        }

        // Проверка конфликтующих опций лексера
        if (LexerConfigRead && LexerConfigDump)
        {
            errorMessage = "Cannot both read and dump lexer configuration.";
            return false;
        }

        if ((LexerConfigRead || LexerConfigDump) && string.IsNullOrWhiteSpace(LexerConfigPath))
        {
            errorMessage = "Lexer configuration path is required when using lexer config options.";
            return false;
        }

        // Проверка кастомных модулей
        if (CustomModuleDlls != null)
        {
            foreach (var dllPath in CustomModuleDlls)
            {
                if (string.IsNullOrWhiteSpace(dllPath))
                {
                    errorMessage = "Custom module DLL path cannot be empty.";
                    return false;
                }

                if (!File.Exists(dllPath))
                {
                    errorMessage = $"Custom module DLL not found: {dllPath}";
                    return false;
                }

                if (!dllPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = $"Custom module must be a DLL file: {dllPath}";
                    return false;
                }
            }
        }

        errorMessage = string.Empty;
        return true;
    }
}