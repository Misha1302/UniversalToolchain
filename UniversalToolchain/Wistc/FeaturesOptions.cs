namespace Wistc;

[Verb("features", HelpText = "Print provider-discovered features for a Wist dialect without creating a runtime host.")]
public sealed class FeaturesOptions
{
    [Option("dialect-file", Required = true, HelpText = "Path to the dialect-definition file to inspect.")]
    public string DialectFile { get; set; } = string.Empty;
}