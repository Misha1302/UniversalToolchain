namespace Wistc;

[Verb("dialect-inspect", HelpText = "Parse, validate and resolve a dialect file in dry-run mode")]
public class DialectInspectOptions
{
    [Option('f', "file", Required = true, HelpText = "Path to dialect definition file")]
    public string File { get; set; } = string.Empty;
}
