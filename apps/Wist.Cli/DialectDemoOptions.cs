namespace Wistc;

[Verb("dialect-demo", HelpText = "Run a minimal framework-native dialect DSL demo workflow")]
public class DialectDemoOptions
{
    [Option('f', "file", Required = false, HelpText = "Path to a dialect definition source file")]
    public string? File { get; set; }

    [Option('s', "scenario", Required = false, Default = "valid", HelpText = "Built-in scenario: valid, invalid-syntax, semantic-conflict, unresolved-module")]
    public string Scenario { get; set; } = "valid";
}