namespace Wistc;

[Verb("run", HelpText = "Run Wist code")]
public class RunOptions : CommonOptions
{
    [Value(0, MetaName = "code", Required = false, HelpText = "Code to execute")]
    public string? Code { get; set; }

    [Option('f', "file", HelpText = "File containing code to execute")]
    public string? File { get; set; }

    [Option('e', "eval", HelpText = "Evaluate expression and print result")]
    public bool Evaluate { get; set; }
}