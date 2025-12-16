// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace Wistc;

// ReSharper disable once ClassNeverInstantiated.Global
public class Options
{
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

    [Option('s', "source", Required = true, HelpText = "Path to the source code.")]
    public string SourcePath { get; set; } = null!;

    [Option('l', "logs", Required = false, HelpText = "Path to the file for logging.")]
    public string? LogsPath { get; set; } = null;

    [Option("parser-configuration", Required = false, HelpText = "Path to the file to dump/read parser configuration.")]
    public string? ParserConfigurationPath { get; set; } = null;

    [Option("parser-configuration-read", Required = false, HelpText = "To read parser configuration.")]
    public bool NeedToReadParserConfiguration { get; set; } = false;

    [Option("parser-configuration-dump", Required = false, HelpText = "To dump parser configuration.")]
    public bool NeedToDumpParserConfiguration { get; set; } = false;
}