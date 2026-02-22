namespace ParserConfigurationModule;

public enum ActionType
{
    /// <summary>
    ///     Dump current configuration to file
    /// </summary>
    DumpConfiguration,

    /// <summary>
    ///     Read configuration from file and apply it
    /// </summary>
    ReadConfiguration
}