namespace Wistc;

public class CommonOptions
{
    [Option('b', "backend", Default = "compiler", HelpText = "Backend alias or id selected from the dialect runtime configuration")]
    public string Backend { get; set; } = "compiler";

    [Option("list-modules", Default = false, HelpText = "List all available runtime components and exit")]
    public bool ListModules { get; set; }

    [Option("dialect-file", HelpText = "Path to a dialect-definition file that configures the Wist runtime")]
    public string? DialectFile { get; set; }
}
