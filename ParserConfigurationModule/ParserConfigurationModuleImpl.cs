// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Diagnostics;
using BasicCore;
using BasicCore.ParserWrapper;
using ExceptionsManager;

namespace ParserConfigurationModule;

/// <summary>
///     <para> You can fix parser-modules execution order like this: </para>
///     1. Dump parser configuration to file <br />
///     2. Rearrange modules by hand <br />
///     3. Read configuration from file <br />
///     <para> For correct work this module have to execute InitParser after all others plugins </para>
/// </summary>
/// <param name="actionType">dump or read configuration</param>
/// <param name="path">path to file</param>
public class ParserConfigurationModuleImpl(ActionType actionType, string path = "ParserConfiguration.txt") : ICoreModule
{
    public void InitParser(IParser parser)
    {
        if (actionType == ActionType.Dump)
        {
            File.WriteAllText(path, ConfigurationToString(parser));
            Debug.WriteLine($"Parser configuration dumped to {Path.GetFullPath(path)}");
        }
        else
        {
            InitializeConfiguration(parser, File.ReadAllText(path));
            Debug.WriteLine($"Parser configuration read from {Path.GetFullPath(path)}");
        }
    }

    private void InitializeConfiguration(IParser parser, string text)
    {
        var dict = parser.Configuration.NodeCreators
            .SelectMany(x =>
                x.Value.Select(y => (y.GetType().FullName.NotNull(), y))
            ).ToDictionary();

        parser.Configuration.NodeCreators.Clear();

        var i = 0;
        foreach (var fullName in text.Split("\n"))
            parser.Configuration.NodeCreators.Add(i++, dict[fullName]);
    }

    private string ConfigurationToString(IParser parser)
    {
        var names = parser.Configuration.NodeCreators
            .SelectMany(x =>
                x.Value.Select(y => y.GetType().FullName.NotNull())
            );
        return string.Join("\n", names);
    }
}