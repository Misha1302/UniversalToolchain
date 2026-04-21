namespace Wistc;

public class CommonOptions
{
    [Option('m', "mode", Default = "compiler", HelpText = "Execution mode: 'compiler' or 'interpreter'")]
    public string Mode { get; set; } = "compiler";

    [Option("exclude-module", Separator = ',', HelpText = "Comma-separated list of module aliases to exclude (e.g., 'Whitespaces,Arithmetic')")]
    public IEnumerable<string>? ExcludeModules { get; set; }

    [Option("include-module", Separator = ',', HelpText = "Comma-separated list of additional module aliases to include")]
    public IEnumerable<string>? IncludeModules { get; set; }

    [Option("list-modules", Default = false, HelpText = "List all available modules and exit")]
    public bool ListModules { get; set; }

    [Option("use-native-math", Default = false, HelpText = "Using native math instead of generic math")]
    public bool UseNativeMath { get; set; }

    [Option("dialect-file", HelpText = "Path to a dialect-definition file that configures the Wist runtime")]
    public string? DialectFile { get; set; }
}
