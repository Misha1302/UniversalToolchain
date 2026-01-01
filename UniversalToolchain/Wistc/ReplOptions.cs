namespace Wistc;

[Verb("repl", HelpText = "Start REPL interactive mode")]
public class ReplOptions : CommonOptions
{
    [Option("history", HelpText = "Path to REPL history file")]
    public string? HistoryFile { get; set; }
}