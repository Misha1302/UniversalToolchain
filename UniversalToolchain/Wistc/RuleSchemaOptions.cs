namespace Wistc;

[Verb("rule-schema", HelpText = "Compile Wist rule declarations and print a deterministic rule schema.")]
public sealed class RuleSchemaOptions
{
    [Option("dialect-file", Required = true, HelpText = "Path to the dialect-definition file used to compile the rule set.")]
    public string DialectFile { get; set; } = string.Empty;

    [Option("source", Required = true, HelpText = "Path to the Wist rule source file.")]
    public string Source { get; set; } = string.Empty;

    [Option("backend", Required = false, Default = "compiler", HelpText = "Backend alias or facade mode used for compilation.")]
    public string Backend { get; set; } = "compiler";
}
