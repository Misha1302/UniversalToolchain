namespace Wistc;

[Verb("rule-run", HelpText = "Compile Wist rule declarations and run one rule with named arguments.")]
public sealed class RuleRunOptions
{
    [Option("dialect-file", Required = true, HelpText = "Path to the dialect-definition file used to compile the rule set.")]
    public string DialectFile { get; set; } = string.Empty;

    [Option("source", Required = true, HelpText = "Path to the Wist rule source file.")]
    public string Source { get; set; } = string.Empty;

    [Option("rule", Required = true, HelpText = "Name of the rule to execute.")]
    public string Rule { get; set; } = string.Empty;

    [Option("arg", Required = false, Separator = ',', HelpText = "Named argument in key=value form. Repeat or comma-separate values.")]
    public IEnumerable<string> Arguments { get; set; } = [];

    [Option("backend", Required = false, Default = "compiler", HelpText = "Backend alias or facade mode used for compilation and execution.")]
    public string Backend { get; set; } = "compiler";
}
