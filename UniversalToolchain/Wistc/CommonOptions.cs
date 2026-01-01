namespace Wistc;

public class CommonOptions
{
    [Option('m', "mode", Default = "compiler", HelpText = "Execution mode: 'compiler' or 'interpreter'")]
    public string Mode { get; set; } = "compiler";

    [Option("exclude-module", Separator = ',', HelpText = "Comma-separated list of module type names to exclude (e.g., 'WhitespacesModule.WhitespaceModuleImpl,ArithmeticModule.ArithmeticModuleImpl')")]
    public IEnumerable<string>? ExcludeModules { get; set; }

    [Option("include-module", Separator = ',', HelpText = "Comma-separated list of additional module type names to include (must be in loaded assemblies)")]
    public IEnumerable<string>? IncludeModules { get; set; }

    [Option("list-modules", Default = false, HelpText = "List all available modules and exit")]
    public bool ListModules { get; set; }
}