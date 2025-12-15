// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace ParserConfigurationModule;

public enum ActionType
{
    /// <summary>
    /// Dump current configuration to file
    /// </summary>
    DumpConfiguration,
    
    /// <summary>
    /// Read configuration from file and apply it
    /// </summary>
    ReadConfiguration,
    
    /// <summary>
    /// Validate configuration file without applying
    /// </summary>
    Validate
}